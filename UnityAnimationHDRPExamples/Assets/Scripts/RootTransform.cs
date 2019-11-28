using UnityEngine;
using Unity.Entities;

public class RootTransform : MonoBehaviour
{
    public ConfigurableClipInput Input;

    void Update()
    {
        if (Input == null)
            return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var currentEntity = Input.ActiveEntity;
        if (currentEntity == Entity.Null
            || !entityManager.HasComponent<ConfigurableClipData>(currentEntity))
            return;

        var data = entityManager.GetComponentData<ConfigurableClipData>(currentEntity);
        transform.position = data.RootX.pos;
        transform.rotation = data.RootX.rot;
    }
}
