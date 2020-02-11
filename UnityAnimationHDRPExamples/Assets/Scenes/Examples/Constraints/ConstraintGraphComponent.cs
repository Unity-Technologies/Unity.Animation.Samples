using Unity.Animation;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

#if UNITY_EDITOR
using UnityEngine;
using Unity.Animation.Hybrid;

[RequiresEntityConversion]
[RequireComponent(typeof(RigComponent))]
public class ConstraintGraphComponent : MonoBehaviour, IConvertGameObjectToEntity
{
    [System.Serializable]
    public struct TwoBoneIKSetup
    {
        public Transform Root;
        public Transform Mid;
        public Transform Tip;
    }

    [System.Serializable]
    public struct AimConstraintSetup
    {
        public Transform Bone;
        public float3    LocalAimAxis;
    }

    public AnimationClip Clip;
    public TwoBoneIKSetup LeftArmIK;
    public TwoBoneIKSetup RightArmIK;
    public AimConstraintSetup HeadLookAt;

    int3 Convert(in TwoBoneIKSetup setup, RigComponent rig)
    {
        var indices = math.int3(
            RigGenerator.FindTransformIndex(setup.Root, rig.Bones),
            RigGenerator.FindTransformIndex(setup.Mid, rig.Bones),
            RigGenerator.FindTransformIndex(setup.Tip, rig.Bones)
            );

        if (indices.x == -1 || indices.y == -1 || indices.z == -1)
            throw new System.InvalidOperationException("Invalid TwoBoneIK transforms");

        return indices;
    }

    int Convert(in AimConstraintSetup setup, RigComponent rig)
    {
        var index = RigGenerator.FindTransformIndex(setup.Bone, rig.Bones);
        if (index == -1)
            throw new System.InvalidOperationException("Invalid AimConstraint Bone");

        return index;
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var rig = GetComponent<RigComponent>();

        var fkGraphSetup = new FKGraphSetup
        {
            Clip = ClipBuilder.AnimationClipToDenseClip(Clip)
        };

        var ikGraphSetup = new IKGraphSetup
        {
            LeftArmIK = Convert(LeftArmIK, rig),
            RightArmIK = Convert(RightArmIK, rig),
            HeadIndex = Convert(HeadLookAt, rig),
            HeadLocalAimAxis = HeadLookAt.LocalAimAxis
        };

        dstManager.AddComponentData(entity, fkGraphSetup);
        dstManager.AddComponentData(entity, ikGraphSetup);
    }
}
#endif

public struct FKGraphSetup : ISampleSetup
{
    public BlobAssetReference<Clip> Clip;
};

public struct FKGraphData : ISampleData
{
    public NodeHandle<DeltaTimeNode>  DeltaTimeNode;
    public NodeHandle<ClipPlayerNode> ClipNode;
    public NodeHandle<ComponentNode>  EntityNode;

    public NodeHandle<ComponentNode> DebugEntityNode;
}

public struct IKGraphSetup : ISampleSetup
{
    public int3 LeftArmIK;
    public int3 RightArmIK;
    public int HeadIndex;
    public float3 HeadLocalAimAxis;
};

public struct IKGraphData : ISampleData
{
    public NodeHandle<TwoBoneIKNode>     LeftArmIKNode;
    public NodeHandle<TwoBoneIKNode>     RightArmIKNode;
    public NodeHandle<AimConstraintNode> HeadLookAtNode;
    public NodeHandle<ComponentNode>     EntityNode;
}

