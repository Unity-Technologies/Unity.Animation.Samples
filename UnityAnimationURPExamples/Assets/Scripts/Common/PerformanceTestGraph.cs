using Unity.Entities;
using Unity.Animation;
using Unity.DataFlowGraph;
using Unity.Collections;
using UnityEngine.Assertions;

#if UNITY_EDITOR
using UnityEngine;
using Unity.Animation.Hybrid;

public class PerformanceTestGraph : AnimationGraphBase
{
    public AnimationClip[] Clips;

    public override void AddGraphSetupComponent(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (Clips == null || Clips.Length == 0)
        {
            UnityEngine.Debug.LogError("No clips specified for performance test!");
            return;
        }

        var clipBuffer = dstManager.AddBuffer<PerformanceSetupAsset>(entity);
        for (int i = 0; i < Clips.Length; ++i)
            clipBuffer.Add(new PerformanceSetupAsset { Clip = Clips[i].ToDenseClip() });

        dstManager.AddComponent<PerformanceSetup>(entity);

        dstManager.AddComponent<DeltaTime>(entity);
    }
}
#endif

public struct PerformanceSetup : ISampleSetup {};

public struct PerformanceSetupAsset : IBufferElementData
{
    public BlobAssetReference<Clip> Clip;
}

public struct PerformanceData : ISampleData
{
    public NodeHandle<ConvertDeltaTimeToFloatNode> DeltaTimeNode;
    public NodeHandle<MixerNode>     MixerNode;
    public NodeHandle<NMixerNode>    NMixerNode;
    public NodeHandle<ComponentNode> EntityNode;
}

public struct PerformanceDataAsset : ISystemStateBufferElementData
{
    public NodeHandle<ClipPlayerNode> ClipNode;
}

[UpdateBefore(typeof(DefaultAnimationSystemGroup))]
public class PerformanceGraphSystem : SampleSystemBase<
    PerformanceSetup,
    PerformanceData,
    ProcessDefaultAnimationGraph
