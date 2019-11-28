using Unity.Animation;
using Unity.Animation.Hybrid;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

#if UNITY_EDITOR
using UnityEngine;

public class AnimationControllerGraph : AnimationGraphBase
{
    public AnimationClip WalkShortLeftClip;
    public AnimationClip WalkLongLeftClip;
    public AnimationClip WalkStraightClip;
    public AnimationClip WalkLongRightClip;
    public AnimationClip WalkShortRightClip;

    public AnimationClip JogShortLeftClip;
    public AnimationClip JogLongLeftClip;
    public AnimationClip JogStraightClip;
    public AnimationClip JogLongRightClip;
    public AnimationClip JogShortRightClip;

    public string MotionName;
    public bool Bake;
    public float SampleRate = 60.0f;
    public bool LoopValues;
    public bool BankPivot;

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
        var walkShortLeftClip  = ClipBuilder.AnimationClipToDenseClip(WalkShortLeftClip);
        var walkLongLeftClip   = ClipBuilder.AnimationClipToDenseClip(WalkLongLeftClip);
        var walkStraightClip   = ClipBuilder.AnimationClipToDenseClip(WalkStraightClip);
        var walkLongRightClip  = ClipBuilder.AnimationClipToDenseClip(WalkLongRightClip);
        var walkShortRightClip = ClipBuilder.AnimationClipToDenseClip(WalkShortRightClip);

        var jogShortLeftClip   = ClipBuilder.AnimationClipToDenseClip(JogShortLeftClip);
        var jogLongLeftClip    = ClipBuilder.AnimationClipToDenseClip(JogLongLeftClip);
        var jogStraightClip    = ClipBuilder.AnimationClipToDenseClip(JogStraightClip);
        var jogLongRightClip   = ClipBuilder.AnimationClipToDenseClip(JogLongRightClip);
        var jogShortRightClip  = ClipBuilder.AnimationClipToDenseClip(JogShortRightClip);
        
        var clipConfiguration = new ClipConfiguration();

        clipConfiguration.Mask = 0;
        clipConfiguration.Mask |= (int)ClipConfigurationMask.LoopTime;
        clipConfiguration.Mask |= LoopValues ? (int)ClipConfigurationMask.LoopValues : 0;
        clipConfiguration.Mask |= (int)ClipConfigurationMask.CycleRootMotion;
        clipConfiguration.Mask |= (int)ClipConfigurationMask.DeltaRootMotion;
        clipConfiguration.Mask |= BankPivot ? (int)ClipConfigurationMask.BankPivot : 0;

        clipConfiguration.MotionID = m_MotionId;

        var graphSetup = new AnimationControllerSetup
        {
            WalkShortLeftClip = walkShortLeftClip,
            WalkLongLeftClip = walkLongLeftClip,
            WalkStraightClip = walkStraightClip,
            WalkLongRightClip = walkLongRightClip,
            WalkShortRightClip = walkShortRightClip,

            JogShortLeftClip = jogShortLeftClip,
            JogLongLeftClip = jogLongLeftClip,
            JogStraightClip = jogStraightClip,
            JogLongRightClip = jogLongRightClip,
            JogShortRightClip = jogShortRightClip,
        };
        
