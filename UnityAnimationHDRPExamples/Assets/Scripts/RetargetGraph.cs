using Unity.Entities;
using Unity.Animation;
using Unity.DataFlowGraph;
using Unity.Mathematics;
using Unity.Transforms;

#if UNITY_EDITOR
using UnityEngine;
using Unity.Animation.Hybrid;
using System.Collections.Generic;

public class RetargetGraph : AnimationGraphBase
{
    public GameObject SourceRigPrefab;
    public AnimationClip SourceClip;

    public List<string> RetargetMap;

    private RigRemapQuery m_RemapQuery;

    public override void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(SourceRigPrefab);
    }

    public override void PreProcessData<T>(T data)
    {
        if (data is RigComponent)
        {
            var dstRig = data as RigComponent;
            var srcRig = SourceRigPrefab.GetComponent<RigComponent>();
            m_RemapQuery = CreateRemapQuery(srcRig, dstRig, RetargetMap);
        }
    }

    public override void AddGraphSetupComponent(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (dstManager.HasComponent<RigDefinitionSetup>(entity))
        {
            var srcRigPrefab = conversionSystem.TryGetPrimaryEntity(SourceRigPrefab);
            var srcRigDefinition = dstManager.GetComponentData<RigDefinitionSetup>(srcRigPrefab);

            var dstRigDefinition = dstManager.GetComponentData<RigDefinitionSetup>(entity);

            var graphSetup = new RetargetSetup
            {
                SrcClip = ClipBuilder.AnimationClipToDenseClip(SourceClip),
                SrcRig = srcRigDefinition.Value,
                DstRig = dstRigDefinition.Value,
                RemapTable = m_RemapQuery.ToRigRemapTable(srcRigDefinition.Value, dstRigDefinition.Value)
            };

            dstManager.AddComponentData(entity, graphSetup);
        }
    }

    public RigRemapQuery CreateRemapQuery(RigComponent srcRig, RigComponent dstRig, List<string> retargetMap)
    {
        string status = "";

        List<ChannelMap> translationChannels = new List<ChannelMap>();
        List<ChannelMap> rotationChannels = new List<ChannelMap>();
        List<RigTranslationOffset> translationOffsets = new List<RigTranslationOffset>();
        List<RigRotationOffset> rotationOffsets = new List<RigRotationOffset>();

        for (var mapIter = 0; mapIter < retargetMap.Count; mapIter++)
        {
            bool success = false;

            string[] splitMap = retargetMap[mapIter].Split(new char[] { ' ' }, 3);

            for (var srcBoneIter = 0; srcBoneIter < srcRig.Bones.Length; srcBoneIter++)
            {
                if (splitMap.Length > 0 && splitMap[0] == srcRig.Bones[srcBoneIter].name)
                {
                    for (var dstBoneIter = 0; dstBoneIter < dstRig.Bones.Length; dstBoneIter++)
                    {
                        if (splitMap.Length > 1 && splitMap[1] == dstRig.Bones[dstBoneIter].name)
                        {
                            if (splitMap.Length > 2)
                            {
                                var srcPath = RigGenerator.ComputeRelativePath(srcRig.Bones[srcBoneIter], srcRig.transform);
                                var dstPath = RigGenerator.ComputeRelativePath(dstRig.Bones[dstBoneIter], dstRig.transform);

                                if (splitMap[2] == "TR" || splitMap[2] == "T")
                                {
                                    var translationOffset = new RigTranslationOffset();

                                    // heuristic that computes retarget scale based on translation node (ex: hips) height (assumed to be y)
                                    translationOffset.Scale = dstRig.Bones[dstBoneIter].position.y / srcRig.Bones[srcBoneIter].position.y;
                                    
                                    translationOffset.Rotation = math.mul(math.conjugate(dstRig.Bones[dstBoneIter].parent.rotation), srcRig.Bones[srcBoneIter].parent.rotation);

                                    translationOffsets.Add(translationOffset);
                                    translationChannels.Add(new ChannelMap() { SourceId = srcPath, DestinationId = dstPath, OffsetIndex = translationOffsets.Count });
                                }

                                if (splitMap[2] == "TR" || splitMap[2] == "R")
                                {
                                    var rotationOffset = new RigRotationOffset();

                                    rotationOffset.PreRotation = math.mul(math.conjugate(dstRig.Bones[dstBoneIter].parent.rotation), srcRig.Bones[srcBoneIter].parent.rotation);
                                    rotationOffset.PostRotation = math.mul(math.conjugate(srcRig.Bones[srcBoneIter].rotation), dstRig.Bones[dstBoneIter].rotation);

                                    rotationOffsets.Add(rotationOffset);
                                    rotationChannels.Add(new ChannelMap() { SourceId = srcPath, DestinationId = dstPath, OffsetIndex = rotationOffsets.Count });
                                }

                                success = true;
                            }
                        }
                    }
                }
            }

            if (!success)
            {
                status = status + mapIter + " ";
            }
        }

        RigRemapQuery rigRemapQuery = new RigRemapQuery();

        rigRemapQuery.TranslationChannels = new ChannelMap[translationChannels.Count];

        for (var iter = 0; iter < translationChannels.Count; iter++)
        {
            rigRemapQuery.TranslationChannels[iter] = translationChannels[iter];
        }

        rigRemapQuery.TranslationOffsets = new RigTranslationOffset[translationOffsets.Count + 1];
        rigRemapQuery.TranslationOffsets[0] = new RigTranslationOffset();

        for (var iter = 0; iter < translationOffsets.Count; iter++)
        {
            rigRemapQuery.TranslationOffsets[iter + 1] = translationOffsets[iter];
        }

        rigRemapQuery.RotationChannels = new ChannelMap[rotationChannels.Count];

        for (var iter = 0; iter < rotationChannels.Count; iter++)
        {
            rigRemapQuery.RotationChannels[iter] = rotationChannels[iter];
        }

        rigRemapQuery.RotationOffsets = new RigRotationOffset[rotationOffsets.Count + 1];
        rigRemapQuery.RotationOffsets[0] = new RigRotationOffset();

        for (var iter = 0; iter < rotationOffsets.Count; iter++)
        {
            rigRemapQuery.RotationOffsets[iter + 1] = rotationOffsets[iter];
        }

        if (!string.IsNullOrEmpty(status))
        {
            UnityEngine.Debug.LogError("Faulty Entries : " + status);
        }

        return rigRemapQuery;
    }
}
#endif

