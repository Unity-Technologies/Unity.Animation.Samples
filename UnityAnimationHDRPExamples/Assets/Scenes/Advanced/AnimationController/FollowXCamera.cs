using UnityEngine;
using Unity.Entities;

public class FollowXCamera : MonoBehaviour
{
    public AnimationControllerInput Input;

    void Update()
    {
        if (Input == null)
            return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var currentEntity = Input.ActiveEntity;
        if (currentEntity == Entity.Null
            || !entityManager.HasComponent<AnimationControllerData>(currentEntity))
            return;

        var data = entityManager.GetComponentData<AnimationControllerData>(currentEntity);
        transform.position = data.FollowX.pos;
        transform.rotation = data.FollowX.rot;
    }
}
