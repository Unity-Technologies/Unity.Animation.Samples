using Unity.Animation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

using UnityEngine;

public class AnimationCurveTranslationSystem : JobComponentSystem
{
    EntityQuery m_Group;
    KeyframeCurve m_Curve;

    protected override void OnCreate()
    {
        m_Curve = new KeyframeCurve(3, Allocator.Persistent);
        m_Curve[0] = new Unity.Animation.Keyframe { InTangent = 1.996254f, OutTangent = 1.996254f, Time = 0, Value = 0 };
        m_Curve[1] = new Unity.Animation.Keyframe { InTangent = 1.996254f, OutTangent = -2.042508f, Time = 0.5042475f, Value = 1.006606f };
        m_Curve[2] = new Unity.Animation.Keyframe { InTangent = -2.042508f, OutTangent = -2.042508f, Time = 0.99823f, Value = -0.002357483f };

        m_Group = GetEntityQuery(typeof(Translation), ComponentType.ReadOnly<CurveTag>());
    }

    protected override void OnDestroy()
    {
        m_Curve.Dispose();
    }

    [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
    struct AnimCurveJob : IJobChunk
    {
        public float Time;
        public ArchetypeChunkComponentType<Translation> TranslationType;
        [ReadOnly] public ArchetypeChunkComponentType<CurveTag> CurveTagType;
        public KeyframeCurve Curve;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var chunkTranslations = chunk.GetNativeArray(TranslationType);

            for (var i = 0; i < chunk.Count; i++)
            {
                var pos = chunkTranslations[i].Value;
                pos.y = 3.0f * KeyframeCurveEvaluator.Evaluate(Time, ref Curve);

                chunkTranslations[i] = new Translation
                {
                    Value = pos
                };
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var translationType = GetArchetypeChunkComponentType<Translation>();
        var curveTagType = GetArchetypeChunkComponentType<CurveTag>(true);

        var job = new AnimCurveJob()
        {
            Time = Mathf.Repeat((float)Time.ElapsedTime, 1.0f),
            TranslationType = translationType,
            CurveTagType = curveTagType,
            Curve = m_Curve
        };

        return job.Schedule(m_Group, inputDeps);
    }
}
