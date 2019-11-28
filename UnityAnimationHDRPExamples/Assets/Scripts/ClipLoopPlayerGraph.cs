using Unity.Entities;
using Unity.Animation;
using Unity.DataFlowGraph;

#if UNITY_EDITOR
using UnityEngine;

public class ClipLoopPlayerGraph : AnimationGraphBase
{
    public AnimationClip Clip;

    public override void AddGraphSetupComponent(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var graphSetup = new ClipLoopPlayerSetup
        {
            Clip = ClipBuilder.AnimationClipToDenseClip(Clip)
        };

        dstManager.AddComponentData(entity, graphSetup);
    }
}
#endif

public struct ClipLoopPlayerSetup : ISampleSetup
{
    public BlobAssetReference<Clip> Clip;
};

public struct ClipLoopPlayerData : ISampleData
{
    public NodeHandle DeltaTimeNode;
    public NodeHandle ClipPlayerNode;

    public GraphOutput Output;
}

[UpdateInGroup(typeof(AnimationSystemGroup))]
[UpdateBefore(typeof(AnimationGraphSystem))]
public class ClipLoopPlayerGraphSystem : SampleSystemBase<ClipLoopPlayerSetup, ClipLoopPlayerData>
{
    static Unity.Mathematics.Random s_Random = new Unity.Mathematics.Random(0x12345678);

    protected override ClipLoopPlayerData CreateGraph(Entity entity, NodeSet set, ref ClipLoopPlayerSetup setup)
    {
        if (!EntityManager.HasComponent<SharedRigDefinition>(entity))
        {
            throw new System.NullReferenceException("Entity doesn't have required SharedRigDefinition");
        }

        var rigDefinition = EntityManager.GetSharedComponentData<SharedRigDefinition>(entity);
        var clip = ClipManager.Instance.GetClipFor(rigDefinition.Value, setup.Clip);

        var data = new ClipLoopPlayerData();

        data.DeltaTimeNode = set.Create<DeltaTimeNode>();
        data.ClipPlayerNode = set.Create<ClipPlayerNode>();

        set.SendMessage(data.ClipPlayerNode, (InputPortID)ClipPlayerNode.SimulationPorts.Speed, 1.0f);
  
        // Connect kernel ports
        set.Connect(data.DeltaTimeNode, (OutputPortID)DeltaTimeNode.KernelPorts.DeltaTime, data.ClipPlayerNode, (InputPortID)ClipPlayerNode.KernelPorts.DeltaTime);

        // Send messages
        set.SendMessage(data.ClipPlayerNode, (InputPortID)ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = (int)ClipConfigurationMask.LoopTime });
        set.SendMessage(data.ClipPlayerNode, (InputPortID)ClipPlayerNode.SimulationPorts.ClipInstance, clip);

        data.Output.Buffer = set.CreateGraphValue<Buffer<float>>(data.ClipPlayerNode, (OutputPortID)ClipPlayerNode.KernelPorts.Output);
        PostUpdateCommands.AddComponent(entity, data.Output);

        return data;
    }

    protected override void DestroyGraph(Entity entity, NodeSet set, ref ClipLoopPlayerData data)
    {
        set.Destroy(data.DeltaTimeNode);
        set.Destroy(data.ClipPlayerNode);
        set.ReleaseGraphValue(data.Output.Buffer);
    }

    protected override void UpdateGraph(Entity entity, NodeSet set, ref ClipLoopPlayerData data)
    {
    }
}
