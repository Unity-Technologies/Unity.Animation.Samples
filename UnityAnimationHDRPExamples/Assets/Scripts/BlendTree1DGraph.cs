using Unity.Entities;
using Unity.Animation;
using Unity.DataFlowGraph;

#if UNITY_EDITOR
using UnityEditor.Animations;
using Unity.Animation.Editor;

public class BlendTree1DGraph : AnimationGraphBase
{
    public BlendTree BlendTree;

    public override void AddGraphSetupComponent(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var rigDefinition = dstManager.GetComponentData<RigDefinitionSetup>(entity);
        var clipConfiguration = new ClipConfiguration { Mask = (int)ClipConfigurationMask.LoopValues };
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
    public NodeHandle DeltaTimeNode;
    public NodeHandle TimeCounterNode;
    public NodeHandle TimeLoopNode;
    public NodeHandle FloatRcpSimNode;

    public NodeHandle BlendTree;
    public GraphOutput Output;

    public BlobAssetReference<BlendTree1D> BlendTreeAsset;
    public float paramX;
}

[UpdateInGroup(typeof(AnimationSystemGroup))]
[UpdateBefore(typeof(AnimationGraphSystem))]
public class BlendTree1DGraphSystem : SampleSystemBase<BlendTree1DSetup, BlendTree1DData>
{
    protected override BlendTree1DData CreateGraph(Entity entity, NodeSet set, ref BlendTree1DSetup setup)
    {
        if (!EntityManager.HasComponent<SharedRigDefinition>(entity))
        {
            throw new System.NullReferenceException("Entity doesn't have required SharedRigDefinition");
        }

        var rigDefinition = EntityManager.GetSharedComponentData<SharedRigDefinition>(entity);

        var blendTreeComponent = EntityManager.GetBuffer<BlendTree1DResource>(entity);
        var blendTreeAsset = BlendTreeBuilder.CreateBlendTree1DFromComponents(blendTreeComponent[setup.BlendTreeIndex], EntityManager, entity);
        var data = new BlendTree1DData();

        var strongHandle = set.Create<BlendTree1DNode>();

        data.paramX = 0;
        data.BlendTree = strongHandle;
        data.BlendTreeAsset = blendTreeAsset;

        data.DeltaTimeNode = set.Create<DeltaTimeNode>();
        data.TimeCounterNode = set.Create<TimeCounterNode>();
        data.TimeLoopNode = set.Create<TimeLoopNode>();
        data.FloatRcpSimNode = set.Create<FloatRcpSimNode>();
        
        set.Connect(data.DeltaTimeNode, (OutputPortID)DeltaTimeNode.KernelPorts.DeltaTime, data.TimeCounterNode, (InputPortID)TimeCounterNode.KernelPorts.DeltaTime);
        set.Connect(data.TimeCounterNode, (OutputPortID)TimeCounterNode.KernelPorts.Time, data.TimeLoopNode, (InputPortID)TimeLoopNode.KernelPorts.InputTime);
        set.Connect(data.TimeLoopNode, (OutputPortID)TimeLoopNode.KernelPorts.OutputTime, data.BlendTree, (InputPortID)BlendTree1DNode.KernelPorts.NormalizedTime);
        
        set.Connect(data.BlendTree, (OutputPortID)BlendTree1DNode.SimulationPorts.Duration, data.FloatRcpSimNode, (InputPortID)FloatRcpSimNode.SimulationPorts.Input);
        set.Connect(data.FloatRcpSimNode, (OutputPortID)FloatRcpSimNode.SimulationPorts.Output, data.TimeCounterNode, (InputPortID)TimeCounterNode.SimulationPorts.Speed);
        
        set.SendMessage(data.TimeLoopNode, (InputPortID)TimeLoopNode.SimulationPorts.Duration, 1.0F);
    
        set.SendMessage(data.BlendTree, (InputPortID)BlendTree1DNode.SimulationPorts.RigDefinition, rigDefinition.Value);
        set.SendMessage(data.BlendTree, (InputPortID)BlendTree1DNode.SimulationPorts.BlendTree, data.BlendTreeAsset);

        data.Output.Buffer = set.CreateGraphValue(strongHandle, BlendTree1DNode.KernelPorts.Output);
        PostUpdateCommands.AddComponent(entity, data.Output);

        return data;
    }

    protected override void DestroyGraph(Entity entity, NodeSet set, ref BlendTree1DData data)
    {
        set.Destroy(data.DeltaTimeNode);
        set.Destroy(data.TimeCounterNode);
        set.Destroy(data.TimeLoopNode);
        set.Destroy(data.FloatRcpSimNode);
        set.Destroy(data.BlendTree);
        set.ReleaseGraphValue(data.Output.Buffer);
    }

    protected override void UpdateGraph(Entity entity, NodeSet set, ref BlendTree1DData data)
    {
        var param = new Parameter {
            Id = data.BlendTreeAsset.Value.BlendParameter,
            Value = data.paramX
        };
        set.SendMessage(data.BlendTree, (InputPortID)BlendTree1DNode.SimulationPorts.Parameter, param);
    }
}