public struct RetargetSetup : ISampleSetup
{
    public BlobAssetReference<Clip>  SrcClip;
    public BlobAssetReference<RigDefinition> SrcRig;
    public BlobAssetReference<RigDefinition> DstRig;
    public BlobAssetReference<RigRemapTable> RemapTable;
};

public struct RetargetData : ISampleData
{
    public NodeHandle DeltaTimeNode;
    public NodeHandle ClipPlayerNode;
    public NodeHandle RemapperNode;

    public GraphOutput Output;      // DstRig
    public GraphOutput DebugOutput; // SrcRig visualization
}

[UpdateInGroup(typeof(AnimationSystemGroup))]
[UpdateBefore(typeof(AnimationGraphSystem))]
public class RetargetGraphSystem : SampleSystemBase<RetargetSetup, RetargetData>
{
    protected override RetargetData CreateGraph(Entity entity, NodeSet set, ref RetargetSetup setup)
    {
        var srcClip = ClipManager.Instance.GetClipFor(setup.SrcRig, setup.SrcClip);
        var data = new RetargetData();

        data.DeltaTimeNode = set.Create<DeltaTimeNode>();
        data.ClipPlayerNode = set.Create<ClipPlayerNode>();
        data.RemapperNode = set.Create<RigRemapperNode>();

        set.SendMessage(data.ClipPlayerNode, (InputPortID)ClipPlayerNode.SimulationPorts.Speed, 1.0f);

        // Connect kernel ports
        set.Connect(data.DeltaTimeNode, (OutputPortID)DeltaTimeNode.KernelPorts.DeltaTime, data.ClipPlayerNode, (InputPortID)ClipPlayerNode.KernelPorts.DeltaTime);
        set.Connect(data.ClipPlayerNode, (OutputPortID)ClipPlayerNode.KernelPorts.Output, data.RemapperNode, (InputPortID)RigRemapperNode.KernelPorts.Input);

        // Send messages
        set.SendMessage(data.ClipPlayerNode, (InputPortID)ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = (int)ClipConfigurationMask.LoopTime });
        set.SendMessage(data.ClipPlayerNode, (InputPortID)ClipPlayerNode.SimulationPorts.ClipInstance, srcClip);
        set.SendMessage(data.RemapperNode, (InputPortID)RigRemapperNode.SimulationPorts.SourceRigDefinition, setup.SrcRig);
        set.SendMessage(data.RemapperNode, (InputPortID)RigRemapperNode.SimulationPorts.DestinationRigDefinition, setup.DstRig);
        set.SendMessage(data.RemapperNode, (InputPortID)RigRemapperNode.SimulationPorts.RemapTable, setup.RemapTable);

        data.Output.Buffer = set.CreateGraphValue<Buffer<float>>(data.RemapperNode, (OutputPortID)RigRemapperNode.KernelPorts.Output);
        data.DebugOutput.Buffer = set.CreateGraphValue<Buffer<float>>(data.ClipPlayerNode, (OutputPortID)ClipPlayerNode.KernelPorts.Output);

        var debugEntity = RigUtils.InstantiateDebugRigEntity(
            setup.SrcRig,
            EntityManager,
            new BoneRendererProperties { BoneShape = BoneRendererUtils.BoneShape.Line, Color = new float4(0f, 1f, 0f, 0.5f), Size = 1f }
            );

        if (EntityManager.HasComponent<Translation>(entity))
        {
            PostUpdateCommands.AddComponent(
                debugEntity,
                EntityManager.GetComponentData<Translation>(entity)
                );
        }

        PostUpdateCommands.AddComponent(entity, data.Output);
        PostUpdateCommands.AddComponent(debugEntity, data.DebugOutput);

        return data;
    }

    protected override void DestroyGraph(Entity entity, NodeSet set, ref RetargetData data)
    {
        set.Destroy(data.DeltaTimeNode);
        set.Destroy(data.ClipPlayerNode);
        set.Destroy(data.RemapperNode);
        set.ReleaseGraphValue(data.Output.Buffer);
        set.ReleaseGraphValue(data.DebugOutput.Buffer);
    }

    protected override void UpdateGraph(Entity entity, NodeSet set, ref RetargetData data)
    {
    }
}
