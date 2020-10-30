using Unity.Animation;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

#if UNITY_EDITOR
using UnityEngine;
using Unity.Animation.Hybrid;
#endif

namespace ConstraintsSample
{
#if UNITY_EDITOR
    [RequireComponent(typeof(RigComponent))]
    public class ConstraintGraphComponent : MonoBehaviour, IConvertGameObjectToEntity
    {
        [System.Serializable]
        public struct TwoBoneIKSetup
        {
            public Transform Root;
            public Transform Mid;
            public Transform Tip;

            public Transform Target;
        }

        [System.Serializable]
        public struct AimConstraintSetup
        {
            public Transform Bone;
            public float3 LocalAimAxis;
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
                Clip = Clip.ToDenseClip()
            };

            var ikGraphSetup = new IKGraphSetup
            {
                LeftArmIK = Convert(LeftArmIK, rig),
                LeftTargetEntity = conversionSystem.GetPrimaryEntity(LeftArmIK.Target),
                RightArmIK = Convert(RightArmIK, rig),
                RightTargetEntity = conversionSystem.GetPrimaryEntity(RightArmIK.Target),
                HeadIndex = Convert(HeadLookAt, rig),
                HeadLocalAimAxis = HeadLookAt.LocalAimAxis
            };

            dstManager.AddComponentData(entity, fkGraphSetup);
            dstManager.AddComponentData(entity, ikGraphSetup);

            dstManager.AddComponent<DeltaTime>(entity);
        }
    }
#endif

    public struct FKGraphSetup : ISampleSetup
    {
        public BlobAssetReference<Clip> Clip;
    };

    public struct FKGraphData : ISampleData
    {
        public GraphHandle Graph;
    }

    public struct IKGraphSetup : ISampleSetup
    {
        public int3 LeftArmIK;
        public Entity LeftTargetEntity;
        public int3 RightArmIK;
        public Entity RightTargetEntity;
        public int HeadIndex;
        public float3 HeadLocalAimAxis;
    };

    public struct IKGraphData : ISampleData
    {
        public GraphHandle Graph;
    }

// Creates a simple clip sampler in the ProcessDefaultAnimationGraph system which is evaluated before the TransformSystemGroup.
    [UpdateBefore(typeof(DefaultAnimationSystemGroup))]
    public class FKGraphSystem : SampleSystemBase<
        FKGraphSetup,
        FKGraphData,
        ProcessDefaultAnimationGraph
    >
    {
        protected override FKGraphData CreateGraph(Entity entity, ref Rig rig, ProcessDefaultAnimationGraph graphSystem,
            ref FKGraphSetup setup)
        {
            var set = graphSystem.Set;
            var graph = graphSystem.CreateGraph();

            var debugEntity = RigUtils.InstantiateDebugRigEntity(
                rig,
                EntityManager,
                new BoneRendererProperties
                {BoneShape = BoneRendererUtils.BoneShape.Line, Color = math.float4(1f, 0f, 0f, 1f), Size = 1f}
            );

            if (EntityManager.HasComponent<Translation>(entity))
            {
                float3 t = EntityManager.GetComponentData<Translation>(entity).Value;
                PostUpdateCommands.AddComponent(
                    debugEntity,
                    new Translation {Value = t - math.float3(0f, 0f, 0.5f)}
                );
            }

            var data = new FKGraphData();
            data.Graph = graph;

            var deltaTimeNode = graphSystem.CreateNode<ConvertDeltaTimeToFloatNode>(graph);
            var clipNode = graphSystem.CreateNode<ClipPlayerNode>(graph);
            var entityNode = graphSystem.CreateNode(graph, entity);
            var debugEntityNode = graphSystem.CreateNode(graph, debugEntity);

            set.Connect(entityNode, deltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Input);
            set.Connect(deltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, clipNode, ClipPlayerNode.KernelPorts.DeltaTime);
            set.Connect(clipNode, ClipPlayerNode.KernelPorts.Output, entityNode, NodeSet.ConnectionType.Feedback);
            set.Connect(clipNode, ClipPlayerNode.KernelPorts.Output, debugEntityNode);

            set.SetData(clipNode, ClipPlayerNode.KernelPorts.Speed, 1.0f);
            set.SendMessage(clipNode, ClipPlayerNode.SimulationPorts.Configuration,
                new ClipConfiguration {Mask = ClipConfigurationMask.LoopTime | ClipConfigurationMask.LoopValues});
            set.SendMessage(clipNode, ClipPlayerNode.SimulationPorts.Rig, rig);
            set.SendMessage(clipNode, ClipPlayerNode.SimulationPorts.Clip, setup.Clip);

            return data;
        }

        protected override void DestroyGraph(Entity entity, ProcessDefaultAnimationGraph graphSystem, ref FKGraphData data)
        {
            graphSystem.Dispose(data.Graph);
        }
    }

