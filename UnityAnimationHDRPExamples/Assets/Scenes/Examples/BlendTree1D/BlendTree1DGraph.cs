using Unity.Entities;
using Unity.Animation;
using Unity.DataFlowGraph;
using Unity.Burst;

#if UNITY_EDITOR
using UnityEditor.Animations;
using Unity.Animation.Editor;

public class BlendTree1DGraph : AnimationGraphBase
{
    public BlendTree BlendTree;

    public override void AddGraphSetupComponent(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var rigDefinition = dstManager.GetComponentData<Rig>(entity);
        var clipConfiguration = new ClipConfiguration { Mask = ClipConfigurationMask.LoopValues };
        var bakeOptions = new BakeOptions { RigDefinition = rigDefinition.Value, ClipConfiguration = clipConfiguration, SampleRate = 60.0f };

        var blendTreeIndex = BlendTreeConversion.Convert(BlendTree, entity, dstManager, bakeOptions);

        var graphSetup = new BlendTree1DSetup
        {
            BlendTreeIndex = blendTreeIndex,
        };

        dstManager.AddComponentData(entity, graphSetup);
    }
}
#endif

public struct BlendTree1DSetup : ISampleSetup
{
    public int BlendTreeIndex;
}

public struct BlendTree1DData : ISampleData
{
    public NodeHandle<DeltaTimeNode>                    DeltaTimeNode;
    public NodeHandle<TimeCounterNode>                  TimeCounterNode;
    public NodeHandle<TimeLoopNode>                     TimeLoopNode;
    public NodeHandle<FloatRcpNode>                     FloatRcpNode;
    public NodeHandle<BlendTree1DNode>                  BlendTreeNode;
    public NodeHandle<ExtractBlendTree1DParametersNode> BlendTreeInputNode;
    public NodeHandle<ComponentNode>                    EntityNode;

    public BlobAssetReference<BlendTree1D> BlendTreeAsset;
    public float paramX;
}

[UpdateBefore(typeof(PreAnimationSystemGroup))]
public class BlendTree1DGraphSystem : SampleSystemBase<
    BlendTree1DSetup,
    BlendTree1DData,
    PreAnimationGraphTag,
    PreAnimationGraphSystem
    >
{
    protected override BlendTree1DData CreateGraph(Entity entity, ref Rig rig, PreAnimationGraphSystem graphSystem, ref BlendTree1DSetup setup)
    {
        var set = graphSystem.Set;
        var blendTreeComponent = EntityManager.GetBuffer<BlendTree1DResource>(entity);
        var blendTreeAsset = BlendTreeBuilder.CreateBlendTree1DFromComponents(blendTreeComponent[setup.BlendTreeIndex], EntityManager, entity);
        var data = new BlendTree1DData();

        data.BlendTreeNode = set.Create<BlendTree1DNode>();
        data.BlendTreeAsset = blendTreeAsset;

        data.EntityNode         = set.CreateComponentNode(entity);
        data.DeltaTimeNode      = set.Create<DeltaTimeNode>();
        data.TimeCounterNode    = set.Create<TimeCounterNode>();
        data.TimeLoopNode       = set.Create<TimeLoopNode>();
        data.FloatRcpNode       = set.Create<FloatRcpNode>();
        data.BlendTreeInputNode = set.Create<ExtractBlendTree1DParametersNode>();

        set.Connect(data.DeltaTimeNode, DeltaTimeNode.KernelPorts.DeltaTime, data.TimeCounterNode, TimeCounterNode.KernelPorts.DeltaTime);
        set.Connect(data.TimeCounterNode, TimeCounterNode.KernelPorts.Time, data.TimeLoopNode, TimeLoopNode.KernelPorts.InputTime);
        set.Connect(data.TimeLoopNode, TimeLoopNode.KernelPorts.OutputTime, data.BlendTreeNode, BlendTree1DNode.KernelPorts.NormalizedTime);

        set.Connect(data.BlendTreeNode, BlendTree1DNode.KernelPorts.Duration, data.FloatRcpNode, FloatRcpNode.KernelPorts.Input);
        set.Connect(data.FloatRcpNode, FloatRcpNode.KernelPorts.Output, data.TimeCounterNode, TimeCounterNode.KernelPorts.Speed);

        set.Connect(data.BlendTreeNode, BlendTree1DNode.KernelPorts.Output, data.EntityNode);
        set.Connect(data.EntityNode, data.BlendTreeInputNode, ExtractBlendTree1DParametersNode.KernelPorts.Input, NodeSet.ConnectionType.Feedback);
        set.Connect(data.BlendTreeInputNode, ExtractBlendTree1DParametersNode.KernelPorts.Output, data.BlendTreeNode, BlendTree1DNode.KernelPorts.BlendParameter);

        set.SendMessage(data.TimeLoopNode, TimeLoopNode.SimulationPorts.Duration, 1.0F);
        set.SendMessage(data.BlendTreeNode, BlendTree1DNode.SimulationPorts.Rig, rig);
        set.SendMessage(data.BlendTreeNode, BlendTree1DNode.SimulationPorts.BlendTree, data.BlendTreeAsset);

        PostUpdateCommands.AddComponent(entity, graphSystem.Tag);

        return data;
    }

    protected override void DestroyGraph(Entity entity, PreAnimationGraphSystem graphSystem, ref BlendTree1DData data)
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

public class ExtractBlendTree1DParametersNode
    : NodeDefinition<ExtractBlendTree1DParametersNode.Data, ExtractBlendTree1DParametersNode.SimPorts, ExtractBlendTree1DParametersNode.KernelData, ExtractBlendTree1DParametersNode.KernelDefs, ExtractBlendTree1DParametersNode.Kernel>
{
    public struct SimPorts : ISimulationPortDefinition { }

    public struct KernelDefs : IKernelPortDefinition
    {
        public DataInput<ExtractBlendTree1DParametersNode, BlendTree1DData> Input;
        public DataOutput<ExtractBlendTree1DParametersNode, float> Output;
    }

    public struct Data : INodeData { }

    public struct KernelData : IKernelData { }

    [BurstCompile]
    public struct Kernel : IGraphKernel<KernelData, KernelDefs>
    {
        public void Execute(RenderContext ctx, KernelData data, ref KernelDefs ports)
        {
            ctx.Resolve(ref ports.Output) = ctx.Resolve(ports.Input).paramX;
        }
    }
}