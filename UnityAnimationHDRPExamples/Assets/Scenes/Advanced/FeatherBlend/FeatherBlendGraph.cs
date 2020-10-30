using System;
using Unity.Animation;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

#if UNITY_EDITOR
using UnityEngine;
using Unity.Animation.Hybrid;
using System.Collections.Generic;

public class FeatherBlendGraph : AnimationGraphBase
{
    [Serializable]
    public struct FeatherBlend
    {
        // ID is name of the transform or custom channel's ID.
        public string Id;
        public float Weight;
    }

    public AnimationClip Clip1;
    public AnimationClip Clip2;
    public float DefaultWeight;
    public List<FeatherBlend> Blends;

    private ChannelWeightQuery m_FeatherBlendQuery;

    public override void PreProcessData<T>(T data)
    {
        if (data is RigComponent)
        {
            var rig = data as RigComponent;
            m_FeatherBlendQuery = CreateFeatherBlendQuery(rig, Blends);
        }
    }

    public override void AddGraphSetupComponent(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (dstManager.HasComponent<Rig>(entity))
        {
            var denseClip1 = Clip1.ToDenseClip();
            var denseClip2 = Clip2.ToDenseClip();
            var rig = dstManager.GetComponentData<Rig>(entity);

            var graphSetup = new FeatherBlendSetup
            {
                Clip1 = denseClip1,
                Clip2 = denseClip2,
                DefaultWeight = DefaultWeight,
                WeightTable = m_FeatherBlendQuery.ToChannelWeightTable(rig)
            };

            dstManager.AddComponentData(entity, graphSetup);

            dstManager.AddComponent<DeltaTime>(entity);
        }
    }

    /// <summary>
    /// From the transform's name, find the path in the rig that will later be converted to
    /// a hash, and then the hash will be used to find an index in the AnimationStream.
    /// </summary>
    public ChannelWeightQuery CreateFeatherBlendQuery(RigComponent rig, List<FeatherBlend> blendMap)
    {
        string status = "";

        List<ChannelWeightMap> channels = new List<ChannelWeightMap>();

        for (var mapIter = 0; mapIter < blendMap.Count; mapIter++)
        {
            bool success = false;

            for (var srcBoneIter = 0; srcBoneIter < rig.Bones.Length; srcBoneIter++)
            {
                if (blendMap[mapIter].Id == rig.Bones[srcBoneIter].name)
                {
                    var srcPath = RigGenerator.ComputeRelativePath(rig.Bones[srcBoneIter], rig.transform);

                    channels.Add(new ChannelWeightMap() {Id = srcPath, Weight = blendMap[mapIter].Weight});
                }

                success = true;
            }

            if (!success)
            {
                status = status + mapIter + " ";
            }
        }

        ChannelWeightQuery featherBlendQuery = new ChannelWeightQuery();
        featherBlendQuery.Channels = new ChannelWeightMap[channels.Count];

        for (var iter = 0; iter < channels.Count; iter++)
        {
            featherBlendQuery.Channels[iter] = channels[iter];
        }

        if (!string.IsNullOrEmpty(status))
        {
            UnityEngine.Debug.LogError("Faulty Entries : " + status);
        }

        return featherBlendQuery;
    }
}
#endif

public struct FeatherBlendSetup : ISampleSetup
{
    public BlobAssetReference<Clip>  Clip1;
    public BlobAssetReference<Clip>  Clip2;
    public float DefaultWeight;
    public BlobAssetReference<ChannelWeightTable> WeightTable;
};

public struct FeatherBlendData : ISampleData
{
    public NodeHandle<ConvertDeltaTimeToFloatNode> DeltaTimeNode;
    public NodeHandle<ClipPlayerNode> Clip1Node;
    public NodeHandle<ClipPlayerNode> Clip2Node;
    public NodeHandle<ChannelWeightMixerNode> FeatherBlendNode;
    public NodeHandle<WeightBuilderNode> WeightBuilderNode;

    public NodeHandle<ComponentNode> EntityNode;
    public NodeHandle<ComponentNode> DebugClip1EntityNode;
    public NodeHandle<ComponentNode> DebugClip2EntityNode;
}

[UpdateBefore(typeof(DefaultAnimationSystemGroup))]
public class FeatherBlendGraphSystem : SampleSystemBase<
    FeatherBlendSetup,
    FeatherBlendData,
    ProcessDefaultAnimationGraph
