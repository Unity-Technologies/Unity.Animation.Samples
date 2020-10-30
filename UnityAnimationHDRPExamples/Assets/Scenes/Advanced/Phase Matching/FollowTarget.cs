using UnityEngine;
using Unity.Entities;
using Unity.Animation;
using Unity.Transforms;

public class FollowTarget : MonoBehaviour
{
    private void OnEnable()
    {
        // Can't have two GO holding this Monobehaviour since the last initialized will overwrite the
        // target GO.
        var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<PhaseMatchingSamples_FollowTargetApplyState>();
        system.Target = this.transform;
    }
}

[UpdateInGroup(typeof(LateAnimationSystemGroup))]
[UpdateAfter(typeof(ComputeRigMatrices))]
public class PhaseMatchingSamples_FollowTargetApplyState : SystemBase
{
    public Transform Target;
    protected override void OnUpdate()
    {
        CompleteDependency();

        Entities
            .WithAll<Rig>()
            .WithAny<SynchronizeMotionSample, SynchronizeTagsSample>()
            .WithoutBurst()
            .ForEach((Entity entity, in LocalToWorld localToWorld) =>
            {
                Target.position = localToWorld.Position;
            }).Run();
    }
}
