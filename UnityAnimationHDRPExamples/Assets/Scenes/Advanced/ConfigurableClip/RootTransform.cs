using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

public class RootTransform : MonoBehaviour
{
    public ConfigurableClipInput Input;

    void Update()
    {
        if (Input == null)
            return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var currentEntity = Input.ActiveEntity;
        if (currentEntity == Entity.Null || !entityManager.HasComponent<ConfigurableClipData>(currentEntity))
            return;

        transform.position = entityManager.GetComponentData<Translation>(currentEntity).Value;
        transform.rotation = entityManager.GetComponentData<Rotation>(currentEntity).Value;
    }
}
