using Unity.Animation;
using Unity.Animation.Hybrid;
using Unity.Collections;
using Unity.DataFlowGraph;
using Unity.Entities;
using UnityEngine;

#if UNITY_EDITOR
[RequireComponent(typeof(RigComponent))]
[ConverterVersion("SynchronizeTagsSample", 8)]
[UpdateInGroup(typeof(GameObjectConversionGroup))]
[UpdateAfter(typeof(PhaseMatchingAuthoringComponent))]
public class SynchronizeTagsSampleAuthoringComponent : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (!dstManager.HasComponent<SampleClip>(entity))
            return;

        var motions = dstManager.GetBuffer<SampleClip>(entity);
        var motionCount = motions.Length;

        dstManager.AddComponentData(entity, new SynchronizeTagsSample
        {
            SynchronizationTagType = nameof(HumanoidGait)
        });

        var motionTimes = dstManager.AddBuffer<SampleClipTime>(entity);
        for (int i = 0; i < motionCount; i++)
        {
            motionTimes.Add(new SampleClipTime { Value = 0 });
        }

        dstManager.AddComponent<ProcessDefaultAnimationGraph.AnimatedRootMotion>(entity);
    }
}
#endif

public struct SynchronizeTagsSample : IComponentData
{
    public StringHash SynchronizationTagType;
}

public struct SampleClipTime : IBufferElementData
{
    public float Value;
}

public struct SynchronizeTagsGraphComponent : ISystemStateComponentData
{
    public GraphHandle                                              GraphHandle;
    public NodeHandle<ComponentNode>                                EntityNode;
    public NodeHandle<NMixerNode>                                   NMixerNode;
    public NodeHandle<SynchronizeMotionTimerOnSyncTagsNode>         SyncTimerNode;
}


[UpdateAfter(typeof(BlendWeightApplyState))]
[UpdateBefore(typeof(DefaultAnimationSystemGroup))]
public class SynchronizeTagsSystem : SystemBase
{
    ProcessDefaultAnimationGraph m_GraphSystem;
    EndSimulationEntityCommandBufferSystem m_ECBSystem;
    EntityQuery m_AnimationDataQuery;

    protected override void OnUpdate()
    {
        CompleteDependency();

        var set = m_GraphSystem.Set;
        var ecb = m_ECBSystem.CreateCommandBuffer();

        // Create graph for entities that have a SampleClip but no graph (SynchronizeTagsGraphComponent)
        Entities
            .WithName("CreateGraph")
            .WithNone<SynchronizeTagsGraphComponent>()
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((Entity e, in Rig rig, in SynchronizeTagsSample synchronizeTagsSample, in DynamicBuffer<SampleClip> animations) =>
            {
                var anims = animations.ToNativeArray(Allocator.Temp);
                var graph = CreateGraph(e, in rig, in synchronizeTagsSample, ref anims);
                ecb.AddComponent(e, graph);
                anims.Dispose();
            }).Run();

        // Update normalized time.
        Entities
            .WithName("UpdateGraph")
            .WithAll<SynchronizeTagsSample>()
            .WithoutBurst()
            .ForEach((Entity e, in SynchronizeTagsGraphComponent graph) =>
            {
                set.SetData(graph.SyncTimerNode, SynchronizeMotionTimerOnSyncTagsNode.KernelPorts.DeltaTime, World.Time.DeltaTime);
            }).Run();

        // Destroy graph for which the entity is missing the SampleClip
        Entities
            .WithName("DestroyGraph")
            .WithAll<SynchronizeTagsSample>()
            .WithNone<SampleClip>()
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((Entity e, ref SynchronizeTagsGraphComponent graph) =>
            {
                DestroyGraph(ref graph);
            }).Run();

        if (m_AnimationDataQuery.CalculateEntityCount() > 0)
            ecb.RemoveComponent(m_AnimationDataQuery, typeof(SynchronizeTagsGraphComponent));
    }

