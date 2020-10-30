#if UNITY_EDITOR
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Animation;
using Unity.Animation.Hybrid;
using Unity.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(RigComponent))]
public class RemapSubRigPartComponent : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public GameObject SourceRigPrefab;
    public AnimationClip SourceClip;

    public Transform RemapLocalToRoot;

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs) =>
        referencedPrefabs.Add(SourceRigPrefab);

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var srcRigComponent = SourceRigPrefab.GetComponent<RigComponent>();
        var dstRigComponent = GetComponent<RigComponent>();
        var srcRigPrefab = conversionSystem.TryGetPrimaryEntity(SourceRigPrefab);
        var srcRigDefinition = dstManager.GetComponentData<Rig>(srcRigPrefab);

        StringHash binding = RemapLocalToRoot.transform.name;
        var overrides = new Unity.Animation.RigRemapUtils.OffsetOverrides(1, Allocator.Temp);
        overrides.AddTranslationOffsetOverride(binding, new RigTranslationOffset { Rotation = quaternion.identity, Scale = 1f, Space = RigRemapSpace.LocalToRoot });
        overrides.AddRotationOffsetOverride(binding, new RigRotationOffset { PreRotation = quaternion.identity, PostRotation = quaternion.identity, Space = RigRemapSpace.LocalToRoot });

        var setup = new RigRemapSetup
        {
            SrcClip = SourceClip.ToDenseClip(),
            SrcRig = srcRigDefinition.Value,

            // Automatically create a remap table based on matching rig component properties. This example uses two hierarchies that are
            // different but have matching transform names. For this specific case, we use the BindingHashUtils.HashName delegate instead
            // of the default HashFullPath strategy in order to find matching entries. Given we are remapping the srcRig (TerraFormerLOD1) to
            // a sub rig part (HandRigLOD0) we override the remapping offsets of the RightHand transform to perform operations in
            // LocalToRoot space (lines 29-32) in order to displace the hand at the wanted position/rotation.
            RemapTable = Unity.Animation.Hybrid.RigRemapUtils.CreateRemapTable(
                srcRigComponent, dstRigComponent, Unity.Animation.RigRemapUtils.ChannelFilter.All, overrides, BindingHashUtils.HashName
            )
        };

        dstManager.AddComponentData(entity, setup);

        dstManager.AddComponent<DeltaTime>(entity);
    }
}
#endif