        if (Bake)
        {
            var rigDefinition = dstManager.GetComponentData<RigDefinitionSetup>(entity);

            var walkShortLeftClipInstance = ClipInstance.Create(rigDefinition.Value, walkShortLeftClip);
            var walkLongLeftClipInstance = ClipInstance.Create(rigDefinition.Value, walkLongLeftClip);
            var walkStraightClipInstance = ClipInstance.Create(rigDefinition.Value, walkStraightClip);
            var walkLongRightClipInstance = ClipInstance.Create(rigDefinition.Value, walkLongRightClip);
            var walkShortRightClipInstance = ClipInstance.Create(rigDefinition.Value, walkShortRightClip);

            var jogShortLeftClipInstance = ClipInstance.Create(rigDefinition.Value, jogShortLeftClip);
            var jogLongLeftClipInstance = ClipInstance.Create(rigDefinition.Value, jogLongLeftClip);
            var jogStraightClipInstance = ClipInstance.Create(rigDefinition.Value, jogStraightClip);
            var jogLongRightClipInstance = ClipInstance.Create(rigDefinition.Value, jogLongRightClip);
            var jogShortRightClipInstance = ClipInstance.Create(rigDefinition.Value, jogShortRightClip);

            graphSetup.WalkShortLeftClip = UberClipNode.Bake(walkShortLeftClipInstance, clipConfiguration, SampleRate);
            graphSetup.WalkLongLeftClip = UberClipNode.Bake(walkLongLeftClipInstance, clipConfiguration, SampleRate);
            graphSetup.WalkStraightClip = UberClipNode.Bake(walkStraightClipInstance, clipConfiguration, SampleRate);
            graphSetup.WalkLongRightClip = UberClipNode.Bake(walkLongRightClipInstance, clipConfiguration, SampleRate);
            graphSetup.WalkShortRightClip = UberClipNode.Bake(walkShortRightClipInstance, clipConfiguration, SampleRate);

            graphSetup.JogShortLeftClip = UberClipNode.Bake(jogShortLeftClipInstance, clipConfiguration, SampleRate);
            graphSetup.JogLongLeftClip = UberClipNode.Bake(jogLongLeftClipInstance, clipConfiguration, SampleRate);
            graphSetup.JogStraightClip = UberClipNode.Bake(jogStraightClipInstance, clipConfiguration, SampleRate);
            graphSetup.JogLongRightClip = UberClipNode.Bake(jogLongRightClipInstance, clipConfiguration, SampleRate);
            graphSetup.JogShortRightClip = UberClipNode.Bake(jogShortRightClipInstance, clipConfiguration, SampleRate);
            
            walkShortLeftClipInstance.Dispose();
            walkLongLeftClipInstance.Dispose();
            walkStraightClipInstance.Dispose();
            walkLongRightClipInstance.Dispose();
            walkShortRightClipInstance.Dispose();

            jogShortLeftClipInstance.Dispose();
            jogLongLeftClipInstance.Dispose();
            jogStraightClipInstance.Dispose();
            jogLongRightClipInstance.Dispose();
            jogShortRightClipInstance.Dispose();
            
            clipConfiguration.Mask = 0;
            clipConfiguration.Mask |= (int)ClipConfigurationMask.NormalizedTime;
            clipConfiguration.Mask |= (int)ClipConfigurationMask.LoopTime;
            clipConfiguration.Mask |= (int)ClipConfigurationMask.RootMotionFromVelocity;
            clipConfiguration.MotionID = 0;
        }
        else
        {
            clipConfiguration.Mask |= (int)ClipConfigurationMask.NormalizedTime;
        }

        graphSetup.Configuration = clipConfiguration;
        
        dstManager.AddComponentData(entity, graphSetup);
    }
}
#endif

public struct AnimationControllerSetup : ISampleSetup
{
    public BlobAssetReference<Clip> WalkShortLeftClip;
    public BlobAssetReference<Clip> WalkLongLeftClip;
    public BlobAssetReference<Clip> WalkStraightClip;
    public BlobAssetReference<Clip> WalkLongRightClip;
    public BlobAssetReference<Clip> WalkShortRightClip;

    public BlobAssetReference<Clip> JogShortLeftClip;
    public BlobAssetReference<Clip> JogLongLeftClip;
    public BlobAssetReference<Clip> JogStraightClip;
    public BlobAssetReference<Clip> JogLongRightClip;
    public BlobAssetReference<Clip> JogShortRightClip;

    public ClipConfiguration Configuration;
}

public struct AnimationControllerData : ISampleData
{
    public NodeHandle DeltaTimeNode;
    public NodeHandle TimeCounterNode;
    public NodeHandle MixerWalkNode;
    public NodeHandle MixerJogNode;
    public NodeHandle MixerSpeedNode;
    public NodeHandle RootMotionNode;

