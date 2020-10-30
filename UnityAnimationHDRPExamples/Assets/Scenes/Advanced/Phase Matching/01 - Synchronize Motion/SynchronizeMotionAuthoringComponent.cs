using Unity.Animation;
using Unity.Collections;
using Unity.DataFlowGraph;
using Unity.Entities;
using UnityEngine;

#if UNITY_EDITOR
[ConverterVersion("SynchronizeMotion", 1)]
[UpdateInGroup(typeof(GameObjectConversionGroup))]
[UpdateAfter(typeof(PhaseMatchingAuthoringComponent))]
public class SynchronizeMotionAuthoringComponent : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new SynchronizeMotionSample {});
        dstManager.AddComponentData(entity, new NormalizedTimeComponent
        {
            Value = 0.0f
        });
        dstManager.AddComponent<ProcessDefaultAnimationGraph.AnimatedRootMotion>(entity);
    }
}
#endif

public struct SynchronizeMotionSample : IComponentData
{
}

public struct NormalizedTimeComponent : IComponentData
{
    public float Value;
}

public struct SynchronizeMotionGraphComponent : ISystemStateComponentData
{
    public GraphHandle                                              GraphHandle;
    public NodeHandle<NMixerNode>                                   NMixerNode;
    public NodeHandle<ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode>   ComputeSynchronizeMotionNormalizedTimeNode;
    public NodeHandle<ComponentNode>                                EntityNode;
}

[UpdateAfter(typeof(BlendWeightApplyState))]
[UpdateBefore(typeof(DefaultAnimationSystemGroup))]
public class SynchronizeMotionSystem : SystemBase
{
    ProcessDefaultAnimationGraph m_GraphSystem;
    EndSimulationEntityCommandBufferSystem m_ECBSystem;
    EntityQuery m_AnimationDataQuery;

    protected override void OnUpdate()
    {
        CompleteDependency();

        var set = m_GraphSystem.Set;
        var ecb = m_ECBSystem.CreateCommandBuffer();

        // Create graph for entities that have a SampleClip but no graph (SynchronizeMotionGraphComponent)
        Entities
            .WithName("CreateGraph")
            .WithAll<SynchronizeMotionSample>()
            .WithNone<SynchronizeMotionGraphComponent>()
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((Entity e, in Rig rig, in DynamicBuffer<SampleClip> animations) =>
            {
                var anims = animations.ToNativeArray(Allocator.Temp);
                var graph = CreateGraph(e, in rig, ref anims);
                ecb.AddComponent(e, graph);
                anims.Dispose();
            }).Run();

        // Update normalized time.
        Entities
            .WithName("UpdateGraph")
            .WithAll<SynchronizeMotionSample>()
            .WithoutBurst()
            .ForEach((Entity e, in SynchronizeMotionGraphComponent graph) =>
            {
                set.SetData(graph.ComputeSynchronizeMotionNormalizedTimeNode, ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode.KernelPorts.DeltaTime, World.Time.DeltaTime);
            }).Run();

        // Destroy graph for which the entity is missing the SampleClip
        Entities
            .WithName("DestroyGraph")
            .WithAll<SynchronizeMotionSample>()
            .WithNone<SampleClip>()
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((Entity e, ref SynchronizeMotionGraphComponent graph) =>
            {
                DestroyGraph(ref graph);
            }).Run();

        if (m_AnimationDataQuery.CalculateEntityCount() > 0)
            ecb.RemoveComponent(m_AnimationDataQuery, typeof(SynchronizeMotionGraphComponent));
    }

