using Unity.Animation;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;

#if UNITY_EDITOR
using UnityEngine;
using Unity.Animation.Hybrid;

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
        var walkShortLeftClip  = WalkShortLeftClip.ToDenseClip();
        var walkLongLeftClip   = WalkLongLeftClip.ToDenseClip();
        var walkStraightClip   = WalkStraightClip.ToDenseClip();
        var walkLongRightClip  = WalkLongRightClip.ToDenseClip();
        var walkShortRightClip = WalkShortRightClip.ToDenseClip();

        var jogShortLeftClip   = JogShortLeftClip.ToDenseClip();
        var jogLongLeftClip    = JogLongLeftClip.ToDenseClip();
        var jogStraightClip    = JogStraightClip.ToDenseClip();
        var jogLongRightClip   = JogLongRightClip.ToDenseClip();
        var jogShortRightClip  = JogShortRightClip.ToDenseClip();

        var clipConfiguration = new ClipConfiguration();

        clipConfiguration.Mask = ClipConfigurationMask.LoopTime | ClipConfigurationMask.CycleRootMotion | ClipConfigurationMask.DeltaRootMotion;
        clipConfiguration.Mask |= LoopValues ? ClipConfigurationMask.LoopValues : 0;
        clipConfiguration.Mask |= BankPivot ? ClipConfigurationMask.BankPivot : 0;

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
            var rigDefinition = dstManager.GetComponentData<Rig>(entity);

            graphSetup.WalkShortLeftClip = UberClipNode.Bake(rigDefinition.Value, walkShortLeftClip, clipConfiguration, SampleRate);
            graphSetup.WalkLongLeftClip = UberClipNode.Bake(rigDefinition.Value, walkLongLeftClip, clipConfiguration, SampleRate);
            graphSetup.WalkStraightClip = UberClipNode.Bake(rigDefinition.Value, walkStraightClip, clipConfiguration, SampleRate);
            graphSetup.WalkLongRightClip = UberClipNode.Bake(rigDefinition.Value, walkLongRightClip, clipConfiguration, SampleRate);
            graphSetup.WalkShortRightClip = UberClipNode.Bake(rigDefinition.Value, walkShortRightClip, clipConfiguration, SampleRate);

            graphSetup.JogShortLeftClip = UberClipNode.Bake(rigDefinition.Value, jogShortLeftClip, clipConfiguration, SampleRate);
            graphSetup.JogLongLeftClip = UberClipNode.Bake(rigDefinition.Value, jogLongLeftClip, clipConfiguration, SampleRate);
            graphSetup.JogStraightClip = UberClipNode.Bake(rigDefinition.Value, jogStraightClip, clipConfiguration, SampleRate);
            graphSetup.JogLongRightClip = UberClipNode.Bake(rigDefinition.Value, jogLongRightClip, clipConfiguration, SampleRate);
            graphSetup.JogShortRightClip = UberClipNode.Bake(rigDefinition.Value, jogShortRightClip, clipConfiguration, SampleRate);

            clipConfiguration.Mask = ClipConfigurationMask.NormalizedTime | ClipConfigurationMask.LoopTime | ClipConfigurationMask.RootMotionFromVelocity;
            clipConfiguration.MotionID = 0;
        }
        else
        {
            clipConfiguration.Mask |= ClipConfigurationMask.NormalizedTime;
        }

        graphSetup.Configuration = clipConfiguration;
        dstManager.AddComponentData(entity, graphSetup);
        dstManager.AddComponent<ProcessDefaultAnimationGraph.AnimatedRootMotion>(entity);

        dstManager.AddComponent<DeltaTime>(entity);
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
    public GraphHandle Graph;

    public RigidTransform FollowX;

    public float Direction;
    public float DirectionDamped;
    public float Speed;
    public float SpeedDamped;

    public int Player;
}

public struct AnimationControllerDataInput : IComponentData
{
    public float MixerWalkJobBlend;
    public float TimeCounterSpeed;
    public float MixerSpeedBlend;
}

[UpdateBefore(typeof(DefaultAnimationSystemGroup))]
public class AnimationControllerSystem : SampleSystemBase<
    AnimationControllerSetup,
    AnimationControllerData,
    ProcessDefaultAnimationGraph
