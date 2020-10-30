using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public abstract class AnimationGraphBase : MonoBehaviour
{
    public abstract void AddGraphSetupComponent(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem);

    public virtual void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs) {}

    public virtual void PreProcessData<T>(T data) {}
}