// Creates a simple clip sampler in the PreAnimationGraphSystem which is evaluated before the TransformSystemGroup.
[UpdateBefore(typeof(PreAnimationSystemGroup))]
public class FKGraphSystem : SampleSystemBase<
    FKGraphSetup,
    FKGraphData,
    PreAnimationGraphTag,
    PreAnimationGraphSystem
    >
{
    protected override FKGraphData CreateGraph(Entity entity, ref Rig rig, PreAnimationGraphSystem graphSystem, ref FKGraphSetup setup)
    {
        var set = graphSystem.Set;

        var debugEntity = RigUtils.InstantiateDebugRigEntity(
            rig,
            EntityManager,
            new BoneRendererProperties { BoneShape = BoneRendererUtils.BoneShape.Line, Color = math.float4(1f, 0f, 0f, 1f), Size = 1f }
            );

        if (EntityManager.HasComponent<Translation>(entity))
        {
            float3 t = EntityManager.GetComponentData<Translation>(entity).Value;
            PostUpdateCommands.AddComponent(
                debugEntity,
                new Translation { Value = t - math.float3(0f, 0f, 0.5f) }
                );
        }

        var data = new FKGraphData();

        data.DeltaTimeNode   = set.Create<DeltaTimeNode>();
        data.ClipNode        = set.Create<ClipPlayerNode>();
        data.EntityNode      = set.CreateComponentNode(entity);
        data.DebugEntityNode = set.CreateComponentNode(debugEntity);

        set.Connect(data.DeltaTimeNode, DeltaTimeNode.KernelPorts.DeltaTime, data.ClipNode, ClipPlayerNode.KernelPorts.DeltaTime);
        set.Connect(data.ClipNode, ClipPlayerNode.KernelPorts.Output, data.EntityNode);
        set.Connect(data.ClipNode, ClipPlayerNode.KernelPorts.Output, data.DebugEntityNode);

        set.SetData(data.ClipNode, ClipPlayerNode.KernelPorts.Speed, 1.0f);
        set.SendMessage(data.ClipNode, ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime | ClipConfigurationMask.LoopValues });
        set.SendMessage(data.ClipNode, ClipPlayerNode.SimulationPorts.Rig, rig);
        set.SendMessage(data.ClipNode, ClipPlayerNode.SimulationPorts.Clip, setup.Clip);

        PostUpdateCommands.AddComponent(entity, graphSystem.Tag);

        return data;
    }

    protected override void DestroyGraph(Entity entity, PreAnimationGraphSystem graphSystem, ref FKGraphData data)
    {
        var set = graphSystem.Set;
        set.Destroy(data.DeltaTimeNode);
        set.Destroy(data.ClipNode);
        set.Destroy(data.EntityNode);
        set.Destroy(data.DebugEntityNode);
    }
}