>
{
    protected override AnimationControllerData CreateGraph(Entity entity, ref Rig rig, ProcessDefaultAnimationGraph graphSystem, ref AnimationControllerSetup setup)
    {
        var set = graphSystem.Set;
        var data = new AnimationControllerData();
        data.Graph = graphSystem.CreateGraph();

        var entityNode                       = graphSystem.CreateNode(data.Graph, entity);
        var deltaTimeNode                    = graphSystem.CreateNode<ConvertDeltaTimeToFloatNode>(data.Graph);
        var timeCounterNode                  = graphSystem.CreateNode<TimeCounterNode>(data.Graph);
        var mixerWalkNode                    = graphSystem.CreateNode<DirectionMixerNode>(data.Graph);
        var mixerJogNode                     = graphSystem.CreateNode<DirectionMixerNode>(data.Graph);
        var mixerSpeedNode                   = graphSystem.CreateNode<MixerNode>(data.Graph);
        var animationControllerDataInputNode = graphSystem.CreateNode<AnimationControllerDataInputNode>(data.Graph);

        data.Direction = 2.0f;
        data.Speed = 0.0f;

        set.Connect(entityNode, deltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Input, NodeSet.ConnectionType.Feedback);
        set.Connect(deltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, timeCounterNode, TimeCounterNode.KernelPorts.DeltaTime);
        set.Connect(timeCounterNode, TimeCounterNode.KernelPorts.OutputDeltaTime, mixerWalkNode, DirectionMixerNode.KernelPorts.DeltaTime);
        set.Connect(timeCounterNode, TimeCounterNode.KernelPorts.Time, mixerWalkNode, DirectionMixerNode.KernelPorts.Time);
        set.Connect(timeCounterNode, TimeCounterNode.KernelPorts.OutputDeltaTime, mixerJogNode, DirectionMixerNode.KernelPorts.DeltaTime);
        set.Connect(timeCounterNode, TimeCounterNode.KernelPorts.Time, mixerJogNode, DirectionMixerNode.KernelPorts.Time);

        set.Connect(mixerWalkNode, DirectionMixerNode.KernelPorts.Output, mixerSpeedNode, MixerNode.KernelPorts.Input0);
        set.Connect(mixerJogNode, DirectionMixerNode.KernelPorts.Output, mixerSpeedNode, MixerNode.KernelPorts.Input1);
        set.Connect(mixerSpeedNode, MixerNode.KernelPorts.Output, entityNode);

        set.Connect(entityNode, animationControllerDataInputNode, AnimationControllerDataInputNode.KernelPorts.Input, NodeSet.ConnectionType.Feedback);
        set.Connect(animationControllerDataInputNode, AnimationControllerDataInputNode.KernelPorts.MixerWalkJobBlend, mixerWalkNode, DirectionMixerNode.KernelPorts.Weight);
        set.Connect(animationControllerDataInputNode, AnimationControllerDataInputNode.KernelPorts.MixerWalkJobBlend, mixerJogNode, DirectionMixerNode.KernelPorts.Weight);
        set.Connect(animationControllerDataInputNode, AnimationControllerDataInputNode.KernelPorts.TimeCounterSpeed, timeCounterNode, TimeCounterNode.KernelPorts.Speed);
        set.Connect(animationControllerDataInputNode, AnimationControllerDataInputNode.KernelPorts.MixerSpeedBlend, mixerSpeedNode, MixerNode.KernelPorts.Weight);

        set.SendMessage(mixerWalkNode, DirectionMixerNode.SimulationPorts.Rig, rig);
        set.SendMessage(mixerWalkNode, DirectionMixerNode.SimulationPorts.ClipConfiguration, setup.Configuration);
        set.SendMessage(mixerWalkNode, DirectionMixerNode.SimulationPorts.Clip0, setup.WalkShortLeftClip);
        set.SendMessage(mixerWalkNode, DirectionMixerNode.SimulationPorts.Clip1, setup.WalkLongLeftClip);
        set.SendMessage(mixerWalkNode, DirectionMixerNode.SimulationPorts.Clip2, setup.WalkStraightClip);
        set.SendMessage(mixerWalkNode, DirectionMixerNode.SimulationPorts.Clip3, setup.WalkLongRightClip);
        set.SendMessage(mixerWalkNode, DirectionMixerNode.SimulationPorts.Clip4, setup.WalkShortRightClip);

        set.SendMessage(mixerJogNode, DirectionMixerNode.SimulationPorts.Rig, rig);
        set.SendMessage(mixerJogNode, DirectionMixerNode.SimulationPorts.ClipConfiguration, setup.Configuration);
        set.SendMessage(mixerJogNode, DirectionMixerNode.SimulationPorts.Clip0, setup.JogShortLeftClip);
        set.SendMessage(mixerJogNode, DirectionMixerNode.SimulationPorts.Clip1, setup.JogLongLeftClip);
        set.SendMessage(mixerJogNode, DirectionMixerNode.SimulationPorts.Clip2, setup.JogStraightClip);
        set.SendMessage(mixerJogNode, DirectionMixerNode.SimulationPorts.Clip3, setup.JogLongRightClip);
        set.SendMessage(mixerJogNode, DirectionMixerNode.SimulationPorts.Clip4, setup.JogShortRightClip);

        set.SendMessage(mixerSpeedNode, MixerNode.SimulationPorts.Rig, rig);

        PostUpdateCommands.AddComponent<AnimationControllerDataInput>(entity);

        return data;
    }

    protected override void DestroyGraph(Entity entity, ProcessDefaultAnimationGraph graphSystem, ref AnimationControllerData data)
    {
        graphSystem.Dispose(data.Graph);
    }
}

