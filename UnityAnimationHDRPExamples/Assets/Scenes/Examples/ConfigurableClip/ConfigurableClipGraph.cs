using Unity.Entities;
using Unity.Animation;
using Unity.DataFlowGraph;
using Unity.Mathematics;
using Unity.Burst;

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
            Clip = ClipBuilder.AnimationClipToDenseClip(Clip),
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
    public NodeHandle<ConfigurableClipNode> ConfigurableClipNode;
    public NodeHandle<RootMotionNode>       RootMotionNode;
    public NodeHandle<CopyRootMotionNode>   CopyRootMotionNode;
    public NodeHandle<ComponentNode>        EntityNode;

    public float ClipTime;

    public bool UpdateConfiguration;
    public bool NormalizedTime;
    public bool LoopTime;
    public bool LoopValues;
    public bool CycleRootMotion;
    public bool InPlace;
    public bool BankPivot;
    public StringHash MotionID;

    public float4x4 RootX;
}

[UpdateBefore(typeof(PreAnimationSystemGroup))]
public class ConfigurableClipGraphSystem : SampleSystemBase<
    ConfigurableClipSetup,
    ConfigurableClipData,
    PreAnimationGraphTag,
    PreAnimationGraphSystem
    >
{
    protected override ConfigurableClipData CreateGraph(Entity entity, ref Rig rig, PreAnimationGraphSystem graphSystem, ref ConfigurableClipSetup setup)
    {
        var set = graphSystem.Set;
        var data = new ConfigurableClipData();
        data.MotionID = setup.MotionID;
        data.UpdateConfiguration = true;

        data.ConfigurableClipNode = set.Create<ConfigurableClipNode>();
        data.RootMotionNode = set.Create<RootMotionNode>();
        data.CopyRootMotionNode = set.Create<CopyRootMotionNode>();
        data.EntityNode = set.CreateComponentNode(entity);

        set.Connect(data.ConfigurableClipNode, ConfigurableClipNode.KernelPorts.Output, data.RootMotionNode, RootMotionNode.KernelPorts.Input);
        set.Connect(data.ConfigurableClipNode, ConfigurableClipNode.KernelPorts.Output, data.EntityNode);

        set.SendMessage(data.ConfigurableClipNode, ConfigurableClipNode.SimulationPorts.Rig, rig);
        set.SendMessage(data.ConfigurableClipNode, ConfigurableClipNode.SimulationPorts.Clip, setup.Clip);
        set.SendMessage(data.RootMotionNode, RootMotionNode.SimulationPorts.Rig, rig);

        set.SetData(data.ConfigurableClipNode, ConfigurableClipNode.KernelPorts.Time, setup.ClipTime);
        set.Connect(data.RootMotionNode, RootMotionNode.KernelPorts.RootX, data.CopyRootMotionNode, CopyRootMotionNode.KernelPorts.InputRootX);
        set.Connect(data.CopyRootMotionNode, CopyRootMotionNode.KernelPorts.Output, data.EntityNode);
        set.Connect(data.EntityNode, data.CopyRootMotionNode, CopyRootMotionNode.KernelPorts.InputClipData, NodeSet.ConnectionType.Feedback);

        PostUpdateCommands.AddComponent(entity, graphSystem.Tag);

        return data;
    }

    protected override void DestroyGraph(Entity entity, PreAnimationGraphSystem graphSystem, ref ConfigurableClipData data)
    {
        var set = graphSystem.Set;
        set.Destroy(data.ConfigurableClipNode);
        set.Destroy(data.RootMotionNode);
        set.Destroy(data.CopyRootMotionNode);
        set.Destroy(data.EntityNode);
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        // Performed on main thread since sending messages from NodeSet and ClipConfiguration changes incur a structural graph change.
        // It's not recommended to do this at runtime, it's mostly shown here to showcase clip configuration features.
        Entities.WithAll<ConfigurableClipSetup, ConfigurableClipData>()
            .ForEach((Entity e, ref ConfigurableClipData data) =>
            {
                m_GraphSystem.Set.SetData(data.ConfigurableClipNode, ConfigurableClipNode.KernelPorts.Time, data.ClipTime);
                if (data.UpdateConfiguration)
                {
                    data.UpdateConfiguration = false;
            
                    var config = new ClipConfiguration { Mask = 0, MotionID = data.InPlace ? data.MotionID : 0 };
                    if (data.NormalizedTime)
                        config.Mask |= ClipConfigurationMask.NormalizedTime;
                    if (data.LoopTime)
                        config.Mask |= ClipConfigurationMask.LoopTime;
                    if (data.LoopValues)
                        config.Mask |= ClipConfigurationMask.LoopValues;
                    if (data.CycleRootMotion)
                        config.Mask |= ClipConfigurationMask.CycleRootMotion;
                    if (data.BankPivot)
                        config.Mask |= ClipConfigurationMask.BankPivot;

                    m_GraphSystem.Set.SendMessage(data.ConfigurableClipNode, ConfigurableClipNode.SimulationPorts.Configuration, config);
                }
            });
    }
}

public class CopyRootMotionNode
    : NodeDefinition<CopyRootMotionNode.Data, CopyRootMotionNode.SimPorts, CopyRootMotionNode.KernelData, CopyRootMotionNode.KernelDefs, CopyRootMotionNode.Kernel>
{
    public struct SimPorts : ISimulationPortDefinition { }

    public struct KernelDefs : IKernelPortDefinition
    {
        public DataInput<CopyRootMotionNode, ConfigurableClipData> InputClipData;
        public DataInput<CopyRootMotionNode, float4x4> InputRootX;

        public DataOutput<CopyRootMotionNode, ConfigurableClipData> Output;
    }

    public struct Data : INodeData { }

    public struct KernelData : IKernelData { }

    [BurstCompile]
    public struct Kernel : IGraphKernel<KernelData, KernelDefs>
    {
        public void Execute(RenderContext ctx, KernelData data, ref KernelDefs ports)
        {
            ref var clipData = ref ctx.Resolve(ref ports.Output);
            clipData = ctx.Resolve(ports.InputClipData);
            clipData.RootX = ctx.Resolve(ports.InputRootX);
        }
    }
}