// Creates a graph which constrains the left and right arms using a TwoBoneIK constraint and a head lookat using an AimConstraint.
// This graph is created in the PostAnimationGraphSystem which is evaluated after the TransformSystemGroup making it ideal to
// do pose corrections
[UpdateBefore(typeof(PreAnimationSystemGroup))]
public class IKGraphSystem : SampleSystemBase<
    IKGraphSetup,
    IKGraphData,
    PostAnimationGraphTag,
    PostAnimationGraphSystem
    >
{
    protected override IKGraphData CreateGraph(Entity entity, ref Rig rig, PostAnimationGraphSystem graphSystem, ref IKGraphSetup setup)
    {
        var set = graphSystem.Set;

        var data = new IKGraphData();

        data.EntityNode     = set.CreateComponentNode(entity);
        data.LeftArmIKNode  = set.Create<TwoBoneIKNode>();
        data.RightArmIKNode = set.Create<TwoBoneIKNode>();
        data.HeadLookAtNode = set.Create<AimConstraintNode>();

        set.Connect(data.EntityNode, data.LeftArmIKNode, TwoBoneIKNode.KernelPorts.Input, NodeSet.ConnectionType.Feedback);
        set.Connect(data.LeftArmIKNode, TwoBoneIKNode.KernelPorts.Output, data.RightArmIKNode, TwoBoneIKNode.KernelPorts.Input);
        set.Connect(data.RightArmIKNode, TwoBoneIKNode.KernelPorts.Output, data.HeadLookAtNode, AimConstraintNode.KernelPorts.Input);
        set.Connect(data.HeadLookAtNode, AimConstraintNode.KernelPorts.Output, data.EntityNode);

        var stream = AnimationStream.FromDefaultValues(rig);
        var leftArmIK = new TwoBoneIKNode.SetupMessage
        {
            RootIndex = setup.LeftArmIK.x,
            MidIndex = setup.LeftArmIK.y,
            TipIndex = setup.LeftArmIK.z,
            TargetOffset = math.RigidTransform(math.inverse(stream.GetLocalToRootRotation(setup.LeftArmIK.z)), float3.zero),
            LimbLengths = math.float2(
                math.distance(stream.GetLocalToRootTranslation(setup.LeftArmIK.x), stream.GetLocalToRootTranslation(setup.LeftArmIK.y)),
                math.distance(stream.GetLocalToRootTranslation(setup.LeftArmIK.y), stream.GetLocalToRootTranslation(setup.LeftArmIK.z))
                )
        };

        var rightArmIK = new TwoBoneIKNode.SetupMessage
        {
            RootIndex = setup.RightArmIK.x,
            MidIndex = setup.RightArmIK.y,
            TipIndex = setup.RightArmIK.z,
            TargetOffset = math.RigidTransform(math.inverse(stream.GetLocalToRootRotation(setup.RightArmIK.z)), float3.zero),
            LimbLengths = math.float2(
                math.distance(stream.GetLocalToRootTranslation(setup.RightArmIK.x), stream.GetLocalToRootTranslation(setup.RightArmIK.y)),
                math.distance(stream.GetLocalToRootTranslation(setup.RightArmIK.y), stream.GetLocalToRootTranslation(setup.RightArmIK.z))
                )
        };

        var headAim = new AimConstraintNode.SetupMessage
        {
            Index = setup.HeadIndex,
            LocalAimAxis = setup.HeadLocalAimAxis,
            LocalAxesMask = math.bool3(true)
        };

        // Apply static corrections to the left and right arms but this data
        // could be coming from other entities or the AnimationStream itself
        set.SendMessage(data.LeftArmIKNode, TwoBoneIKNode.SimulationPorts.Rig, rig);
        set.SendMessage(data.LeftArmIKNode, TwoBoneIKNode.SimulationPorts.ConstraintSetup, leftArmIK);
        set.SetData(data.LeftArmIKNode, TwoBoneIKNode.KernelPorts.Target, math.float4x4(quaternion.AxisAngle(math.up(), math.radians(60f)), math.float3(-0.45f, 1.1f, 0.2f)));

        set.SendMessage(data.RightArmIKNode, TwoBoneIKNode.SimulationPorts.Rig, rig);
        set.SendMessage(data.RightArmIKNode, TwoBoneIKNode.SimulationPorts.ConstraintSetup, rightArmIK);
        set.SetData(data.RightArmIKNode, TwoBoneIKNode.KernelPorts.Target, math.float4x4(quaternion.AxisAngle(math.up(), math.radians(-60f)), math.float3(0.45f, 1.1f, 0.2f)));

        // Same as above but now applying static corrections to the head bone for a look at
        set.SendMessage(data.HeadLookAtNode, AimConstraintNode.SimulationPorts.Rig, rig);
        set.SendMessage(data.HeadLookAtNode, AimConstraintNode.SimulationPorts.ConstraintSetup, headAim);
        set.SetPortArraySize(data.HeadLookAtNode, AimConstraintNode.KernelPorts.SourcePositions, 1);
        set.SetPortArraySize(data.HeadLookAtNode, AimConstraintNode.KernelPorts.SourceOffsets, 1);
        set.SetPortArraySize(data.HeadLookAtNode, AimConstraintNode.KernelPorts.SourceWeights, 1);
        set.SetData(data.HeadLookAtNode, AimConstraintNode.KernelPorts.SourcePositions, 0, stream.GetLocalToRootTranslation(headAim.Index) + math.float3(0f, -2f, 0.5f));
        set.SetData(data.HeadLookAtNode, AimConstraintNode.KernelPorts.SourceOffsets, 0, quaternion.identity);
        set.SetData(data.HeadLookAtNode, AimConstraintNode.KernelPorts.SourceWeights, 0, 1f);

        PostUpdateCommands.AddComponent(entity, graphSystem.Tag);

        return data;
    }

    protected override void DestroyGraph(Entity entity, PostAnimationGraphSystem graphSystem, ref IKGraphData data)
    {
        var set = graphSystem.Set;
        set.Destroy(data.EntityNode);
        set.Destroy(data.LeftArmIKNode);
        set.Destroy(data.RightArmIKNode);
        set.Destroy(data.HeadLookAtNode);
    }
}
