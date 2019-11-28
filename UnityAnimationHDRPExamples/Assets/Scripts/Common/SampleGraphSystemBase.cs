using Unity.Entities;
using Unity.Animation;
using Unity.Collections;
using Unity.DataFlowGraph;

public interface ISampleSetup : IComponentData { };

public interface ISampleData : ISystemStateComponentData { };

public abstract class SampleSystemBase<TSampleSetup, TSampleData> : ComponentSystem
    where TSampleSetup : struct, ISampleSetup
    where TSampleData  : struct, ISampleData
{
    AnimationGraphSystem m_GraphSystem;
    
    EntityQueryBuilder.F_ED<TSampleSetup> m_CreateLambda;
    EntityQueryBuilder.F_ED<TSampleData> m_DestroyLambda;
    EntityQueryBuilder.F_ED<TSampleData> m_UpdateLambda;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_GraphSystem = World.GetOrCreateSystem<AnimationGraphSystem>();
        m_GraphSystem.AddRef();

        if (m_GraphSystem.Set.RendererModel != RenderExecutionModel.Islands)
            m_GraphSystem.Set.RendererModel = RenderExecutionModel.Islands;

        m_CreateLambda = (Entity e, ref TSampleSetup setup) =>
        {
            var data = CreateGraph(e, m_GraphSystem.Set, ref setup);
            PostUpdateCommands.AddComponent(e, data);
        };

        m_DestroyLambda = (Entity e, ref TSampleData data) =>
        {
            DestroyGraph(e, m_GraphSystem.Set, ref data);
            PostUpdateCommands.RemoveComponent(e, typeof(TSampleData));
        };

        m_UpdateLambda = (Entity e, ref TSampleData data) =>
        {
            UpdateGraph(e, m_GraphSystem.Set, ref data);
        };
    }

    protected override void OnDestroy()
    {
        if (m_GraphSystem == null)
            return;

        var cmdBuffer = new EntityCommandBuffer(Allocator.Temp);
        Entities.ForEach((Entity e, ref TSampleData data) =>
        {
            DestroyGraph(e, m_GraphSystem.Set, ref data);
            cmdBuffer.RemoveComponent(e, typeof(TSampleData));
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

        // Update graph
        Entities.WithAll<TSampleSetup, TSampleData>().ForEach(m_UpdateLambda);
    }

    protected abstract TSampleData CreateGraph(Entity entity, NodeSet set, ref TSampleSetup setup);

    protected abstract void DestroyGraph(Entity entity, NodeSet set, ref TSampleData data);

    protected abstract void UpdateGraph(Entity entity, NodeSet set, ref TSampleData data);
}
