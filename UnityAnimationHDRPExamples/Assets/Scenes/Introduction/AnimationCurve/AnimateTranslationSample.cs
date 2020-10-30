using Unity.Animation;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

#if UNITY_EDITOR
using Unity.Animation.Hybrid;

public class AnimateTranslationSample : MonoBehaviour, IConvertGameObjectToEntity
{
    public UnityEngine.AnimationCurve TranslationCurve;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new AnimateTranslation
        {
            TranslationCurve = conversionSystem.BlobAssetStore.GetAnimationCurve(TranslationCurve)
        });
    }
}
#endif

struct AnimateTranslation : IComponentData
{
    public BlobAssetReference<AnimationCurveBlob> TranslationCurve;
}

public class AnimationCurveBlobTranslationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var time = Mathf.Repeat((float)Time.ElapsedTime, 1.0F);
        Dependency = Entities.ForEach((ref Translation position, in AnimateTranslation translation) =>
        {
            float value = AnimationCurveEvaluator.Evaluate(time, translation.TranslationCurve);
            position.Value.y = value;
        }).Schedule(Dependency);
    }
}
