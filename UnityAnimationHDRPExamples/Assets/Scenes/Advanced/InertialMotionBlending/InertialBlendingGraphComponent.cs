using System;
using Unity.Animation;
using Unity.Entities;
using UnityEngine;
using Unity.DataFlowGraph;

#if UNITY_EDITOR
using Unity.Animation.Hybrid;

[ConverterVersion("InertialBlendingGraphComponentConversion", 2)]
public class InertialBlendingGraphComponent : MonoBehaviour, IConvertGameObjectToEntity
{
    public AnimationClip SourceClip;
    public AnimationClip DestClip;
    public TransitionByBoolNode.TransitionType TransitionType;
    public UnityEngine.AnimationCurve BlendCurve;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (SourceClip == null)
            throw new NullReferenceException($"Error in ({this}) conversion: source clip is null.");
        if (DestClip == null)
            throw new NullReferenceException($"Error in ({this}) conversion: dest clip is null.");

        conversionSystem.DeclareAssetDependency(gameObject, SourceClip);
        conversionSystem.DeclareAssetDependency(gameObject, DestClip);

        var blendCurve = BlobAssetReference<AnimationCurveBlob>.Null;
        if (BlendCurve != null && TransitionType == TransitionByBoolNode.TransitionType.Crossfade)
            blendCurve = CurveConversion.ToAnimationCurveBlobAssetRef(BlendCurve);

        dstManager.AddComponentData(entity, new InertialBlendingGraphSetup
        {
            SourceClip = conversionSystem.BlobAssetStore.GetClip(SourceClip),
            DestClip = conversionSystem.BlobAssetStore.GetClip(DestClip),
            TransitionType = TransitionType,
            BlendCurve = blendCurve,
        });

        dstManager.AddComponent<DeltaTime>(entity);
    }
}
#endif

public struct InertialBlendingGraphSetup : ISampleSetup
{
    public BlobAssetReference<Clip> SourceClip;
    public BlobAssetReference<Clip> DestClip;
    public TransitionByBoolNode.TransitionType TransitionType;
    public BlobAssetReference<AnimationCurveBlob> BlendCurve;
}

public struct InertialBlendingGraphData : ISampleData
{
    public GraphHandle Graph;
    public NodeHandle<TransitionByBoolNode> TransitionNode;
}

[UpdateBefore(typeof(DefaultAnimationSystemGroup))]
public class InertialBlendingGraphSystem : SampleSystemBase<InertialBlendingGraphSetup, InertialBlendingGraphData, ProcessDefaultAnimationGraph>
{
    protected override void DestroyGraph(Entity entity, ProcessDefaultAnimationGraph graphSystem, ref InertialBlendingGraphData data)
    {
        graphSystem.Dispose(data.Graph);
    }

    protected override InertialBlendingGraphData CreateGraph(Entity entity, ref Rig rig, ProcessDefaultAnimationGraph graphSystem, ref InertialBlendingGraphSetup setup)
    {
        GraphHandle graph = graphSystem.CreateGraph();
        var data = new InertialBlendingGraphData
        {
            Graph = graph,
            TransitionNode = graphSystem.CreateNode<TransitionByBoolNode>(graph),
        };

        var clipPlayerNode0 = graphSystem.CreateNode<ClipPlayerNode>(graph);
        var clipPlayerNode1 = graphSystem.CreateNode<ClipPlayerNode>(graph);
        var deltaTimeNode = graphSystem.CreateNode<ConvertDeltaTimeToFloatNode>(graph);
        var timeCounterNode = graphSystem.CreateNode<TimeCounterNode>(graph);
        var entityNode = graphSystem.CreateNode(graph, entity);

        var set = graphSystem.Set;

        // Connect kernel ports
        set.Connect(entityNode, deltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Input);
        set.Connect(deltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, clipPlayerNode0, ClipPlayerNode.KernelPorts.DeltaTime);
        set.Connect(deltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, clipPlayerNode1, ClipPlayerNode.KernelPorts.DeltaTime);
        set.Connect(deltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, timeCounterNode, TimeCounterNode.KernelPorts.DeltaTime);
        set.Connect(timeCounterNode, TimeCounterNode.KernelPorts.OutputDeltaTime, data.TransitionNode, TransitionByBoolNode.KernelPorts.DeltaTime);
        set.Connect(timeCounterNode, TimeCounterNode.KernelPorts.Time, data.TransitionNode, TransitionByBoolNode.KernelPorts.Time);

        set.Connect(clipPlayerNode0, ClipPlayerNode.KernelPorts.Output, data.TransitionNode, TransitionByBoolNode.KernelPorts.FalseInput);
        set.Connect(clipPlayerNode1, ClipPlayerNode.KernelPorts.Output, data.TransitionNode, TransitionByBoolNode.KernelPorts.TrueInput);
        set.Connect(data.TransitionNode, TransitionByBoolNode.KernelPorts.Output, entityNode, NodeSet.ConnectionType.Feedback);

        // Send messages to set parameters on the ClipPlayerNode
        set.SetData(clipPlayerNode0, ClipPlayerNode.KernelPorts.Speed, 1.0f);
        set.SetData(clipPlayerNode1, ClipPlayerNode.KernelPorts.Speed, 1.0f);
        set.SetData(timeCounterNode, TimeCounterNode.KernelPorts.Speed, 1.0f);
        set.SendMessage(clipPlayerNode0, ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime });
        set.SendMessage(clipPlayerNode1, ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime });
        set.SendMessage(clipPlayerNode0, ClipPlayerNode.SimulationPorts.Rig, rig);
        set.SendMessage(clipPlayerNode1, ClipPlayerNode.SimulationPorts.Rig, rig);
        set.SendMessage(clipPlayerNode0, ClipPlayerNode.SimulationPorts.Clip, setup.SourceClip);
        set.SendMessage(clipPlayerNode1, ClipPlayerNode.SimulationPorts.Clip, setup.DestClip);

        // Configure our transition
        set.SendMessage(data.TransitionNode, TransitionByBoolNode.SimulationPorts.Rig, rig);
        set.SendMessage(data.TransitionNode, TransitionByBoolNode.SimulationPorts.TransitionType, setup.TransitionType);
        set.SendMessage(data.TransitionNode, TransitionByBoolNode.SimulationPorts.BlendCurve, setup.BlendCurve);

        return data;
    }
}
