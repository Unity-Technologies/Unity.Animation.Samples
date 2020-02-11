using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

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

        var data = entityManager.GetComponentData<ConfigurableClipData>(currentEntity);
        var rigidT = new RigidTransform(data.RootX);
        transform.position = rigidT.pos;
        transform.rotation = rigidT.rot;
    }
}
