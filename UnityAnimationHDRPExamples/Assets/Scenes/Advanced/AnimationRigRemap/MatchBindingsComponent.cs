#if UNITY_EDITOR

using UnityEngine;
using Unity.Entities;
using Unity.Animation;
using Unity.Animation.Hybrid;
using System.Collections.Generic;

[RequireComponent(typeof(RigComponent))]
public class MatchBindingsComponent : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public GameObject SourceRigPrefab;
    public AnimationClip SourceClip;

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs) =>
        referencedPrefabs.Add(SourceRigPrefab);

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var srcRigPrefab     = conversionSystem.TryGetPrimaryEntity(SourceRigPrefab);
        var srcRigDefinition = dstManager.GetComponentData<Rig>(srcRigPrefab);
        var dstRigDefinition = dstManager.GetComponentData<Rig>(entity);

        var setup = new RigRemapSetup
        {
            SrcClip = SourceClip.ToDenseClip(),
            SrcRig = srcRigDefinition.Value,

            // Automatically create a remap table based on matching rig definition bindings.
            // This is useful for LOD setups which define matching skeleton hierarchies.
            // This example uses the TerraFormerLOD1 (srcRigDefinition) which animates 31 bones and the animation data is remapped
            // to TerraFormerLOD0 (dstRigDefinition) containing 131 bones.
            RemapTable = Unity.Animation.RigRemapUtils.CreateRemapTable(srcRigDefinition.Value, dstRigDefinition.Value)
        };

        dstManager.AddComponentData(entity, setup);

        dstManager.AddComponent<DeltaTime>(entity);
    }
}

#endif
