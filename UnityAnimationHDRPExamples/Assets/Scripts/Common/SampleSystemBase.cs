using Unity.Entities;
using Unity.Animation;
using Unity.Collections;
using Unity.DataFlowGraph;

public interface ISampleSetup : IComponentData { };

public interface ISampleData : ISystemStateComponentData { };

public abstract class SampleSystemBase<TSampleSetup, TSampleData, TAnimationGraphTag, TAnimationGraph> : ComponentSystem
    where TSampleSetup : struct, ISampleSetup
    where TSampleData  : struct, ISampleData
    where TAnimationGraphTag : struct, IGraphTag
    where TAnimationGraph  : ComponentSystemBase, IGraphSystem<TAnimationGraphTag>
{
    protected TAnimationGraph  m_GraphSystem;

    EntityQueryBuilder.F_EDD<Rig, TSampleSetup> m_CreateLambda;
    EntityQueryBuilder.F_ED<TSampleData>        m_DestroyLambda;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_GraphSystem  = World.GetOrCreateSystem<TAnimationGraph>();
        m_GraphSystem.AddRef();
        m_GraphSystem.Set.RendererModel  = NodeSet.RenderExecutionModel.Islands;

        m_CreateLambda = (Entity e, ref Rig rig, ref TSampleSetup setup) =>
        {
            var data = CreateGraph(e, ref rig, m_GraphSystem, ref setup);
            PostUpdateCommands.AddComponent(e, data);
        };

        m_DestroyLambda = (Entity e, ref TSampleData data) =>
        {
            DestroyGraph(e, m_GraphSystem, ref data);
            PostUpdateCommands.RemoveComponent<TSampleData>(e);
        };
    }

    protected override void OnDestroy()
    {
        if (m_GraphSystem == null)
            return;

        var cmdBuffer = new EntityCommandBuffer(Allocator.Temp);
        Entities.ForEach((Entity e, ref TSampleData data) =>
        {
            DestroyGraph(e, m_GraphSystem, ref data);
            cmdBuffer.RemoveComponent<TSampleData>(e);
        });

        cmdBuffer.Playback(EntityManager);
        cmdBuffer.Dispose();

        m_GraphSystem.RemoveRef();
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        // Create graph
        Entities.WithNone<TSampleData>().ForEach(m_CreateLambda);

        // Destroy graph
        Entities.WithNone<TSampleSetup>().ForEach(m_DestroyLambda);
    }

    protected abstract TSampleData CreateGraph(Entity entity, ref Rig rig, TAnimationGraph graphSystem, ref TSampleSetup setup);

    protected abstract void DestroyGraph(Entity entity, TAnimationGraph graphSystem, ref TSampleData data);
}
