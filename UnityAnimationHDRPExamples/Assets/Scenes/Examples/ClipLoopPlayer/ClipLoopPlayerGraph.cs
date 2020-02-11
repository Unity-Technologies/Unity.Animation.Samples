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
    public NodeHandle<DeltaTimeNode>  DeltaTimeNode;
    public NodeHandle<ClipPlayerNode> ClipPlayerNode;
    public NodeHandle<ComponentNode>  EntityNode;
}

[UpdateBefore(typeof(PreAnimationSystemGroup))]
public class ClipLoopPlayerGraphSystem : SampleSystemBase<
    ClipLoopPlayerSetup,
    ClipLoopPlayerData,
    PreAnimationGraphTag,
    PreAnimationGraphSystem
    >
{
    protected override ClipLoopPlayerData CreateGraph(Entity entity, ref Rig rig, PreAnimationGraphSystem graphSystem, ref ClipLoopPlayerSetup setup)
    {
        var set = graphSystem.Set;
        var data = new ClipLoopPlayerData();

        data.DeltaTimeNode = set.Create<DeltaTimeNode>();
        data.ClipPlayerNode = set.Create<ClipPlayerNode>();
        data.EntityNode = set.CreateComponentNode(entity);

        set.SetData(data.ClipPlayerNode, ClipPlayerNode.KernelPorts.Speed, 1.0f);
  
        // Connect kernel ports
        set.Connect(data.DeltaTimeNode, DeltaTimeNode.KernelPorts.DeltaTime, data.ClipPlayerNode, ClipPlayerNode.KernelPorts.DeltaTime);
        set.Connect(data.ClipPlayerNode, ClipPlayerNode.KernelPorts.Output, data.EntityNode);

        // Send messages
        set.SendMessage(data.ClipPlayerNode, ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime });
        set.SendMessage(data.ClipPlayerNode, ClipPlayerNode.SimulationPorts.Rig, rig);
        set.SendMessage(data.ClipPlayerNode, ClipPlayerNode.SimulationPorts.Clip, setup.Clip);

        PostUpdateCommands.AddComponent(entity, graphSystem.Tag);

        return data;
    }

    protected override void DestroyGraph(Entity entity, PreAnimationGraphSystem graphSystem, ref ClipLoopPlayerData data)
    {
        var set = graphSystem.Set;
        set.Destroy(data.DeltaTimeNode);
        set.Destroy(data.ClipPlayerNode);
        set.Destroy(data.EntityNode);
    }
}
