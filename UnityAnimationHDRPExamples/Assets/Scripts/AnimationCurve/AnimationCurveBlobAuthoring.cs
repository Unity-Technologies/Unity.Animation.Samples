using Unity.Animation;
using Unity.Entities;

using UnityEngine;

[RequiresEntityConversion]
public class AnimationCurveBlobAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public AnimationCurve Curve;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var curve = Curve.ToKeyframeCurveBlob();
        dstManager.AddComponentData(entity, new CurveBlobComponent() { CurveBlobRef = curve });
    }
}
