using Unity.Entities;
using Unity.Animation;
using Unity.DataFlowGraph;

#if UNITY_EDITOR
using UnityEngine;

public class TwoClipsAndMixerGraph : AnimationGraphBase
{
    public AnimationClip Clip1;
    public AnimationClip Clip2;

    public override void AddGraphSetupComponent(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var denseClip1 = ClipBuilder.AnimationClipToDenseClip(Clip1);
        var denseClip2 = ClipBuilder.AnimationClipToDenseClip(Clip2);

        var graphSetup = new TwoClipsAndMixerSetup
        {
            Clip1 = denseClip1,
            Clip2 = denseClip2
        };

        dstManager.AddComponentData(entity, graphSetup);
    }
}
#endif

public struct TwoClipsAndMixerSetup : ISampleSetup
{
    public BlobAssetReference<Clip> Clip1;
    public BlobAssetReference<Clip> Clip2;
};

public struct TwoClipsAndMixerData : ISampleData
{
    public NodeHandle DeltaTimeNode;
    public NodeHandle Clip1Node;
    public NodeHandle Clip2Node;
    public NodeHandle MixerNode;

    public GraphOutput Output;
}

[UpdateInGroup(typeof(AnimationSystemGroup))]
[UpdateBefore(typeof(AnimationGraphSystem))]
public class TwoClipsAndMixerGraphSystem : SampleSystemBase<TwoClipsAndMixerSetup, TwoClipsAndMixerData>
{
    static Unity.Mathematics.Random s_Random = new Unity.Mathematics.Random(0x12345678);

    protected override TwoClipsAndMixerData CreateGraph(Entity entity, NodeSet set, ref TwoClipsAndMixerSetup setup)
    {
        if (!EntityManager.HasComponent<Unity.Animation.SharedRigDefinition>(entity))
        {
            throw new System.NullReferenceException("Entity doesn't have required SharedRigDefinition");
        }

        var rigDefinition = EntityManager.GetSharedComponentData<Unity.Animation.SharedRigDefinition>(entity);
        var clip1 = ClipManager.Instance.GetClipFor(rigDefinition.Value, setup.Clip1);
        var clip2 = ClipManager.Instance.GetClipFor(rigDefinition.Value, setup.Clip2);

        var data = new TwoClipsAndMixerData();

        data.DeltaTimeNode = set.Create<DeltaTimeNode>();
        data.Clip1Node = set.Create<ClipPlayerNode>();
        data.Clip2Node = set.Create<ClipPlayerNode>();
        data.MixerNode = set.Create<MixerNode>();

        // Set constant kernel ports
        set.SendMessage(data.Clip1Node, (InputPortID)ClipPlayerNode.SimulationPorts.Speed, s_Random.NextFloat(0.1f, 1f));
        set.SendMessage(data.Clip2Node, (InputPortID)ClipPlayerNode.SimulationPorts.Speed, s_Random.NextFloat(0.1f, 1f));
 
        // Connect kernel ports
        set.Connect(data.DeltaTimeNode, (OutputPortID)DeltaTimeNode.KernelPorts.DeltaTime, data.Clip1Node, (InputPortID)ClipPlayerNode.KernelPorts.DeltaTime);
        set.Connect(data.DeltaTimeNode, (OutputPortID)DeltaTimeNode.KernelPorts.DeltaTime,  data.Clip2Node, (InputPortID)ClipPlayerNode.KernelPorts.DeltaTime);

        set.Connect(data.Clip1Node, (OutputPortID)ClipPlayerNode.KernelPorts.Output, data.MixerNode, (InputPortID)MixerNode.KernelPorts.Input0);
        set.Connect(data.Clip2Node, (OutputPortID)ClipPlayerNode.KernelPorts.Output, data.MixerNode, (InputPortID)MixerNode.KernelPorts.Input1);
        
        // Send messages
        set.SendMessage(data.Clip1Node, (InputPortID)ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = (int)ClipConfigurationMask.LoopTime });
        set.SendMessage(data.Clip2Node, (InputPortID)ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = (int)ClipConfigurationMask.LoopTime });
        set.SendMessage(data.Clip1Node, (InputPortID)ClipPlayerNode.SimulationPorts.ClipInstance, clip1);
        set.SendMessage(data.Clip2Node, (InputPortID)ClipPlayerNode.SimulationPorts.ClipInstance, clip2);
        set.SendMessage(data.MixerNode, (InputPortID)MixerNode.SimulationPorts.RigDefinition, rigDefinition.Value);
        set.SendMessage(data.MixerNode, (InputPortID)MixerNode.SimulationPorts.Blend, s_Random.NextFloat(0f, 1f));

        data.Output.Buffer = set.CreateGraphValue<Buffer<float>>(data.MixerNode, (OutputPortID)MixerNode.KernelPorts.Output);
        PostUpdateCommands.AddComponent(entity, data.Output);

        return data;
    }

    protected override void DestroyGraph(Entity entity, NodeSet set, ref TwoClipsAndMixerData data)
    {
        set.Destroy(data.DeltaTimeNode);
        set.Destroy(data.Clip1Node);
        set.Destroy(data.Clip2Node);
        set.Destroy(data.MixerNode);
        set.ReleaseGraphValue(data.Output.Buffer);
    }

    protected override void UpdateGraph(Entity entity, NodeSet set, ref TwoClipsAndMixerData data)
    {
    }
}
