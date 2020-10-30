using Unity.Animation;
using Unity.Entities;
using UnityEngine;

public class DurationComponent : MonoBehaviour
{
    [Range(0, 10)]
    public float Duration;

    public void OnEnable()
    {
        var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<DurationSystem>();
        system.Duration = this;
    }

    public void SetDuration(float value)
    {
        Duration = value;
    }
}

[UpdateBefore(typeof(DefaultAnimationSystemGroup))]
public class DurationSystem : SystemBase
{
    public DurationComponent Duration;
    ProcessDefaultAnimationGraph m_GraphSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_GraphSystem = World.GetOrCreateSystem<ProcessDefaultAnimationGraph>();
    }

    protected override void OnUpdate()
    {
        if (Duration == null)
            return;
        var value = Duration.Duration;
        Entities
            .WithoutBurst()
            .ForEach((Entity entity, ref InertialBlendingGraphData state) =>
            {
                m_GraphSystem.Set.SendMessage(state.TransitionNode, TransitionByBoolNode.SimulationPorts.Duration, value);
            }).Run();
    }
}
