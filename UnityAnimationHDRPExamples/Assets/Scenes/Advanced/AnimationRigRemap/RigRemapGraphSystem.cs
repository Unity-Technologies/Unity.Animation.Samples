using Unity.Entities;
using Unity.Animation;
using Unity.DataFlowGraph;
using Unity.Mathematics;
using Unity.Transforms;

public struct RigRemapSetup : ISampleSetup
{
    public BlobAssetReference<Clip>          SrcClip;
    public BlobAssetReference<RigDefinition> SrcRig;
    public BlobAssetReference<RigRemapTable> RemapTable;
};

public struct RigRemapData : ISampleData
{
    public NodeHandle<ConvertDeltaTimeToFloatNode> DeltaTimeNode;
    public NodeHandle<ClipPlayerNode>  ClipPlayerNode;
    public NodeHandle<RigRemapperNode> RemapperNode;

    public NodeHandle<ComponentNode>   EntityNode;
    public NodeHandle<ComponentNode>   DebugEntityNode;
}

[UpdateBefore(typeof(DefaultAnimationSystemGroup))]
public class RigRemapGraphSystem : SampleSystemBase<
    RigRemapSetup,
    RigRemapData,
    ProcessDefaultAnimationGraph
>
{
    protected override RigRemapData CreateGraph(Entity entity, ref Rig rig, ProcessDefaultAnimationGraph graphSystem, ref RigRemapSetup setup)
    {
        var set = graphSystem.Set;
        var debugEntity = RigUtils.InstantiateDebugRigEntity(
            setup.SrcRig,
            EntityManager,
            new BoneRendererProperties { BoneShape = BoneRendererUtils.BoneShape.Line, Color = new float4(0f, 1f, 0f, 0.5f), Size = 1f }
        );

        PostUpdateCommands.AddComponent(debugEntity, new LocalToParent { Value = float4x4.identity });
        PostUpdateCommands.AddComponent(debugEntity, new Parent { Value = entity });

        var data = new RigRemapData();

        data.DeltaTimeNode   = set.Create<ConvertDeltaTimeToFloatNode>();
        data.ClipPlayerNode  = set.Create<ClipPlayerNode>();
        data.RemapperNode    = set.Create<RigRemapperNode>();
        data.EntityNode      = set.CreateComponentNode(entity);
        data.DebugEntityNode = set.CreateComponentNode(debugEntity);

        set.SetData(data.ClipPlayerNode, ClipPlayerNode.KernelPorts.Speed, 1.0f);

        // Connect kernel ports
        set.Connect(data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, data.ClipPlayerNode, ClipPlayerNode.KernelPorts.DeltaTime);
        set.Connect(data.ClipPlayerNode, ClipPlayerNode.KernelPorts.Output, data.RemapperNode, RigRemapperNode.KernelPorts.Input);

        // Connect EntityNode
        set.Connect(data.EntityNode, data.DeltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Input);
        set.Connect(data.RemapperNode, RigRemapperNode.KernelPorts.Output, data.EntityNode, NodeSet.ConnectionType.Feedback);

        // Connect DebugEntityNode
        set.Connect(data.ClipPlayerNode, ClipPlayerNode.KernelPorts.Output, data.DebugEntityNode);

        // Send messages
        set.SendMessage(data.ClipPlayerNode, ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime });
        set.SendMessage(data.ClipPlayerNode, ClipPlayerNode.SimulationPorts.Rig, new Rig { Value = setup.SrcRig });
        set.SendMessage(data.ClipPlayerNode, ClipPlayerNode.SimulationPorts.Clip, setup.SrcClip);
        set.SendMessage(data.RemapperNode, RigRemapperNode.SimulationPorts.SourceRig, new Rig { Value = setup.SrcRig });
        set.SendMessage(data.RemapperNode, RigRemapperNode.SimulationPorts.DestinationRig, rig);
        set.SendMessage(data.RemapperNode, RigRemapperNode.SimulationPorts.RemapTable, setup.RemapTable);

        return data;
    }

    protected override void DestroyGraph(Entity entity, ProcessDefaultAnimationGraph graphSystem, ref RigRemapData data)
    {
        var set = graphSystem.Set;
        set.Destroy(data.DeltaTimeNode);
        set.Destroy(data.ClipPlayerNode);
        set.Destroy(data.RemapperNode);
        set.Destroy(data.EntityNode);
        set.Destroy(data.DebugEntityNode);
    }
}
