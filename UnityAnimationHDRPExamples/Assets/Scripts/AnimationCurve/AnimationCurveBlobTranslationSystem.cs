using Unity.Animation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

using UnityEngine;

public class AnimationCurveBlobTranslationSystem : JobComponentSystem
{
    [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
    struct AnimCurveJobForEach : IJobForEach<Translation, CurveBlobComponent>
    {
        public float Time;

        public void Execute(ref Translation pos, ref CurveBlobComponent curve)
        {
            pos.Value.y = 3.0f * KeyframeCurveEvaluator.Evaluate(Time, curve.CurveBlobRef);
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new AnimCurveJobForEach
        {
            Time = Mathf.Repeat((float)Time.ElapsedTime, 1.0f)
        };

        return job.Schedule(this, inputDeps);
    }
}