[UpdateBefore(typeof(DefaultAnimationSystemGroup))]
public class AnimationControllerApplyState : SystemBase
{
    protected override void OnUpdate()
    {
        var dampWeight = Time.DeltaTime / 0.5f;
        var time = Time.ElapsedTime;

        Dependency = Entities
            .ForEach((Entity entity, ref LocalToWorld localToWorld, ref AnimationControllerData data, ref AnimationControllerDataInput input) =>
        {
            var rootX = new RigidTransform(localToWorld.Value);

            if (data.Player == 0)
            {
                var rand = new Unity.Mathematics.Random((uint)entity.Index + (uint)math.fmod(time * 1000, 1000));

                data.Direction += rand.NextBool() ? -0.1f : 0.1f;
                data.Direction = math.clamp(data.Direction, 0, 4);

                data.Speed += rand.NextBool() ? -0.1f : 0.1f;
                data.Speed = math.clamp(data.Speed, 0, 1);
            }

            data.Player = 0;

            data.DirectionDamped = math.lerp(data.DirectionDamped, data.Direction, dampWeight);
            input.MixerWalkJobBlend = data.DirectionDamped;

            data.SpeedDamped = math.lerp(data.SpeedDamped, data.Speed, dampWeight);
            input.TimeCounterSpeed = 1.0f + 0.5f * data.SpeedDamped;
            input.MixerSpeedBlend = data.SpeedDamped;

            data.FollowX.pos = math.lerp(data.FollowX.pos, rootX.pos, dampWeight);
            data.FollowX.rot = mathex.lerp(math.normalizesafe(data.FollowX.rot), rootX.rot, dampWeight);
        }).ScheduleParallel(Dependency);
    }
}

public class AnimationControllerDataInputNode
    : KernelNodeDefinition<AnimationControllerDataInputNode.KernelDefs>
{
    public struct KernelDefs : IKernelPortDefinition
    {
        public DataInput<AnimationControllerDataInputNode, AnimationControllerDataInput> Input;

        public DataOutput<AnimationControllerDataInputNode, float> MixerWalkJobBlend;
        public DataOutput<AnimationControllerDataInputNode, float> TimeCounterSpeed;
        public DataOutput<AnimationControllerDataInputNode, float> MixerSpeedBlend;
    }

    struct KernelData : IKernelData {}

    [BurstCompile]
    struct Kernel : IGraphKernel<KernelData, KernelDefs>
    {
        public void Execute(RenderContext ctx, in KernelData data, ref KernelDefs ports)
        {
            var input = ctx.Resolve(ports.Input);
            ctx.Resolve(ref ports.MixerWalkJobBlend) = input.MixerWalkJobBlend;
            ctx.Resolve(ref ports.TimeCounterSpeed)  = input.TimeCounterSpeed;
            ctx.Resolve(ref ports.MixerSpeedBlend)   = input.MixerSpeedBlend;
        }
    }
}
