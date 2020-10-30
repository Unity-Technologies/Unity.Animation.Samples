using Unity.Animation;
using Unity.Collections;
using Unity.DataFlowGraph;
using Unity.Entities;
#if UNITY_EDITOR
using UnityEngine;
using Unity.Animation.Hybrid;

public class PerformanceInertialMotionBlendingGraph : AnimationGraphBase
{
    public AnimationClip[] Clips;

    public override void AddGraphSetupComponent(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (Clips == null || Clips.Length == 0)
        {
            UnityEngine.Debug.LogError("No clips specified for performance test!");
            return;
        }

        var clipBuffer = dstManager.AddBuffer<PerformanceSetupAsset>(entity);
        for (int i = 0; i < Clips.Length; ++i)
            clipBuffer.Add(new PerformanceSetupAsset { Clip = Clips[i].ToDenseClip() });

        dstManager.AddComponent<InertialMotionBlending.PerformanceSetup>(entity);

        dstManager.AddComponent<DeltaTime>(entity);
    }
}
#endif

namespace InertialMotionBlending
{
    public struct PerformanceSetup : ISampleSetup {};
    public struct PerformanceData : ISampleData
    {
        public GraphHandle Graph;
        public NodeHandle<InertialBlendingNode> InertialNode;
    }

    public class PerformanceGraphSystem : SampleSystemBase<
        PerformanceSetup,
        PerformanceData,
        ProcessDefaultAnimationGraph
    >
    {
        static Unity.Mathematics.Random s_Random = new Unity.Mathematics.Random(0x12345678);

        protected override PerformanceData CreateGraph(Entity entity, ref Rig rig, ProcessDefaultAnimationGraph graphSystem, ref PerformanceSetup setup)
        {
            if (!EntityManager.HasComponent<PerformanceSetupAsset>(entity))
                throw new System.InvalidOperationException("Entity is missing a PerformanceSetupAsset IBufferElementData");

            if (EntityManager.GetBuffer<PerformanceSetupAsset>(entity).Length != 2)
            {
                throw new System.InvalidOperationException("Entity needs 2 PerformanceSetupAssets");
            }

            var set = graphSystem.Set;
            var data = new PerformanceData
            {
                Graph = graphSystem.CreateGraph()
            };
            var deltaTimeNode = graphSystem.CreateNode<ConvertDeltaTimeToFloatNode>(data.Graph);
            var timeNode = graphSystem.CreateNode<TimeCounterNode>(data.Graph);
            var entityNode = graphSystem.CreateNode(data.Graph, entity);

            var clipBuffer = EntityManager.GetBuffer<PerformanceSetupAsset>(entity);
            var clipPlayerNodes = new NativeArray<NodeHandle<ClipPlayerNode>>(clipBuffer.Length, Allocator.Temp);

            // Clips to binary mixer
            data.InertialNode = graphSystem.CreateNode<InertialBlendingNode>(data.Graph);
            clipPlayerNodes[0] = graphSystem.CreateNode<ClipPlayerNode>(data.Graph);
            clipPlayerNodes[1] = graphSystem.CreateNode<ClipPlayerNode>(data.Graph);

            set.SetData(clipPlayerNodes[0], ClipPlayerNode.KernelPorts.Speed, s_Random.NextFloat(0.1f, 1f));
            set.SetData(clipPlayerNodes[1], ClipPlayerNode.KernelPorts.Speed, s_Random.NextFloat(0.1f, 1f));
            set.SetData(timeNode, TimeCounterNode.KernelPorts.Speed, 1f);

            set.Connect(entityNode, deltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Input);
            set.Connect(deltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, clipPlayerNodes[0], ClipPlayerNode.KernelPorts.DeltaTime);
            set.Connect(deltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, clipPlayerNodes[1], ClipPlayerNode.KernelPorts.DeltaTime);
            set.Connect(deltaTimeNode, ConvertDeltaTimeToFloatNode.KernelPorts.Output, timeNode, TimeCounterNode.KernelPorts.DeltaTime);
            set.Connect(clipPlayerNodes[0], ClipPlayerNode.KernelPorts.Output, data.InertialNode, InertialBlendingNode.KernelPorts.Input0);
            set.Connect(clipPlayerNodes[1], ClipPlayerNode.KernelPorts.Output, data.InertialNode, InertialBlendingNode.KernelPorts.Input1);
            set.Connect(timeNode, TimeCounterNode.KernelPorts.Time, data.InertialNode, InertialBlendingNode.KernelPorts.Time);
            set.Connect(timeNode, TimeCounterNode.KernelPorts.OutputDeltaTime, data.InertialNode, InertialBlendingNode.KernelPorts.DeltaTime);
            set.Connect(data.InertialNode, InertialBlendingNode.KernelPorts.Output, entityNode, NodeSet.ConnectionType.Feedback);

            set.SendMessage(clipPlayerNodes[0], ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime });
            set.SendMessage(clipPlayerNodes[1], ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime });
            set.SendMessage(clipPlayerNodes[0], ClipPlayerNode.SimulationPorts.Rig, rig);
            set.SendMessage(clipPlayerNodes[1], ClipPlayerNode.SimulationPorts.Rig, rig);
            set.SendMessage(clipPlayerNodes[0], ClipPlayerNode.SimulationPorts.Clip, clipBuffer[0].Clip);
            set.SendMessage(clipPlayerNodes[1], ClipPlayerNode.SimulationPorts.Clip, clipBuffer[1].Clip);
            set.SendMessage(data.InertialNode, InertialBlendingNode.SimulationPorts.Rig, rig);
            set.SendMessage(data.InertialNode, InertialBlendingNode.SimulationPorts.Duration, 1);

            return data;
        }

        protected override void DestroyGraph(Entity entity, ProcessDefaultAnimationGraph graphSystem, ref PerformanceData data)
        {
            graphSystem.Dispose(data.Graph);
        }
    }

    [UpdateBefore(typeof(DefaultAnimationSystemGroup))]
    public class BlendTriggerSystem : SystemBase
    {
        double m_LastTriggerTime;
        ProcessDefaultAnimationGraph m_GraphSystem;
        bool m_CurrentClipSource = false;

        protected override void OnStartRunning()
        {
            m_GraphSystem = World.GetOrCreateSystem<ProcessDefaultAnimationGraph>();
            m_LastTriggerTime = Time.ElapsedTime;
            TriggerBlend();
        }

        protected override void OnUpdate()
        {
            float triggerPeriod = 1.5f;

            if (m_LastTriggerTime + triggerPeriod < Time.ElapsedTime)
            {
                m_LastTriggerTime = Time.ElapsedTime;
                TriggerBlend();
            }
        }

        void TriggerBlend()
        {
            m_CurrentClipSource = !m_CurrentClipSource;
            Entities.WithoutBurst().ForEach((ref PerformanceData data) => {
                m_GraphSystem.Set.SendMessage(data.InertialNode, InertialBlendingNode.SimulationPorts.ClipSource, m_CurrentClipSource);
            }).Run();
        }
    }
}
