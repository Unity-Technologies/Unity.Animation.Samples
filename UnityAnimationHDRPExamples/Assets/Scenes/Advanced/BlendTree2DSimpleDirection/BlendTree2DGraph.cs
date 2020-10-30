using Unity.Entities;
using Unity.Animation;
using Unity.DataFlowGraph;
using Unity.Burst;

#if UNITY_EDITOR
using UnityEditor.Animations;
using Unity.Animation.Editor;

public class BlendTree2DGraph : AnimationGraphBase
{
    public BlendTree BlendTree;

    public override void AddGraphSetupComponent(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var blendTreeIndex = BlendTreeConversion.Convert(BlendTree, entity, dstManager);

        var graphSetup = new BlendTree2DSetup
        {
            BlendTreeIndex = blendTreeIndex,
        };

        dstManager.AddComponentData(entity, graphSetup);

        dstManager.AddComponent<DeltaTime>(entity);
    }
}
#endif

public struct BlendTree2DSetup : ISampleSetup
{
    public int BlendTreeIndex;
}

public struct BlendTree2DData : ISampleData
{
    public NodeHandle<ConvertDeltaTimeToFloatNode>      DeltaTimeNode;
    public NodeHandle<TimeCounterNode>                  TimeCounterNode;
    public NodeHandle<TimeLoopNode>                     TimeLoopNode;
    public NodeHandle<FloatRcpNode>                     FloatRcpNode;
    public NodeHandle<BlendTree2DNode>                  BlendTreeNode;
    public NodeHandle<ExtractBlendTree2DParametersNode> BlendTreeInputNode;
    public NodeHandle<ComponentNode>                    EntityNode;

    public BlobAssetReference<BlendTree2DSimpleDirectional> BlendTreeAsset;
    public float paramX;
    public float paramY;
}

[UpdateBefore(typeof(DefaultAnimationSystemGroup))]
public class BlendTree2DGraphSystem : SampleSystemBase<
    BlendTree2DSetup,
    BlendTree2DData,
    ProcessDefaultAnimationGraph
>
{
    protected override BlendTree2DData CreateGraph(Entity entity, ref Rig rig, ProcessDefaultAnimationGraph graphSystem, ref BlendTree2DSetup setup)
    {
        var set = graphSystem.Set;
        var blendTreeComponent = EntityManager.GetBuffer<BlendTree2DResource>(entity);
        var blendTreeAsset = BlendTreeBuilder.CreateBlendTree2DFromComponents(blendTreeComponent[setup.BlendTreeIndex], EntityManager, entity);
        var data = new BlendTree2DData();

        data.BlendTreeNode = set.Create<BlendTree2DNode>();
        data.BlendTreeAsset = blendTreeAsset;

        data.DeltaTimeNode      = set.Create<ConvertDeltaTimeToFloatNode>();
        data.TimeCounterNode = set.Create<TimeCounterNode>();
        data.TimeLoopNode = set.Create<TimeLoopNode>();
        data.FloatRcpNode = set.Create<FloatRcpNode>();
        data.EntityNode = set.CreateComponentNode(entity);
        data.BlendTreeInputNode = set.Create<ExtractBlendTree2DParametersNode>();

        set.Connect(data.EntityNode, data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Input);
        set.Connect(data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, data.TimeCounterNode, TimeCounterNode.KernelPorts.DeltaTime);
        set.Connect(data.TimeCounterNode, TimeCounterNode.KernelPorts.Time, data.TimeLoopNode, TimeLoopNode.KernelPorts.InputTime);
        set.Connect(data.TimeLoopNode, TimeLoopNode.KernelPorts.OutputTime, data.BlendTreeNode, BlendTree2DNode.KernelPorts.NormalizedTime);

        set.Connect(data.BlendTreeNode, BlendTree2DNode.KernelPorts.Duration, data.FloatRcpNode, FloatRcpNode.KernelPorts.Input);
        set.Connect(data.FloatRcpNode, FloatRcpNode.KernelPorts.Output, data.TimeCounterNode, TimeCounterNode.KernelPorts.Speed);

        set.Connect(data.BlendTreeNode, BlendTree2DNode.KernelPorts.Output, data.EntityNode, NodeSet.ConnectionType.Feedback);

        set.Connect(data.EntityNode, data.BlendTreeInputNode, ExtractBlendTree2DParametersNode.KernelPorts.Input, NodeSet.ConnectionType.Feedback);
        set.Connect(data.BlendTreeInputNode, ExtractBlendTree2DParametersNode.KernelPorts.OutParamX, data.BlendTreeNode, BlendTree2DNode.KernelPorts.BlendParameterX);
        set.Connect(data.BlendTreeInputNode, ExtractBlendTree2DParametersNode.KernelPorts.OutParamY, data.BlendTreeNode, BlendTree2DNode.KernelPorts.BlendParameterY);

        set.SendMessage(data.TimeLoopNode, TimeLoopNode.SimulationPorts.Duration, 1.0F);
        set.SendMessage(data.BlendTreeNode, BlendTree2DNode.SimulationPorts.Rig, rig);
        set.SendMessage(data.BlendTreeNode, BlendTree2DNode.SimulationPorts.BlendTree, data.BlendTreeAsset);

        return data;
    }

    protected override void DestroyGraph(Entity entity, ProcessDefaultAnimationGraph graphSystem, ref BlendTree2DData data)
    {
        var set = graphSystem.Set;
        set.Destroy(data.DeltaTimeNode);
        set.Destroy(data.TimeCounterNode);
        set.Destroy(data.TimeLoopNode);
        set.Destroy(data.FloatRcpNode);
        set.Destroy(data.BlendTreeNode);
        set.Destroy(data.BlendTreeInputNode);
        set.Destroy(data.EntityNode);
    }
}

public class ExtractBlendTree2DParametersNode
    : KernelNodeDefinition<ExtractBlendTree2DParametersNode.KernelDefs>
{
    public struct KernelDefs : IKernelPortDefinition
    {
        public DataInput<ExtractBlendTree2DParametersNode, BlendTree2DData> Input;
        public DataOutput<ExtractBlendTree2DParametersNode, float> OutParamX;
        public DataOutput<ExtractBlendTree2DParametersNode, float> OutParamY;
    }

    struct KernelData : IKernelData {}

    [BurstCompile]
    struct Kernel : IGraphKernel<KernelData, KernelDefs>
    {
        public void Execute(RenderContext ctx, KernelData data, ref KernelDefs ports)
        {
            ctx.Resolve(ref ports.OutParamX) = ctx.Resolve(ports.Input).paramX;
            ctx.Resolve(ref ports.OutParamY) = ctx.Resolve(ports.Input).paramY;
        }
    }
}