>
{
    protected override FeatherBlendData CreateGraph(Entity entity, ref Rig rig, ProcessDefaultAnimationGraph graphSystem, ref FeatherBlendSetup setup)
    {
        var set = graphSystem.Set;

        var debugClip1Entity = RigUtils.InstantiateDebugRigEntity(
            rig,
            EntityManager,
            new BoneRendererProperties { BoneShape = BoneRendererUtils.BoneShape.Line, Color = new float4(1f, 0f, 0f, 1f), Size = 1f }
        );

        var debugClip2Entity = RigUtils.InstantiateDebugRigEntity(
            rig,
            EntityManager,
            new BoneRendererProperties { BoneShape = BoneRendererUtils.BoneShape.Line, Color = new float4(0f, 0f, 1f, 1f), Size = 1f }
        );

        if (EntityManager.HasComponent<Translation>(entity))
        {
            float3 t = EntityManager.GetComponentData<Translation>(entity).Value;
            float3 offset = new float3(1.5f, 0f, 0f);

            PostUpdateCommands.AddComponent(
                debugClip1Entity,
                new Translation { Value = t - offset }
            );

            PostUpdateCommands.AddComponent(
                debugClip2Entity,
                new Translation { Value = t + offset }
            );
        }

        var data = new FeatherBlendData();

        // Create the nodes.
        data.DeltaTimeNode        = set.Create<ConvertDeltaTimeToFloatNode>();
        data.Clip1Node            = set.Create<ClipPlayerNode>();
        data.Clip2Node            = set.Create<ClipPlayerNode>();
        data.FeatherBlendNode     = set.Create<ChannelWeightMixerNode>();
        data.WeightBuilderNode    = set.Create<WeightBuilderNode>();
        data.EntityNode           = set.CreateComponentNode(entity);
        data.DebugClip1EntityNode = set.CreateComponentNode(debugClip1Entity);
        data.DebugClip2EntityNode = set.CreateComponentNode(debugClip2Entity);

        // Connect the nodes. The DeltaTimeNode output is used to evaluate the clips in the ClipNodes,
        // and the ClipNodes' outputs are blended in the FeatherBlendNode, each channel being weighted
        // depending on the value of the corresponding element in the weight buffer output by the WeightBuilderNode.
        set.Connect(data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, data.Clip1Node, ClipPlayerNode.KernelPorts.DeltaTime);
        set.Connect(data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, data.Clip2Node, ClipPlayerNode.KernelPorts.DeltaTime);
        set.Connect(data.Clip1Node, ClipPlayerNode.KernelPorts.Output, data.FeatherBlendNode, ChannelWeightMixerNode.KernelPorts.Input0);
        set.Connect(data.Clip2Node, ClipPlayerNode.KernelPorts.Output, data.FeatherBlendNode, ChannelWeightMixerNode.KernelPorts.Input1);
        set.Connect(data.WeightBuilderNode, WeightBuilderNode.KernelPorts.Output, data.FeatherBlendNode, ChannelWeightMixerNode.KernelPorts.WeightMasks);

        set.SetData(data.WeightBuilderNode, WeightBuilderNode.KernelPorts.DefaultWeight, setup.DefaultWeight);
        set.SetData(data.FeatherBlendNode, ChannelWeightMixerNode.KernelPorts.Weight, 1.0f);
        set.SetData(data.Clip1Node, ClipPlayerNode.KernelPorts.Speed, 1.0f);
        set.SetData(data.Clip2Node, ClipPlayerNode.KernelPorts.Speed, 1.0f);

        // Set the port arrays for the WightBuilderNode.
        var weightLength = setup.WeightTable.Value.Weights.Length;
        set.SetPortArraySize(data.WeightBuilderNode, WeightBuilderNode.KernelPorts.ChannelIndices, (ushort)weightLength);
        set.SetPortArraySize(data.WeightBuilderNode, WeightBuilderNode.KernelPorts.ChannelWeights, (ushort)weightLength);
        for (ushort i = 0; i < weightLength; ++i)
        {
            var w = setup.WeightTable.Value.Weights[i];
            set.SetData(data.WeightBuilderNode, WeightBuilderNode.KernelPorts.ChannelIndices, i, w.Index);
            set.SetData(data.WeightBuilderNode, WeightBuilderNode.KernelPorts.ChannelWeights, i, w.Weight);
        }

        // Connect RigEntity component data
        set.Connect(data.EntityNode, data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Input);
        set.Connect(data.FeatherBlendNode, ChannelWeightMixerNode.KernelPorts.Output, data.EntityNode, NodeSet.ConnectionType.Feedback);

        // Connect Debug1Entity component data
        set.Connect(data.Clip1Node, ClipPlayerNode.KernelPorts.Output, data.DebugClip1EntityNode);

        // Connect Debug2Entity component data
        set.Connect(data.Clip2Node, ClipPlayerNode.KernelPorts.Output, data.DebugClip2EntityNode);

        // Send messages to setup constant data in the nodes.
        set.SendMessage(data.Clip1Node, ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime });
        set.SendMessage(data.Clip2Node, ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime });
        set.SendMessage(data.Clip1Node, ClipPlayerNode.SimulationPorts.Rig, rig);
        set.SendMessage(data.Clip1Node, ClipPlayerNode.SimulationPorts.Clip, setup.Clip1);
        set.SendMessage(data.Clip2Node, ClipPlayerNode.SimulationPorts.Rig, rig);
        set.SendMessage(data.Clip2Node, ClipPlayerNode.SimulationPorts.Clip, setup.Clip2);
        set.SendMessage(data.FeatherBlendNode, ChannelWeightMixerNode.SimulationPorts.Rig, rig);
        set.SendMessage(data.WeightBuilderNode, WeightBuilderNode.SimulationPorts.Rig, rig);

        return data;
    }

    protected override void DestroyGraph(Entity entity, ProcessDefaultAnimationGraph graphSystem, ref FeatherBlendData data)
    {
        var set = graphSystem.Set;
        set.Destroy(data.DeltaTimeNode);
        set.Destroy(data.Clip1Node);
        set.Destroy(data.Clip2Node);
        set.Destroy(data.WeightBuilderNode);
        set.Destroy(data.FeatherBlendNode);
        set.Destroy(data.EntityNode);
        set.Destroy(data.DebugClip1EntityNode);
        set.Destroy(data.DebugClip2EntityNode);
    }
}
