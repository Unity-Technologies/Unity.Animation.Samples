using Unity.Animation;
using Unity.Animation.Hybrid;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;

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
    public NodeHandle<ComponentNode>                              EntityNode;
    public NodeHandle<DeltaTimeNode>                              DeltaTimeNode;
    public NodeHandle<TimeCounterNode>                            TimeCounterNode;
    public NodeHandle<DirectionMixerNode>                         MixerWalkNode;
    public NodeHandle<DirectionMixerNode>                         MixerJogNode;
    public NodeHandle<MixerNode>                                  MixerSpeedNode;
    public NodeHandle<RootMotionNode>                             RootMotionNode;
    public NodeHandle<AnimationControllerDataInputNode>           AnimationControllerDataInputNode;
    public NodeHandle<ConvertLocalToWorldComponentToFloat4x4Node> LocalToWorldToFloat4x4Node;
    public NodeHandle<ConvertFloat4x4ToLocalToWorldComponentNode> Float4x4ToLocalToWorldNode;

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

[UpdateBefore(typeof(PreAnimationSystemGroup))]
public class AnimationControllerSystem : SampleSystemBase<
    AnimationControllerSetup,
    AnimationControllerData,
    PreAnimationGraphTag,
    PreAnimationGraphSystem
    >
{
    protected override AnimationControllerData CreateGraph(Entity entity, ref Rig rig, PreAnimationGraphSystem graphSystem, ref AnimationControllerSetup setup)
    {
        var set = graphSystem.Set;
        var data = new AnimationControllerData();

        data.EntityNode                       = set.CreateComponentNode(entity);
        data.DeltaTimeNode                    = set.Create<DeltaTimeNode>();
        data.TimeCounterNode                  = set.Create<TimeCounterNode>();
        data.MixerWalkNode                    = set.Create<DirectionMixerNode>();
        data.MixerJogNode                     = set.Create<DirectionMixerNode>();
        data.MixerSpeedNode                   = set.Create<MixerNode>();
        data.RootMotionNode                   = set.Create<RootMotionNode>();
        data.AnimationControllerDataInputNode = set.Create<AnimationControllerDataInputNode>();
        data.LocalToWorldToFloat4x4Node       = set.Create<ConvertLocalToWorldComponentToFloat4x4Node>();
        data.Float4x4ToLocalToWorldNode       = set.Create<ConvertFloat4x4ToLocalToWorldComponentNode>();

        data.Direction = 2.0f;
        data.Speed = 0.0f;

        set.Connect(data.DeltaTimeNode, DeltaTimeNode.KernelPorts.DeltaTime, data.TimeCounterNode, TimeCounterNode.KernelPorts.DeltaTime);
        set.Connect(data.TimeCounterNode, TimeCounterNode.KernelPorts.OutputDeltaTime, data.MixerWalkNode, DirectionMixerNode.KernelPorts.DeltaTime);
        set.Connect(data.TimeCounterNode, TimeCounterNode.KernelPorts.Time, data.MixerWalkNode, DirectionMixerNode.KernelPorts.Time);
        set.Connect(data.TimeCounterNode, TimeCounterNode.KernelPorts.OutputDeltaTime, data.MixerJogNode, DirectionMixerNode.KernelPorts.DeltaTime);
        set.Connect(data.TimeCounterNode, TimeCounterNode.KernelPorts.Time, data.MixerJogNode, DirectionMixerNode.KernelPorts.Time);

        set.Connect(data.MixerWalkNode, DirectionMixerNode.KernelPorts.Output, data.MixerSpeedNode, MixerNode.KernelPorts.Input0);
        set.Connect(data.MixerJogNode, DirectionMixerNode.KernelPorts.Output, data.MixerSpeedNode, MixerNode.KernelPorts.Input1);
        set.Connect(data.MixerSpeedNode, MixerNode.KernelPorts.Output, data.RootMotionNode, RootMotionNode.KernelPorts.Input);
        set.Connect(data.RootMotionNode, RootMotionNode.KernelPorts.Output, data.EntityNode);

        set.Connect(data.EntityNode, data.LocalToWorldToFloat4x4Node, ConvertLocalToWorldComponentToFloat4x4Node.KernelPorts.Input, NodeSet.ConnectionType.Feedback);
        set.Connect(data.LocalToWorldToFloat4x4Node, ConvertLocalToWorldComponentToFloat4x4Node.KernelPorts.Output, data.RootMotionNode, RootMotionNode.KernelPorts.PrevRootX);
        set.Connect(data.EntityNode, data.AnimationControllerDataInputNode, AnimationControllerDataInputNode.KernelPorts.Input, NodeSet.ConnectionType.Feedback);
        set.Connect(data.RootMotionNode, RootMotionNode.KernelPorts.RootX, data.Float4x4ToLocalToWorldNode, ConvertFloat4x4ToLocalToWorldComponentNode.KernelPorts.Input);
        set.Connect(data.Float4x4ToLocalToWorldNode, ConvertFloat4x4ToLocalToWorldComponentNode.KernelPorts.Output, data.EntityNode);

        set.Connect(data.AnimationControllerDataInputNode, AnimationControllerDataInputNode.KernelPorts.MixerWalkJobBlend, data.MixerWalkNode, DirectionMixerNode.KernelPorts.Weight);
        set.Connect(data.AnimationControllerDataInputNode, AnimationControllerDataInputNode.KernelPorts.MixerWalkJobBlend, data.MixerJogNode, DirectionMixerNode.KernelPorts.Weight);
        set.Connect(data.AnimationControllerDataInputNode, AnimationControllerDataInputNode.KernelPorts.TimeCounterSpeed, data.TimeCounterNode, TimeCounterNode.KernelPorts.Speed);
        set.Connect(data.AnimationControllerDataInputNode, AnimationControllerDataInputNode.KernelPorts.MixerSpeedBlend, data.MixerSpeedNode, MixerNode.KernelPorts.Weight);

        set.SendMessage(data.MixerWalkNode, DirectionMixerNode.SimulationPorts.Rig, rig);
        set.SendMessage(data.MixerWalkNode, DirectionMixerNode.SimulationPorts.ClipConfiguration, setup.Configuration);
        set.SendMessage(data.MixerWalkNode, DirectionMixerNode.SimulationPorts.Clip0, setup.WalkShortLeftClip);
        set.SendMessage(data.MixerWalkNode, DirectionMixerNode.SimulationPorts.Clip1, setup.WalkLongLeftClip);
        set.SendMessage(data.MixerWalkNode, DirectionMixerNode.SimulationPorts.Clip2, setup.WalkStraightClip);
        set.SendMessage(data.MixerWalkNode, DirectionMixerNode.SimulationPorts.Clip3, setup.WalkLongRightClip);
        set.SendMessage(data.MixerWalkNode, DirectionMixerNode.SimulationPorts.Clip4, setup.WalkShortRightClip);

        set.SendMessage(data.MixerJogNode, DirectionMixerNode.SimulationPorts.Rig, rig);
        set.SendMessage(data.MixerJogNode, DirectionMixerNode.SimulationPorts.ClipConfiguration, setup.Configuration);
        set.SendMessage(data.MixerJogNode, DirectionMixerNode.SimulationPorts.Clip0, setup.JogShortLeftClip);
        set.SendMessage(data.MixerJogNode, DirectionMixerNode.SimulationPorts.Clip1, setup.JogLongLeftClip);
        set.SendMessage(data.MixerJogNode, DirectionMixerNode.SimulationPorts.Clip2, setup.JogStraightClip);
        set.SendMessage(data.MixerJogNode, DirectionMixerNode.SimulationPorts.Clip3, setup.JogLongRightClip);
        set.SendMessage(data.MixerJogNode, DirectionMixerNode.SimulationPorts.Clip4, setup.JogShortRightClip);

        set.SendMessage(data.MixerSpeedNode, MixerNode.SimulationPorts.Rig, rig);
        set.SendMessage(data.RootMotionNode, RootMotionNode.SimulationPorts.Rig, rig);

        PostUpdateCommands.AddComponent<AnimationControllerDataInput>(entity);
        PostUpdateCommands.AddComponent(entity, graphSystem.Tag);

        return data;
    }

    protected override void DestroyGraph(Entity entity, PreAnimationGraphSystem graphSystem, ref AnimationControllerData data)
    {
        var set = graphSystem.Set;
        set.Destroy(data.DeltaTimeNode);
        set.Destroy(data.TimeCounterNode);
        set.Destroy(data.MixerWalkNode);
        set.Destroy(data.MixerJogNode);
        set.Destroy(data.MixerSpeedNode);
        set.Destroy(data.RootMotionNode);
        set.Destroy(data.EntityNode);
        set.Destroy(data.AnimationControllerDataInputNode);
        set.Destroy(data.LocalToWorldToFloat4x4Node);
        set.Destroy(data.Float4x4ToLocalToWorldNode);
    }
}