    protected SynchronizeTagsGraphComponent CreateGraph(Entity entity, in Rig rig, in SynchronizeTagsSample synchronizeTagsSample, ref NativeArray<SampleClip> animations)
    {
        var graphHandle = m_GraphSystem.CreateGraph();
        var graph = new SynchronizeTagsGraphComponent
        {
            GraphHandle = graphHandle,
            NMixerNode = m_GraphSystem.CreateNode<NMixerNode>(graphHandle),
            SyncTimerNode = m_GraphSystem.CreateNode<SynchronizeMotionTimerOnSyncTagsNode>(graphHandle),
            EntityNode = m_GraphSystem.CreateNode(graphHandle, entity)
        };

        m_GraphSystem.Set.SendMessage(graph.SyncTimerNode, (InputPortID)SynchronizeMotionTimerOnSyncTagsNode.SimulationPorts.MotionCount, animations.Length);
        m_GraphSystem.Set.SendMessage(graph.SyncTimerNode, SynchronizeMotionTimerOnSyncTagsNode.SimulationPorts.SynchronizationTagType, synchronizeTagsSample.SynchronizationTagType);
        m_GraphSystem.Set.SendMessage(graph.NMixerNode, NMixerNode.SimulationPorts.Rig, rig);
        m_GraphSystem.Set.SetPortArraySize(graph.NMixerNode, NMixerNode.KernelPorts.Inputs, animations.Length);
        m_GraphSystem.Set.SetPortArraySize(graph.NMixerNode, NMixerNode.KernelPorts.Weights, animations.Length);


        ClipConfiguration clipConfiguration = new ClipConfiguration
        {
            Mask = ClipConfigurationMask.NormalizedTime | ClipConfigurationMask.LoopTime | ClipConfigurationMask.RootMotionFromVelocity,
            MotionID = 0,
        };

        // Connect kernel ports
        m_GraphSystem.Set.Connect(graph.EntityNode, graph.SyncTimerNode, SynchronizeMotionTimerOnSyncTagsNode.KernelPorts.SampleClipTimersInput, NodeSet.ConnectionType.Feedback);
        m_GraphSystem.Set.Connect(graph.EntityNode, graph.SyncTimerNode, SynchronizeMotionTimerOnSyncTagsNode.KernelPorts.WeightComponent, NodeSet.ConnectionType.Feedback);
        m_GraphSystem.Set.Connect(graph.EntityNode, graph.SyncTimerNode, SynchronizeMotionTimerOnSyncTagsNode.KernelPorts.Durations, NodeSet.ConnectionType.Feedback);
        m_GraphSystem.Set.Connect(graph.EntityNode, graph.SyncTimerNode, SynchronizeMotionTimerOnSyncTagsNode.KernelPorts.Motions, NodeSet.ConnectionType.Feedback);
        m_GraphSystem.Set.Connect(graph.EntityNode, graph.SyncTimerNode, SynchronizeMotionTimerOnSyncTagsNode.KernelPorts.WeightThresholds, NodeSet.ConnectionType.Feedback);
        m_GraphSystem.Set.Connect(graph.SyncTimerNode, SynchronizeMotionTimerOnSyncTagsNode.KernelPorts.SampleClipTimersOutput, graph.EntityNode);

        m_GraphSystem.Set.Connect(graph.NMixerNode, NMixerNode.KernelPorts.Output, graph.EntityNode);

        for (int i = 0; i < animations.Length; i++)
        {
            var uberClipNode = m_GraphSystem.CreateNode<UberClipNode>(graphHandle);
            var weightValueNode = m_GraphSystem.CreateNode<GetBufferElementValueNode>(graphHandle);
            var timeValueNode = m_GraphSystem.CreateNode<GetBufferElementValueNode>(graphHandle);
            var deltaTimeValueNode = m_GraphSystem.CreateNode<GetBufferElementValueNode>(graphHandle);

            m_GraphSystem.Set.SendMessage(uberClipNode, UberClipNode.SimulationPorts.Rig, rig);
            m_GraphSystem.Set.SendMessage(uberClipNode, UberClipNode.SimulationPorts.Clip, animations[i].Clip);
            m_GraphSystem.Set.SendMessage(uberClipNode, UberClipNode.SimulationPorts.Configuration, clipConfiguration);

            m_GraphSystem.Set.SetData(weightValueNode, GetBufferElementValueNode.KernelPorts.Index, i);
            m_GraphSystem.Set.SetData(timeValueNode, GetBufferElementValueNode.KernelPorts.Index, i);
            m_GraphSystem.Set.SetData(deltaTimeValueNode, GetBufferElementValueNode.KernelPorts.Index, i);

            m_GraphSystem.Set.Connect(graph.SyncTimerNode, SynchronizeMotionTimerOnSyncTagsNode.KernelPorts.Weights, weightValueNode, GetBufferElementValueNode.KernelPorts.Input);
            m_GraphSystem.Set.Connect(graph.SyncTimerNode, SynchronizeMotionTimerOnSyncTagsNode.KernelPorts.Timers, timeValueNode, GetBufferElementValueNode.KernelPorts.Input);
            m_GraphSystem.Set.Connect(graph.SyncTimerNode, SynchronizeMotionTimerOnSyncTagsNode.KernelPorts.DeltaTimesOutput, deltaTimeValueNode, GetBufferElementValueNode.KernelPorts.Input);
            m_GraphSystem.Set.Connect(weightValueNode, GetBufferElementValueNode.KernelPorts.Output, graph.NMixerNode, NMixerNode.KernelPorts.Weights, i);

            m_GraphSystem.Set.Connect(timeValueNode, GetBufferElementValueNode.KernelPorts.Output, uberClipNode, UberClipNode.KernelPorts.Time);
            m_GraphSystem.Set.Connect(deltaTimeValueNode, GetBufferElementValueNode.KernelPorts.Output, uberClipNode, UberClipNode.KernelPorts.DeltaTime);
            m_GraphSystem.Set.Connect(uberClipNode, UberClipNode.KernelPorts.Output, graph.NMixerNode, NMixerNode.KernelPorts.Inputs, i);
        }

        return graph;
    }

    protected void DestroyGraph(ref SynchronizeTagsGraphComponent graph)
    {
        m_GraphSystem.Dispose(graph.GraphHandle);
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        m_GraphSystem = World.GetOrCreateSystem<ProcessDefaultAnimationGraph>();
        // Increase the reference count on the graph system so it knows
        // that we want to use it.
        m_GraphSystem.AddRef();
        m_ECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        m_AnimationDataQuery = GetEntityQuery(new EntityQueryDesc()
        {
            None = new ComponentType[] { typeof(SampleClip) },
            All = new ComponentType[] { typeof(SynchronizeTagsSample), typeof(SynchronizeTagsGraphComponent) }
        });

        m_GraphSystem.Set.RendererModel = NodeSet.RenderExecutionModel.Islands;
    }

    protected override void OnDestroy()
    {
        if (m_GraphSystem == null)
            return;

        // Clean up all our nodes in the graph
        Entities
            .WithAll<SynchronizeTagsSample>()
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((Entity e, ref SynchronizeTagsGraphComponent graph) =>
            {
                DestroyGraph(ref graph);
            }).Run();

        // Decrease the reference count on the graph system so it knows
        // that we are done using it.
        m_GraphSystem.RemoveRef();
        base.OnDestroy();
    }
}
