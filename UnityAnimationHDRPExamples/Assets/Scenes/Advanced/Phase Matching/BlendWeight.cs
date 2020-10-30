using System.Collections;
using System.Collections.Generic;
using Unity.Animation;
using Unity.Entities;
using UnityEngine;

public class BlendWeight : MonoBehaviour
{
    [Range(0, 1)]
    public float Weight;

    private void OnEnable()
    {
        var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BlendWeightApplyState>();
        system.blendWeight = this;
    }
}

[UpdateBefore(typeof(DefaultAnimationSystemGroup))]
public class BlendWeightApplyState : SystemBase
{
    public BlendWeight blendWeight;
    protected override void OnUpdate()
    {
        var value = blendWeight.Weight;
        Dependency = Entities
            .ForEach((Entity entity, ref WeightComponent weight) =>
        {
            weight.Value = value;
        }).Schedule(Dependency);
    }
}
