using Unity.Animation;
using Unity.Entities;

using UnityEngine;

[RequiresEntityConversion]
public class AnimationCurveAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new CurveTag());
    }
}
