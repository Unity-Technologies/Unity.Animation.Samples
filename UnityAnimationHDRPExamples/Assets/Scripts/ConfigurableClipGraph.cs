using Unity.Entities;
using Unity.Animation;
using Unity.DataFlowGraph;
using Unity.Mathematics;

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
    public NodeHandle ConfigurableClipNode;
    public NodeHandle RootMotionNode;
    public GraphValue<RigidTransform> RootXValue;
    public GraphOutput Output;

    public float ClipTime;

    public bool UpdateConfiguration;
    public bool NormalizedTime;
    public bool LoopTime;
    public bool LoopValues;
    public bool CycleRootMotion;
    public bool InPlace;
    public bool BankPivot;
    public StringHash MotionID;

    public RigidTransform RootX;
}

[UpdateInGroup(typeof(AnimationSystemGroup))]
[UpdateBefore(typeof(AnimationGraphSystem))]
public class ConfigurableClipGraphSystem : SampleSystemBase<ConfigurableClipSetup, ConfigurableClipData>
{
    protected override ConfigurableClipData CreateGraph(Entity entity, NodeSet set, ref ConfigurableClipSetup setup)
    {
        if (!EntityManager.HasComponent<SharedRigDefinition>(entity))
        {
            throw new System.NullReferenceException("Entity doesn't have required SharedRigDefinition");
        }

        var rigDefinition = EntityManager.GetSharedComponentData<SharedRigDefinition>(entity);
        var clip = ClipManager.Instance.GetClipFor(rigDefinition.Value, setup.Clip);

        var data = new ConfigurableClipData();
        data.MotionID = setup.MotionID;
        data.UpdateConfiguration = true;

        data.ConfigurableClipNode = set.Create<ConfigurableClipNode>();
        data.RootMotionNode = set.Create<RootMotionNode>();
        
        set.Connect(data.ConfigurableClipNode, (OutputPortID)ConfigurableClipNode.KernelPorts.Output, data.RootMotionNode, (InputPortID)RootMotionNode.KernelPorts.Input);
        
        set.SendMessage(data.ConfigurableClipNode, (InputPortID)ConfigurableClipNode.SimulationPorts.ClipInstance, clip);
        set.SendMessage(data.RootMotionNode, (InputPortID)RootMotionNode.SimulationPorts.RigDefinition, rigDefinition.Value);

        set.SetData(data.ConfigurableClipNode, (InputPortID)ConfigurableClipNode.KernelPorts.Time, setup.ClipTime);

        data.RootXValue = set.CreateGraphValue<RigidTransform>(data.RootMotionNode, (OutputPortID)RootMotionNode.KernelPorts.RootX);
        data.Output.Buffer = set.CreateGraphValue<Buffer<float>>(data.ConfigurableClipNode, (OutputPortID)ConfigurableClipNode.KernelPorts.Output);
        PostUpdateCommands.AddComponent(entity, data.Output);

        return data;
    }

    protected override void DestroyGraph(Entity entity, NodeSet set, ref ConfigurableClipData data)
    {
        set.Destroy(data.ConfigurableClipNode);
        set.Destroy(data.RootMotionNode);
        set.ReleaseGraphValue(data.RootXValue);
        set.ReleaseGraphValue(data.Output.Buffer);
    }

    protected override void UpdateGraph(Entity entity, NodeSet set, ref ConfigurableClipData data)
    {
        data.RootX = set.GetValueBlocking(data.RootXValue);
        
        set.SetData(data.ConfigurableClipNode, (InputPortID)ConfigurableClipNode.KernelPorts.Time, data.ClipTime);

        if (data.UpdateConfiguration)
        {
            data.UpdateConfiguration = false;
            
            var mask = 0;
            if (data.NormalizedTime)
                mask |= (int)ClipConfigurationMask.NormalizedTime;
            if (data.LoopTime)
                mask |= (int)ClipConfigurationMask.LoopTime;
            if (data.LoopValues)
                mask |= (int)ClipConfigurationMask.LoopValues;
            if (data.CycleRootMotion)
                mask |= (int)ClipConfigurationMask.CycleRootMotion;
            if (data.BankPivot)
                mask |= (int)ClipConfigurationMask.BankPivot;
            
            var config = new ClipConfiguration { Mask = mask, MotionID = data.InPlace ? data.MotionID : 0 };
            
            set.SendMessage(data.ConfigurableClipNode, (InputPortID)ConfigurableClipNode.SimulationPorts.Configuration, config);
        }
    }
}