    public GraphValue<RigidTransform> RootXValue;
    public GraphOutput Output;

    public RigidTransform FollowX;

    public float Direction;
    public float DirectionDamped;
    public float Speed;
    public float SpeedDamped;

    public int Player;
}

[UpdateInGroup(typeof(AnimationSystemGroup))]
[UpdateBefore(typeof(AnimationGraphSystem))]
public class AnimationControllerGraphSystem : SampleSystemBase<AnimationControllerSetup, AnimationControllerData>
{
    protected override AnimationControllerData CreateGraph(Entity entity, NodeSet set, ref AnimationControllerSetup setup)
    {
        if (!EntityManager.HasComponent<SharedRigDefinition>(entity))
        {
            throw new System.NullReferenceException("Entity doesn't have required SharedRigDefinition");
        }

        var rigDefinition = EntityManager.GetSharedComponentData<SharedRigDefinition>(entity);

        var walkShortLeftClipInstance = ClipManager.Instance.GetClipFor(rigDefinition.Value, setup.WalkShortLeftClip);
        var walkLongLeftClipInstance = ClipManager.Instance.GetClipFor(rigDefinition.Value, setup.WalkLongLeftClip);
        var walkStraightClipInstance = ClipManager.Instance.GetClipFor(rigDefinition.Value, setup.WalkStraightClip);
        var walkLongRightClipInstance = ClipManager.Instance.GetClipFor(rigDefinition.Value, setup.WalkLongRightClip);
        var walkShortRightClipInstance = ClipManager.Instance.GetClipFor(rigDefinition.Value, setup.WalkShortRightClip);

        var jogShortLeftClipInstance = ClipManager.Instance.GetClipFor(rigDefinition.Value, setup.JogShortLeftClip);
        var jogLongLeftClipInstance = ClipManager.Instance.GetClipFor(rigDefinition.Value, setup.JogLongLeftClip);
        var jogStraightClipInstance = ClipManager.Instance.GetClipFor(rigDefinition.Value, setup.JogStraightClip);
        var jogLongRightClipInstance = ClipManager.Instance.GetClipFor(rigDefinition.Value, setup.JogLongRightClip);
        var jogShortRightClipInstance = ClipManager.Instance.GetClipFor(rigDefinition.Value, setup.JogShortRightClip);
        
        var data = new AnimationControllerData();

        data.DeltaTimeNode   = set.Create<DeltaTimeNode>();
        data.TimeCounterNode = set.Create<TimeCounterNode>();
        data.MixerWalkNode   = set.Create<DirectionMixerNode>();
        data.MixerJogNode    = set.Create<DirectionMixerNode>();
        data.MixerSpeedNode  = set.Create<MixerNode>();
        data.RootMotionNode  = set.Create<RootMotionNode>();

        data.Direction = 2.0f;
        data.Speed = 0.0f;

        set.Connect(data.DeltaTimeNode, (OutputPortID)DeltaTimeNode.KernelPorts.DeltaTime, data.TimeCounterNode, (InputPortID)TimeCounterNode.KernelPorts.DeltaTime);
        set.Connect(data.TimeCounterNode, (OutputPortID)TimeCounterNode.KernelPorts.OutputDeltaTime, data.MixerWalkNode, (InputPortID)DirectionMixerNode.KernelPorts.DeltaTime);
        set.Connect(data.TimeCounterNode, (OutputPortID)TimeCounterNode.KernelPorts.Time, data.MixerWalkNode, (InputPortID)DirectionMixerNode.KernelPorts.Time);
        set.Connect(data.TimeCounterNode, (OutputPortID)TimeCounterNode.KernelPorts.OutputDeltaTime, data.MixerJogNode, (InputPortID)DirectionMixerNode.KernelPorts.DeltaTime);
        set.Connect(data.TimeCounterNode, (OutputPortID)TimeCounterNode.KernelPorts.Time, data.MixerJogNode, (InputPortID)DirectionMixerNode.KernelPorts.Time);

        set.Connect(data.MixerWalkNode, (OutputPortID)DirectionMixerNode.KernelPorts.Output, data.MixerSpeedNode, (InputPortID)MixerNode.KernelPorts.Input0);
        set.Connect(data.MixerJogNode, (OutputPortID)DirectionMixerNode.KernelPorts.Output, data.MixerSpeedNode, (InputPortID)MixerNode.KernelPorts.Input1);
        set.Connect(data.MixerSpeedNode, (OutputPortID)MixerNode.KernelPorts.Output, data.RootMotionNode, (InputPortID)RootMotionNode.KernelPorts.Input);

        set.SendMessage(data.MixerWalkNode, (InputPortID)DirectionMixerNode.SimulationPorts.ClipConfiguration, setup.Configuration);
        set.SendMessage(data.MixerWalkNode, (InputPortID)DirectionMixerNode.SimulationPorts.Clip0, walkShortLeftClipInstance);
        set.SendMessage(data.MixerWalkNode, (InputPortID)DirectionMixerNode.SimulationPorts.Clip1, walkLongLeftClipInstance);
        set.SendMessage(data.MixerWalkNode, (InputPortID)DirectionMixerNode.SimulationPorts.Clip2, walkStraightClipInstance);
        set.SendMessage(data.MixerWalkNode, (InputPortID)DirectionMixerNode.SimulationPorts.Clip3, walkLongRightClipInstance);
        set.SendMessage(data.MixerWalkNode, (InputPortID)DirectionMixerNode.SimulationPorts.Clip4, walkShortRightClipInstance);

        set.SendMessage(data.MixerJogNode, (InputPortID)DirectionMixerNode.SimulationPorts.ClipConfiguration, setup.Configuration);
        set.SendMessage(data.MixerJogNode, (InputPortID)DirectionMixerNode.SimulationPorts.Clip0, jogShortLeftClipInstance);
        set.SendMessage(data.MixerJogNode, (InputPortID)DirectionMixerNode.SimulationPorts.Clip1, jogLongLeftClipInstance);
        set.SendMessage(data.MixerJogNode, (InputPortID)DirectionMixerNode.SimulationPorts.Clip2, jogStraightClipInstance);
        set.SendMessage(data.MixerJogNode, (InputPortID)DirectionMixerNode.SimulationPorts.Clip3, jogLongRightClipInstance);
        set.SendMessage(data.MixerJogNode, (InputPortID)DirectionMixerNode.SimulationPorts.Clip4, jogShortRightClipInstance);

        set.SendMessage(data.MixerSpeedNode, (InputPortID)MixerNode.SimulationPorts.RigDefinition, rigDefinition.Value);
        set.SendMessage(data.RootMotionNode, (InputPortID)RootMotionNode.SimulationPorts.RigDefinition, rigDefinition.Value);

        RigidTransform rootX = RigidTransform.identity;
        if (EntityManager.HasComponent<Translation>(entity))
        {
            rootX.pos = EntityManager.GetComponentData<Translation>(entity).Value;
        }
        else
        {
            PostUpdateCommands.AddComponent(entity, new Translation());
        }

        if (EntityManager.HasComponent<Rotation>(entity))
        {
            rootX.rot = EntityManager.GetComponentData<Rotation>(entity).Value;
        }
        else
        {
            PostUpdateCommands.AddComponent(entity, new Rotation { Value = quaternion.identity });
        }
        set.SetData(data.RootMotionNode, (InputPortID)RootMotionNode.KernelPorts.PrevRootX, rootX);

        data.RootXValue = set.CreateGraphValue<RigidTransform>(data.RootMotionNode, (OutputPortID)RootMotionNode.KernelPorts.RootX);
        data.Output.Buffer = set.CreateGraphValue<Buffer<float>>(data.RootMotionNode, (OutputPortID)RootMotionNode.KernelPorts.Output);
        PostUpdateCommands.AddComponent(entity, data.Output);

        return data;
    }