>
{
    static Unity.Mathematics.Random s_Random = new Unity.Mathematics.Random(0x12345678);

    protected override PerformanceData CreateGraph(Entity entity, ref Rig rig, ProcessDefaultAnimationGraph graphSystem, ref PerformanceSetup setup)
    {
        if (!EntityManager.HasComponent<PerformanceSetupAsset>(entity))
            throw new System.InvalidOperationException("Entity is missing a PerformanceSetupAsset IBufferElementData");

        var set = graphSystem.Set;
        var data = new PerformanceData();
        data.DeltaTimeNode = set.Create<ConvertDeltaTimeToFloatNode>();
        data.EntityNode    = set.CreateComponentNode(entity);

        var clipBuffer = EntityManager.GetBuffer<PerformanceSetupAsset>(entity);
        Assert.AreNotEqual(clipBuffer.Length, 0);

        var clipPlayerNodes = new NativeArray<NodeHandle<ClipPlayerNode>>(clipBuffer.Length, Allocator.Temp);
        if (clipBuffer.Length == 1)
        {
            // Clip to output (no mixers)
            clipPlayerNodes[0] = set.Create<ClipPlayerNode>();
            set.SetData(clipPlayerNodes[0], ClipPlayerNode.KernelPorts.Speed, s_Random.NextFloat(0.1f, 1f));

            set.Connect(data.EntityNode, data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Input);
            set.Connect(data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, clipPlayerNodes[0], ClipPlayerNode.KernelPorts.DeltaTime);
            set.Connect(clipPlayerNodes[0], ClipPlayerNode.KernelPorts.Output, data.EntityNode, NodeSet.ConnectionType.Feedback);

            set.SendMessage(clipPlayerNodes[0], ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime });
            set.SendMessage(clipPlayerNodes[0], ClipPlayerNode.SimulationPorts.Rig, rig);
            set.SendMessage(clipPlayerNodes[0], ClipPlayerNode.SimulationPorts.Clip, clipBuffer[0].Clip);
        }
        else if (clipBuffer.Length == 2)
        {
            // Clips to binary mixer
            data.MixerNode = set.Create<MixerNode>();
            clipPlayerNodes[0] = set.Create<ClipPlayerNode>();
            clipPlayerNodes[1] = set.Create<ClipPlayerNode>();

            set.SetData(clipPlayerNodes[0], ClipPlayerNode.KernelPorts.Speed, s_Random.NextFloat(0.1f, 1f));
            set.SetData(clipPlayerNodes[1], ClipPlayerNode.KernelPorts.Speed, s_Random.NextFloat(0.1f, 1f));

            set.Connect(data.EntityNode, data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Input);
            set.Connect(data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, clipPlayerNodes[0], ClipPlayerNode.KernelPorts.DeltaTime);
            set.Connect(data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, clipPlayerNodes[1], ClipPlayerNode.KernelPorts.DeltaTime);
            set.Connect(clipPlayerNodes[0], ClipPlayerNode.KernelPorts.Output, data.MixerNode, MixerNode.KernelPorts.Input0);
            set.Connect(clipPlayerNodes[1], ClipPlayerNode.KernelPorts.Output, data.MixerNode, MixerNode.KernelPorts.Input1);
            set.Connect(data.MixerNode, MixerNode.KernelPorts.Output, data.EntityNode, NodeSet.ConnectionType.Feedback);

            set.SendMessage(clipPlayerNodes[0], ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime });
            set.SendMessage(clipPlayerNodes[1], ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime });
            set.SendMessage(clipPlayerNodes[0], ClipPlayerNode.SimulationPorts.Rig, rig);
            set.SendMessage(clipPlayerNodes[1], ClipPlayerNode.SimulationPorts.Rig, rig);
            set.SendMessage(clipPlayerNodes[0], ClipPlayerNode.SimulationPorts.Clip, clipBuffer[0].Clip);
            set.SendMessage(clipPlayerNodes[1], ClipPlayerNode.SimulationPorts.Clip, clipBuffer[1].Clip);
            set.SendMessage(data.MixerNode, MixerNode.SimulationPorts.Rig, rig);
            set.SetData(data.MixerNode, MixerNode.KernelPorts.Weight, s_Random.NextFloat(0f, 1f));
        }
        else
        {
            // Clips to n-mixer
            data.NMixerNode = set.Create<NMixerNode>();

            set.SendMessage(data.NMixerNode, NMixerNode.SimulationPorts.Rig, rig);
            set.SetPortArraySize(data.NMixerNode, NMixerNode.KernelPorts.Inputs, clipBuffer.Length);
            set.SetPortArraySize(data.NMixerNode, NMixerNode.KernelPorts.Weights, clipBuffer.Length);

            var clipWeights = new NativeArray<float>(clipBuffer.Length, Allocator.Temp);
            var wSum = 0f;
            for (int i = 0; i < clipBuffer.Length; ++i)
            {
                clipPlayerNodes[i] = set.Create<ClipPlayerNode>();

                set.SendMessage(clipPlayerNodes[i], ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime });
                set.SendMessage(clipPlayerNodes[i], ClipPlayerNode.SimulationPorts.Rig, rig);
                set.SendMessage(clipPlayerNodes[i], ClipPlayerNode.SimulationPorts.Clip, clipBuffer[i].Clip);
                set.SetData(clipPlayerNodes[i], ClipPlayerNode.KernelPorts.Speed, s_Random.NextFloat(0.1f, 1f));

                float w = s_Random.NextFloat(0.1f, 1f);
                wSum += w;
                clipWeights[i] = w;

                set.Connect(data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, clipPlayerNodes[i], ClipPlayerNode.KernelPorts.DeltaTime);
                set.Connect(clipPlayerNodes[i], ClipPlayerNode.KernelPorts.Output, data.NMixerNode, NMixerNode.KernelPorts.Inputs, i);
            }

            // Set normalized clip weights on NMixer
            float wFactor = 1f / wSum;
            for (int i = 0; i < clipBuffer.Length; ++i)
                set.SetData(data.NMixerNode, NMixerNode.KernelPorts.Weights, i, clipWeights[i] * wFactor);

            set.Connect(data.EntityNode, data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Input);
            set.Connect(data.NMixerNode, NMixerNode.KernelPorts.Output, data.EntityNode, NodeSet.ConnectionType.Feedback);
        }

        var clipNodeBuffer = PostUpdateCommands.AddBuffer<PerformanceDataAsset>(entity);
        for (int i = 0; i < clipPlayerNodes.Length; ++i)
            clipNodeBuffer.Add(new PerformanceDataAsset { ClipNode = clipPlayerNodes[i] });

        return data;
    }

    protected override void DestroyGraph(Entity entity, ProcessDefaultAnimationGraph graphSystem, ref PerformanceData data)
    {
        if (!EntityManager.HasComponent<PerformanceDataAsset>(entity))
            throw new System.InvalidOperationException("Entity is missing a PerformanceDataAsset ISystemStateBufferElementData");

        var set = graphSystem.Set;
        var clipNodeBuffer = EntityManager.GetBuffer<PerformanceDataAsset>(entity);
        for (int i = 0; i < clipNodeBuffer.Length; ++i)
            set.Destroy(clipNodeBuffer[i].ClipNode);

        EntityManager.RemoveComponent<PerformanceDataAsset>(entity);

        set.Destroy(data.DeltaTimeNode);
        set.Destroy(data.EntityNode);

        if (data.MixerNode != default)
            set.Destroy(data.MixerNode);

        if (data.NMixerNode != default)
            set.Destroy(data.NMixerNode);
    }
}