// Creates a graph which constrains the left and right arms using a TwoBoneIK constraint and a head lookat using an AimConstraint.
// This graph is created in the ProcessLateAnimationGraph system which is evaluated after the TransformSystemGroup making it ideal to
// do pose corrections.
    [UpdateBefore(typeof(DefaultAnimationSystemGroup))]
    public class IKGraphSystem : SampleSystemBase<
        IKGraphSetup,
        IKGraphData,
        ProcessLateAnimationGraph
    >
    {
        protected override IKGraphData CreateGraph(Entity entity, ref Rig rig, ProcessLateAnimationGraph graphSystem,
            ref IKGraphSetup setup)
        {
            var set = graphSystem.Set;
            var graph = graphSystem.CreateGraph();

            var data = new IKGraphData();
            data.Graph = graph;

            var entityNode = graphSystem.CreateNode(graph, entity);

            var leftArmIKNode = graphSystem.CreateNode<TwoBoneIKNode>(graph);
            var leftTargetEntityNode = graphSystem.CreateNode(graph, setup.LeftTargetEntity);
            var leftWorldToRootNode = graphSystem.CreateNode<WorldToRootNode>(graph);

            var rightArmIKNode = graphSystem.CreateNode<TwoBoneIKNode>(graph);
            var rightTargetEntityNode = graphSystem.CreateNode(graph, setup.RightTargetEntity);
            var rightWorldToRootNode = graphSystem.CreateNode<WorldToRootNode>(graph);

            var headLookAtNode = graphSystem.CreateNode<AimConstraintNode>(graph);

            set.Connect(entityNode, leftArmIKNode, TwoBoneIKNode.KernelPorts.Input,
                NodeSet.ConnectionType.Feedback);

            set.Connect(entityNode, leftWorldToRootNode, WorldToRootNode.KernelPorts.Input,
                NodeSet.ConnectionType.Feedback);
            set.Connect(entityNode, leftWorldToRootNode, WorldToRootNode.KernelPorts.RootEntity,
                NodeSet.ConnectionType.Feedback);
            set.Connect(leftTargetEntityNode, leftWorldToRootNode,
                WorldToRootNode.KernelPorts.LocalToWorldToRemap);
            set.Connect(leftWorldToRootNode, WorldToRootNode.KernelPorts.Output, leftArmIKNode,
                TwoBoneIKNode.KernelPorts.Target);
            set.Connect(leftArmIKNode, TwoBoneIKNode.KernelPorts.Output, rightArmIKNode,
                TwoBoneIKNode.KernelPorts.Input);

            set.Connect(entityNode, rightWorldToRootNode, WorldToRootNode.KernelPorts.Input,
                NodeSet.ConnectionType.Feedback);
            set.Connect(entityNode, rightWorldToRootNode, WorldToRootNode.KernelPorts.RootEntity,
                NodeSet.ConnectionType.Feedback);
            set.Connect(rightTargetEntityNode, rightWorldToRootNode,
                WorldToRootNode.KernelPorts.LocalToWorldToRemap);
            set.Connect(rightWorldToRootNode, WorldToRootNode.KernelPorts.Output, rightArmIKNode,
                TwoBoneIKNode.KernelPorts.Target);
            set.Connect(rightArmIKNode, TwoBoneIKNode.KernelPorts.Output, headLookAtNode,
                AimConstraintNode.KernelPorts.Input);

            set.Connect(headLookAtNode, AimConstraintNode.KernelPorts.Output, entityNode);

            var stream = AnimationStream.FromDefaultValues(rig);
            var leftArmIK = new TwoBoneIKNode.SetupMessage
            {
                RootIndex = setup.LeftArmIK.x,
                MidIndex = setup.LeftArmIK.y,
                TipIndex = setup.LeftArmIK.z,
                TargetOffset = new RigidTransform(quaternion.identity, float3.zero)
            };

            var rightArmIK = new TwoBoneIKNode.SetupMessage
            {
                RootIndex = setup.RightArmIK.x,
                MidIndex = setup.RightArmIK.y,
                TipIndex = setup.RightArmIK.z,
                TargetOffset = new RigidTransform(quaternion.identity, float3.zero)
            };

            var headAim = new AimConstraintNode.SetupMessage
            {
                Index = setup.HeadIndex,
                LocalAimAxis = setup.HeadLocalAimAxis,
                LocalAxesMask = math.bool3(true)
            };

            set.SendMessage(leftArmIKNode, TwoBoneIKNode.SimulationPorts.Rig, rig);
            set.SendMessage(leftArmIKNode, TwoBoneIKNode.SimulationPorts.ConstraintSetup, leftArmIK);
            set.SendMessage(leftWorldToRootNode, WorldToRootNode.SimulationPorts.Rig, rig);

            set.SendMessage(rightArmIKNode, TwoBoneIKNode.SimulationPorts.Rig, rig);
            set.SendMessage(rightArmIKNode, TwoBoneIKNode.SimulationPorts.ConstraintSetup, rightArmIK);
            set.SendMessage(rightWorldToRootNode, WorldToRootNode.SimulationPorts.Rig, rig);

            // Same as above but now applying static corrections to the head bone for a look at
            set.SendMessage(headLookAtNode, AimConstraintNode.SimulationPorts.Rig, rig);
            set.SendMessage(headLookAtNode, AimConstraintNode.SimulationPorts.ConstraintSetup, headAim);
            set.SetPortArraySize(headLookAtNode, AimConstraintNode.KernelPorts.SourcePositions, 1);
            set.SetPortArraySize(headLookAtNode, AimConstraintNode.KernelPorts.SourceOffsets, 1);
            set.SetPortArraySize(headLookAtNode, AimConstraintNode.KernelPorts.SourceWeights, 1);
            set.SetData(headLookAtNode, AimConstraintNode.KernelPorts.SourcePositions, 0,
                stream.GetLocalToRootTranslation(headAim.Index) + math.float3(0f, -2f, 0.5f));
            set.SetData(headLookAtNode, AimConstraintNode.KernelPorts.SourceOffsets, 0, quaternion.identity);
            set.SetData(headLookAtNode, AimConstraintNode.KernelPorts.SourceWeights, 0, 1f);

            return data;
        }

        protected override void DestroyGraph(Entity entity, ProcessLateAnimationGraph graphSystem, ref IKGraphData data)
        {
            graphSystem.Dispose(data.Graph);
        }
    }
}
