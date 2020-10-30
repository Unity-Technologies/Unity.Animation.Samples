using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;

public class Socket_AttachToComponent : MonoBehaviour, IConvertGameObjectToEntity
{
    [Tooltip("If true, the LocalToParent is computed so that the current LocalToWorld is preserved. Else, LocalToParent is set to identity.")]
    [SerializeField] bool MaintainOffset = false;

    [SerializeField] Transform TransformToAttachTo = null;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var attachToTransformEntity = conversionSystem.TryGetPrimaryEntity(TransformToAttachTo);

        if (MaintainOffset)
        {
            // We don't take a shortcut by computing
            // l2pMatrix = TransformToAttachTo.worldToLocalMatrix * transform.localToWorldMatrix
            // because the Transform system computes the LocalToParent from the TRS components,
            // but not the other way around.
            // Setting only the LocalToParent matrix will result in it being overriden by the
            // TRS after the conversion.

            var t = TransformToAttachTo.InverseTransformPoint(transform.position);
            var r = TransformToAttachTo.worldToLocalMatrix.rotation * transform.rotation;
            var s = Vector3.Scale(TransformToAttachTo.worldToLocalMatrix.lossyScale, transform.lossyScale);

            AttachTo(entity, attachToTransformEntity, dstManager, t, r, s);
        }
        else
        {
            AttachTo(entity, attachToTransformEntity, dstManager,
                transform.position, transform.rotation, transform.lossyScale);
        }
    }

    void AttachTo(Entity attachment, Entity socket, EntityManager entityManager,
        float3 translation, quaternion rotation, float3 scale)
    {
        entityManager.SetComponentData(attachment, new Translation { Value = translation});
        entityManager.SetComponentData(attachment, new Rotation {Value = rotation});

        if (scale.x != 1.0f || scale.y != 1.0f || scale.z != 1.0f)
        {
            // TransformConversion system does not add a Scale component,
            // so there's nothing, or a NonUniformScale component.
            if (entityManager.HasComponent<NonUniformScale>(attachment))
            {
                entityManager.SetComponentData(attachment, new NonUniformScale { Value = scale });
            }
            else
            {
                entityManager.AddComponentData(attachment, new NonUniformScale { Value = scale });
            }
        }

        // No need to update LocalToParent, it's taken care of by the transform system.
        // No need to add the entity as a Child of the parent entity either, the transform
        // system will detect it and add it.
        if (entityManager.HasComponent<Parent>(attachment))
        {
            entityManager.SetComponentData(attachment, new Parent {Value = socket});
        }
        else
        {
            entityManager.AddComponentData(attachment, new Parent {Value = socket});
            entityManager.AddComponentData(attachment, new LocalToParent());
        }
    }
}
