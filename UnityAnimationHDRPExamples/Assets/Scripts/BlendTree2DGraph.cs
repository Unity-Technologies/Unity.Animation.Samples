using Unity.Entities;
using Unity.Animation;
using Unity.DataFlowGraph;

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
    }
}
#endif

public struct BlendTree2DSetup : ISampleSetup
{
    public int BlendTreeIndex;
}

public struct BlendTree2DData : ISampleData
{
    public NodeHandle DeltaTimeNode;
    public NodeHandle TimeCounterNode;
    public NodeHandle TimeLoopNode;
    public NodeHandle FloatRcpSimNode;

    public NodeHandle BlendTree;
    public GraphOutput Output;

    public BlobAssetReference<BlendTree2DSimpleDirectionnal> BlendTreeAsset;
    public float paramX;
    public float paramY;
}

[UpdateInGroup(typeof(AnimationSystemGroup))]
[UpdateBefore(typeof(AnimationGraphSystem))]
public class BlendTree2DGraphSystem : SampleSystemBase<BlendTree2DSetup, BlendTree2DData>
{
    Unity.Mathematics.Random rand = new Unity.Mathematics.Random(0x12345678);

    protected override BlendTree2DData CreateGraph(Entity entity, NodeSet set, ref BlendTree2DSetup setup)
    {
        if (!EntityManager.HasComponent<SharedRigDefinition>(entity))
        {
            throw new System.NullReferenceException("Entity doesn't have required SharedRigDefinition");
        }

        var rigDefinition = EntityManager.GetSharedComponentData<SharedRigDefinition>(entity);

        var blendTreeComponent = EntityManager.GetBuffer<BlendTree2DResource>(entity);
        var blendTreeAsset = BlendTreeBuilder.CreateBlendTree2DFromComponents(blendTreeComponent[setup.BlendTreeIndex], EntityManager, entity);
        var data = new BlendTree2DData();

        var strongHandle = set.Create<BlendTree2DNode>();

        data.paramX = rand.NextFloat(-1.0f, 1.0f);
        data.paramY = rand.NextFloat(-1.0f, 1.0f);
        data.BlendTree = strongHandle;
        data.BlendTreeAsset = blendTreeAsset;

        data.DeltaTimeNode = set.Create<DeltaTimeNode>();
        data.TimeCounterNode = set.Create<TimeCounterNode>();
        data.TimeLoopNode = set.Create<TimeLoopNode>();
        data.FloatRcpSimNode = set.Create<FloatRcpSimNode>();
        
        set.Connect(data.DeltaTimeNode, (OutputPortID)DeltaTimeNode.KernelPorts.DeltaTime, data.TimeCounterNode, (InputPortID)TimeCounterNode.KernelPorts.DeltaTime);
        set.Connect(data.TimeCounterNode, (OutputPortID)TimeCounterNode.KernelPorts.Time, data.TimeLoopNode, (InputPortID)TimeLoopNode.KernelPorts.InputTime);
        set.Connect(data.TimeLoopNode, (OutputPortID)TimeLoopNode.KernelPorts.OutputTime, data.BlendTree, (InputPortID)BlendTree2DNode.KernelPorts.NormalizedTime);
        
        set.Connect(data.BlendTree, (OutputPortID)BlendTree2DNode.SimulationPorts.Duration, data.FloatRcpSimNode, (InputPortID)FloatRcpSimNode.SimulationPorts.Input);
        set.Connect(data.FloatRcpSimNode, (OutputPortID)FloatRcpSimNode.SimulationPorts.Output, data.TimeCounterNode, (InputPortID)TimeCounterNode.SimulationPorts.Speed);
        
        set.SendMessage(data.TimeLoopNode, (InputPortID)TimeLoopNode.SimulationPorts.Duration, 1.0F);
    
        set.SendMessage(data.BlendTree, (InputPortID)BlendTree2DNode.SimulationPorts.RigDefinition, rigDefinition.Value);
        set.SendMessage(data.BlendTree, (InputPortID)BlendTree2DNode.SimulationPorts.BlendTree, data.BlendTreeAsset);

        data.Output.Buffer = set.CreateGraphValue(strongHandle, BlendTree2DNode.KernelPorts.Output);
        PostUpdateCommands.AddComponent(entity, data.Output);

        return data;
    }

    protected override void DestroyGraph(Entity entity, NodeSet set, ref BlendTree2DData data)
    {
        set.Destroy(data.DeltaTimeNode);
        set.Destroy(data.TimeCounterNode);
        set.Destroy(data.TimeLoopNode);
        set.Destroy(data.FloatRcpSimNode);
        set.Destroy(data.BlendTree);
        set.ReleaseGraphValue(data.Output.Buffer);
    }

    protected override void UpdateGraph(Entity entity, NodeSet set, ref BlendTree2DData data)
    {
        var paramX = new Parameter {
            Id = data.BlendTreeAsset.Value.BlendParameterX,
            Value = data.paramX,
        };
        set.SendMessage(data.BlendTree, (InputPortID)BlendTree2DNode.SimulationPorts.Parameter, paramX);

        var paramY = new Parameter {
            Id = data.BlendTreeAsset.Value.BlendParameterY,
            Value = data.paramY,
        };
        set.SendMessage(data.BlendTree, (InputPortID)BlendTree2DNode.SimulationPorts.Parameter, paramY);
    }
}