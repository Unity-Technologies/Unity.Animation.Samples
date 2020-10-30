using Unity.Animation;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Triggers a blend when the user presses SPACE
/// </summary>
[UpdateBefore(typeof(DefaultAnimationSystemGroup))]
public class BlendTriggerSystem : SystemBase
{
    ProcessDefaultAnimationGraph m_GraphSystem;
    bool m_CurrentClip = false;

    protected override void OnCreate()
    {
        m_GraphSystem = World.GetOrCreateSystem<ProcessDefaultAnimationGraph>();
    }

    protected override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            m_CurrentClip = !m_CurrentClip;
            Entities
                .WithName("InertialBlendingChangeClip")
                .WithoutBurst()
                .ForEach((Entity e, ref InertialBlendingGraphData graph) => {
                    m_GraphSystem.Set.SendMessage(graph.TransitionNode, TransitionByBoolNode.SimulationPorts.ClipSource, m_CurrentClip);
                }).Run();
        }
    }
}
