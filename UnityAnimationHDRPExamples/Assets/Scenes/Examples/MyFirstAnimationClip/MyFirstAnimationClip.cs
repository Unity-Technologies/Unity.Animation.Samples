using Unity.Animation;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace MyFirstAnimationClip
{
    public class MyFirstAnimationClip : MonoBehaviour, IConvertGameObjectToEntity
    {
        public AnimationClip Clip;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new PlayClipComponent
            {
                Clip = ClipBuilder.AnimationClipToDenseClip(Clip)
            });
        }
    }
    
    public struct PlayClipComponent : IComponentData
    {
        public BlobAssetReference<Clip> Clip;
    }
    
    public struct PlayClipStateComponent : ISystemStateComponentData
    {
        public NodeHandle<DeltaTimeNode>  DeltaTimeNode;
        public NodeHandle<ClipPlayerNode> ClipPlayerNode;
        public NodeHandle<ComponentNode>  EntityNode;
    }

    [UpdateBefore(typeof(PreAnimationSystemGroup))]
    public class PlayClipSystem : JobComponentSystem
    {
        PreAnimationGraphSystem m_GraphSystem;
        EndSimulationEntityCommandBufferSystem m_ECBSystem;
        EntityQuery m_AnimationDataQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_GraphSystem = World.GetOrCreateSystem<PreAnimationGraphSystem>();
            // Increase the reference count on the graph system so it knows
            // that we want to use it.
            m_GraphSystem.AddRef();
            m_ECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            m_AnimationDataQuery = GetEntityQuery(new EntityQueryDesc()
            {
                None = new ComponentType[] { typeof(PlayClipComponent) },
                All = new ComponentType[] { typeof(PlayClipStateComponent) }
            });

            m_GraphSystem.Set.RendererModel = NodeSet.RenderExecutionModel.Islands;
        }

        protected override void OnDestroy()
        {
            if (m_GraphSystem == null)
                return;

            // Clean up all our nodes in the graph
            var nodes = m_GraphSystem.Set;
            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity e, ref PlayClipStateComponent data) =>
            {
                DestroyGraph(nodes, ref data);
            }).Run();

            // Decrease the reference count on the graph system so it knows
            // that we are done using it.
            m_GraphSystem.RemoveRef();
            base.OnDestroy();
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var set = m_GraphSystem.Set;
            var ecb = m_ECBSystem.CreateCommandBuffer();

            // Create graph for entities that have a PlayClipComponent but no graph
            Entities
                .WithName("CreateGraph")
                .WithNone<PlayClipStateComponent>()
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity e, ref Rig rig, ref PlayClipComponent animation) =>
                {
                    var state = CreateGraph(e, set, ref rig, ref animation);
                    ecb.AddComponent(e, state);
                    ecb.AddComponent(e, m_GraphSystem.Tag);
                }).Run();

            // Update graph if the animation component changed
            Entities
                .WithName("UpdateGraph")
                .WithChangeFilter<PlayClipComponent>()
                .WithoutBurst()
                .ForEach((Entity e, ref PlayClipComponent animation, ref PlayClipStateComponent state) =>
                {
                    set.SendMessage(state.ClipPlayerNode, ClipPlayerNode.SimulationPorts.Clip, animation.Clip);
                }).Run();

            // Destroy graph for which the entity is missing the AnimationComponent
            Entities
                .WithName("DestroyGraph")
                .WithNone<PlayClipComponent>()
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity e, ref PlayClipStateComponent state) =>
                {
                    DestroyGraph(set, ref state);
                }).Run();

            ecb.RemoveComponent(m_AnimationDataQuery, typeof(PlayClipStateComponent));

            return default;
        }
        
        static PlayClipStateComponent CreateGraph(Entity entity, NodeSet set, ref Rig rig, ref PlayClipComponent playClip)
        {
            var data = new PlayClipStateComponent
            {
                DeltaTimeNode = set.Create<DeltaTimeNode>(),
                ClipPlayerNode = set.Create<ClipPlayerNode>(),
                EntityNode = set.CreateComponentNode(entity)
            };
  
            // Connect kernel ports
            set.Connect(data.DeltaTimeNode, DeltaTimeNode.KernelPorts.DeltaTime, data.ClipPlayerNode, ClipPlayerNode.KernelPorts.DeltaTime);
            set.Connect(data.ClipPlayerNode, ClipPlayerNode.KernelPorts.Output, data.EntityNode);

            // Send messages to set parameters on the ClipPlayerNode
            set.SetData(data.ClipPlayerNode, ClipPlayerNode.KernelPorts.Speed, 1.0f);
            set.SendMessage(data.ClipPlayerNode, ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime });
            set.SendMessage(data.ClipPlayerNode, ClipPlayerNode.SimulationPorts.Rig, rig);
            set.SendMessage(data.ClipPlayerNode, ClipPlayerNode.SimulationPorts.Clip, playClip.Clip);

            return data;
        }

        static void DestroyGraph(NodeSet nodes, ref PlayClipStateComponent stateComponent)
        {
            nodes.Destroy(stateComponent.DeltaTimeNode);
            nodes.Destroy(stateComponent.ClipPlayerNode);
            nodes.Destroy(stateComponent.EntityNode);
        }
    }
}