    protected SynchronizeMotionGraphComponent CreateGraph(Entity entity, in Rig rig, ref NativeArray<SampleClip> animations)
    {
        var graphHandle = m_GraphSystem.CreateGraph();
        var graph = new SynchronizeMotionGraphComponent
        {
            GraphHandle = graphHandle,
            NMixerNode = m_GraphSystem.CreateNode<NMixerNode>(graphHandle),
            ComputeSynchronizeMotionNormalizedTimeNode = m_GraphSystem.CreateNode<ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode>(graphHandle),
            EntityNode = m_GraphSystem.CreateNode(graphHandle, entity)
        };

        m_GraphSystem.Set.SendMessage(graph.NMixerNode, NMixerNode.SimulationPorts.Rig, rig);
        m_GraphSystem.Set.SendMessage(graph.ComputeSynchronizeMotionNormalizedTimeNode, ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode.SimulationPorts.MotionCount, animations.Length);
        m_GraphSystem.Set.SetPortArraySize(graph.NMixerNode, NMixerNode.KernelPorts.Inputs, animations.Length);
        m_GraphSystem.Set.SetPortArraySize(graph.NMixerNode, NMixerNode.KernelPorts.Weights, animations.Length);

        // Connect kernel ports
        m_GraphSystem.Set.Connect(graph.NMixerNode, NMixerNode.KernelPorts.Output, graph.EntityNode);

        m_GraphSystem.Set.Connect(graph.EntityNode, graph.ComputeSynchronizeMotionNormalizedTimeNode, ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode.KernelPorts.NormalizedTimeComponentInput, NodeSet.ConnectionType.Feedback);
        m_GraphSystem.Set.Connect(graph.EntityNode, graph.ComputeSynchronizeMotionNormalizedTimeNode, ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode.KernelPorts.WeightComponent, NodeSet.ConnectionType.Feedback);
        m_GraphSystem.Set.Connect(graph.EntityNode, graph.ComputeSynchronizeMotionNormalizedTimeNode, ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode.KernelPorts.Durations, NodeSet.ConnectionType.Feedback);
        m_GraphSystem.Set.Connect(graph.EntityNode, graph.ComputeSynchronizeMotionNormalizedTimeNode, ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode.KernelPorts.WeightThresholds, NodeSet.ConnectionType.Feedback);
        m_GraphSystem.Set.Connect(graph.ComputeSynchronizeMotionNormalizedTimeNode, ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode.KernelPorts.NormalizedTimeComponentOutput, graph.EntityNode);

        ClipConfiguration clipConfiguration = new ClipConfiguration
        {
            Mask = ClipConfigurationMask.NormalizedTime | ClipConfigurationMask.LoopTime | ClipConfigurationMask.RootMotionFromVelocity,
            MotionID = 0,
        };

        for (int i = 0; i < animations.Length; i++)
        {
            var uberClipNode = m_GraphSystem.CreateNode<UberClipNode>(graphHandle);
            var valueNode = m_GraphSystem.CreateNode<GetBufferElementValueNode>(graphHandle);

            m_GraphSystem.Set.SendMessage(uberClipNode, UberClipNode.SimulationPorts.Rig, rig);
            m_GraphSystem.Set.SendMessage(uberClipNode, UberClipNode.SimulationPorts.Clip, animations[i].Clip);
            m_GraphSystem.Set.SendMessage(uberClipNode, UberClipNode.SimulationPorts.Configuration, clipConfiguration);

            m_GraphSystem.Set.SetData(valueNode, GetBufferElementValueNode.KernelPorts.Index, i);
            m_GraphSystem.Set.Connect(graph.ComputeSynchronizeMotionNormalizedTimeNode, ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode.KernelPorts.Weights, valueNode, GetBufferElementValueNode.KernelPorts.Input);
            m_GraphSystem.Set.Connect(valueNode, GetBufferElementValueNode.KernelPorts.Output, graph.NMixerNode, NMixerNode.KernelPorts.Weights, i);

            m_GraphSystem.Set.Connect(graph.ComputeSynchronizeMotionNormalizedTimeNode, ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode.KernelPorts.NormalizedTime, uberClipNode, UberClipNode.KernelPorts.Time);
            m_GraphSystem.Set.Connect(graph.ComputeSynchronizeMotionNormalizedTimeNode, ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode.KernelPorts.DeltaTimeOutput, uberClipNode, UberClipNode.KernelPorts.DeltaTime);

            m_GraphSystem.Set.Connect(uberClipNode, UberClipNode.KernelPorts.Output, graph.NMixerNode, NMixerNode.KernelPorts.Inputs, i);
        }


        return graph;
    }

    protected void DestroyGraph(ref SynchronizeMotionGraphComponent graph)
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
            All = new ComponentType[] { typeof(SynchronizeMotionSample), typeof(SynchronizeMotionGraphComponent) }
        });

        m_GraphSystem.Set.RendererModel = NodeSet.RenderExecutionModel.Islands;
    }

    protected override void OnDestroy()
    {
        if (m_GraphSystem == null)
            return;

        // Clean up all our nodes in the graph
        Entities
            .WithAll<SynchronizeMotionSample>()
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((Entity e, ref SynchronizeMotionGraphComponent graph) =>
            {
                DestroyGraph(ref graph);
            }).Run();

        // Decrease the reference count on the graph system so it knows
        // that we are done using it.
        m_GraphSystem.RemoveRef();
        base.OnDestroy();
    }
}
