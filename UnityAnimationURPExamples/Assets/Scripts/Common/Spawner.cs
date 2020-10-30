using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using Unity.Animation.Hybrid;

public class Spawner : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject         RigPrefab;
    public AnimationGraphBase GraphPrefab;

    public int CountX = 100;
    public int CountY = 100;

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(RigPrefab);

        if (GraphPrefab != null)
        {
            GraphPrefab.DeclareReferencedPrefabs(referencedPrefabs);
        }
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var rigPrefab = conversionSystem.TryGetPrimaryEntity(RigPrefab);

        if (rigPrefab == Entity.Null)
            throw new Exception($"Something went wrong while creating an Entity for the rig prefab: {RigPrefab.name}");

        if (GraphPrefab != null)
        {
            var rigComponent = RigPrefab.GetComponent<RigComponent>();
            GraphPrefab.PreProcessData(rigComponent);
            GraphPrefab.AddGraphSetupComponent(rigPrefab, dstManager, conversionSystem);
        }

        dstManager.AddComponentData(entity, new RigSpawner
        {
            RigPrefab = rigPrefab,
            CountX = CountX,
            CountY = CountY,
        });
    }
}
#endif

public struct RigSpawner : IComponentData
{
    public Entity RigPrefab;
    public int CountX;
    public int CountY;
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class RigSpawnerSystem : SystemBase
{
    AnimationInputBase m_Input;

    public void RegisterInput(AnimationInputBase input)
    {
        m_Input = input;
    }

    protected override void OnUpdate()
    {
        CompleteDependency();

        Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((Entity e, ref RigSpawner spawner) =>
            {
                for (var x = 0; x < spawner.CountX; x++)
                {
                    for (var y = 0; y < spawner.CountY; ++y)
                    {
                        var rigInstance = EntityManager.Instantiate(spawner.RigPrefab);
                        var translation = new float3(x * 1.3F, 0, y * 1.3F);
                        EntityManager.SetComponentData(rigInstance, new Translation { Value = translation });

                        if (m_Input != null)
                            m_Input.RegisterEntity(rigInstance);
                    }
                }

                EntityManager.DestroyEntity(e);
            }).Run();
    }
}