    protected override void DestroyGraph(Entity entity, NodeSet set, ref AnimationControllerData data)
    {
        set.Destroy(data.DeltaTimeNode);
        set.Destroy(data.TimeCounterNode);
        set.Destroy(data.MixerWalkNode);
        set.Destroy(data.MixerJogNode);
        set.Destroy(data.MixerSpeedNode);
        set.Destroy(data.RootMotionNode);

        set.ReleaseGraphValue(data.RootXValue);
        set.ReleaseGraphValue(data.Output.Buffer);
    }

    protected override void UpdateGraph(Entity entity, NodeSet set, ref AnimationControllerData data)
    {
        RigidTransform rootX = RigidTransform.identity;
        rootX.pos = EntityManager.GetComponentData<Translation>(entity).Value;
        rootX.rot = EntityManager.GetComponentData<Rotation>(entity).Value;

        var dampWeight = Time.DeltaTime / 0.5f;

        if (data.Player == 0)
        {
            var rand = new Unity.Mathematics.Random((uint)entity.Index + (uint)math.fmod((float)Time.ElapsedTime * 1000, 1000));

            data.Direction += rand.NextBool() ? -0.1f : 0.1f;
            data.Direction = math.clamp(data.Direction, 0, 4);

            data.Speed += rand.NextBool() ? -0.1f : 0.1f;
            data.Speed = math.clamp(data.Speed, 0, 1);
        }

        data.Player = 0;
        
        data.DirectionDamped = math.lerp(data.DirectionDamped, data.Direction, dampWeight);
        set.SendMessage(data.MixerWalkNode, (InputPortID)DirectionMixerNode.SimulationPorts.Blend, data.DirectionDamped);
        set.SendMessage(data.MixerJogNode, (InputPortID)DirectionMixerNode.SimulationPorts.Blend, data.DirectionDamped);

        data.SpeedDamped = math.lerp(data.SpeedDamped, data.Speed, dampWeight);
        set.SendMessage(data.TimeCounterNode, (InputPortID)TimeCounterNode.SimulationPorts.Speed, 1.0f + 0.5f * data.SpeedDamped);
        set.SendMessage(data.MixerSpeedNode, (InputPortID)MixerNode.SimulationPorts.Blend, data.SpeedDamped);
        set.SetData(data.RootMotionNode, (InputPortID)RootMotionNode.KernelPorts.PrevRootX, rootX);

        data.FollowX.pos = math.lerp(data.FollowX.pos, rootX.pos, dampWeight);
        data.FollowX.rot = math.nlerp(math.normalizesafe(data.FollowX.rot), rootX.rot, dampWeight);
    }
}

[UpdateInGroup(typeof(AnimationSystemGroup))]
[UpdateAfter(typeof(AnimationGraphSystem))]
public class AnimationControllerUpdateRootMotion : JobComponentSystem
{
    AnimationGraphSystem m_AnimGraphSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_AnimGraphSystem = World.GetOrCreateSystem<AnimationGraphSystem>();
        m_AnimGraphSystem.AddRef();
    }

    protected override void OnDestroy()
    {
        if (m_AnimGraphSystem != null)
            m_AnimGraphSystem.RemoveRef();

        base.OnDestroy();
    }

    protected override JobHandle OnUpdate(JobHandle inputDep)
    {
        var set = m_AnimGraphSystem.Set;
        if (set == null)
            return inputDep;

        inputDep.Complete();

        Entities
            .WithoutBurst()
            .ForEach((Entity entity, ref Translation translation, ref Rotation rotation, ref AnimationControllerData data) =>
            {
                var rootX = set.GetValueBlocking(data.RootXValue);
                translation.Value = rootX.pos;
                rotation.Value = rootX.rot;
            }).Run();

        return default;
    }
}