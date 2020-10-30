using Unity.Entities;
using Unity.Animation;
using Unity.DataFlowGraph;

#if UNITY_EDITOR
using Unity.Animation.Hybrid;
using UnityEngine;

public class ConfigurableClipGraph : AnimationGraphBase
{
    public AnimationClip Clip;
    public string MotionName;
    public float ClipTimeInit;

    private StringHash m_MotionId;

    public override void PreProcessData<T>(T data)
    {
        if (data is RigComponent)
        {
            var rig = data as RigComponent;

            for (var boneIter = 0; boneIter < rig.Bones.Length; boneIter++)
            {
                if (MotionName == rig.Bones[boneIter].name)
                {
                    m_MotionId = RigGenerator.ComputeRelativePath(rig.Bones[boneIter], rig.transform);
                }
            }
        }
    }

    public override void AddGraphSetupComponent(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var graphSetup = new ConfigurableClipSetup
        {
            Clip = Clip.ToDenseClip(),
            ClipTime = ClipTimeInit,
            MotionID = m_MotionId
        };

        dstManager.AddComponentData(entity, graphSetup);
    }
}
#endif

public struct ConfigurableClipSetup : ISampleSetup
{
    public BlobAssetReference<Clip> Clip;
    public float ClipTime;
    public StringHash MotionID;
}

public struct ConfigurableClipData : ISampleData
{
    public GraphHandle Graph;
    public NodeHandle<ConfigurableClipNode> ConfigurableClipNode;

    public float ClipTime;

    public bool UpdateConfiguration;
    public bool InPlace;

    public ClipConfigurationMask ClipOptions;
    public StringHash MotionID;
}

[UpdateBefore(typeof(DefaultAnimationSystemGroup))]
public class ConfigurableClipGraphSystem : SampleSystemBase<
    ConfigurableClipSetup,
    ConfigurableClipData,
    ProcessDefaultAnimationGraph
>
{
    protected override ConfigurableClipData CreateGraph(Entity entity, ref Rig rig, ProcessDefaultAnimationGraph graphSystem, ref ConfigurableClipSetup setup)
    {
        var data = new ConfigurableClipData();

        data.Graph = graphSystem.CreateGraph();
        data.MotionID = setup.MotionID;
        data.UpdateConfiguration = true;
        data.ConfigurableClipNode = graphSystem.CreateNode<ConfigurableClipNode>(data.Graph);

        var entityNode = graphSystem.CreateNode(data.Graph, entity);

        var set = graphSystem.Set;
        set.Connect(data.ConfigurableClipNode, ConfigurableClipNode.KernelPorts.Output, entityNode);

        set.SendMessage(data.ConfigurableClipNode, ConfigurableClipNode.SimulationPorts.Rig, rig);
        set.SendMessage(data.ConfigurableClipNode, ConfigurableClipNode.SimulationPorts.Clip, setup.Clip);
        set.SetData(data.ConfigurableClipNode, ConfigurableClipNode.KernelPorts.Time, setup.ClipTime);

        return data;
    }

    protected override void DestroyGraph(Entity entity, ProcessDefaultAnimationGraph graphSystem, ref ConfigurableClipData data)
    {
        graphSystem.Dispose(data.Graph);
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        // Performed on main thread since sending messages from NodeSet and ClipConfiguration changes incur a structural graph change.
        // It's not recommended to do this at runtime, it's mostly shown here to showcase clip configuration features.
        Entities.WithAll<ConfigurableClipSetup, ConfigurableClipData>()
            .ForEach((Entity e, ref ConfigurableClipData data) =>
            {
                m_AnimationSystem.Set.SetData(data.ConfigurableClipNode, ConfigurableClipNode.KernelPorts.Time, data.ClipTime);
                if (data.UpdateConfiguration)
                {
                    var config = new ClipConfiguration { Mask = data.ClipOptions, MotionID = data.InPlace ? data.MotionID : 0 };
                    m_AnimationSystem.Set.SendMessage(data.ConfigurableClipNode, ConfigurableClipNode.SimulationPorts.Configuration, config);
                    data.UpdateConfiguration = false;
                }
            });
    }
}