[UpdateBefore(typeof(PreAnimationSystemGroup))]
public class AnimationControllerApplyState : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDep)
    {
        var dampWeight = Time.DeltaTime / 0.5f;
        var time = Time.ElapsedTime;

        return Entities
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
            }).Schedule(inputDep);
    }
}

public class AnimationControllerDataInputNode
    : NodeDefinition<AnimationControllerDataInputNode.Data, AnimationControllerDataInputNode.SimPorts, AnimationControllerDataInputNode.KernelData, AnimationControllerDataInputNode.KernelDefs, AnimationControllerDataInputNode.Kernel>
{
    public struct SimPorts : ISimulationPortDefinition { }

    public struct KernelDefs : IKernelPortDefinition
    {
        public DataInput<AnimationControllerDataInputNode, AnimationControllerDataInput> Input;

        public DataOutput<AnimationControllerDataInputNode, float> MixerWalkJobBlend;
        public DataOutput<AnimationControllerDataInputNode, float> TimeCounterSpeed;
        public DataOutput<AnimationControllerDataInputNode, float> MixerSpeedBlend;
    }

    public struct Data : INodeData { }

    public struct KernelData : IKernelData { }

    [BurstCompile]
    public struct Kernel : IGraphKernel<KernelData, KernelDefs>
    {
        public void Execute(RenderContext ctx, KernelData data, ref KernelDefs ports)
        {
            var input = ctx.Resolve(ports.Input);
            ctx.Resolve(ref ports.MixerWalkJobBlend) = input.MixerWalkJobBlend;
            ctx.Resolve(ref ports.TimeCounterSpeed)  = input.TimeCounterSpeed;
            ctx.Resolve(ref ports.MixerSpeedBlend)   = input.MixerSpeedBlend;
        }
    }
}