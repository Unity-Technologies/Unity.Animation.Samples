#if UNITY_EDITOR

using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Animation;
using Unity.Animation.Hybrid;
using System.Collections.Generic;

[RequireComponent(typeof(RigComponent))]
public class RetargetComponent : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public GameObject SourceRigPrefab;
    public AnimationClip SourceClip;

    public List<string> RetargetMap;

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs) =>
        referencedPrefabs.Add(SourceRigPrefab);

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var dstRig = GetComponent<RigComponent>();
        var srcRig = SourceRigPrefab.GetComponent<RigComponent>();
        var query = CreateRemapQuery(srcRig, dstRig, RetargetMap);

        var srcRigPrefab = conversionSystem.TryGetPrimaryEntity(SourceRigPrefab);
        var srcRigDefinition = dstManager.GetComponentData<Rig>(srcRigPrefab);
        var dstRigDefinition = dstManager.GetComponentData<Rig>(entity);

        var setup = new RigRemapSetup
        {
            SrcClip = SourceClip.ToDenseClip(),
            SrcRig = srcRigDefinition.Value,
            RemapTable = query.ToRigRemapTable(srcRigDefinition.Value, dstRigDefinition.Value)
        };

        dstManager.AddComponentData(entity, setup);

        dstManager.AddComponent<DeltaTime>(entity);
    }

    RigRemapQuery CreateRemapQuery(RigComponent srcRig, RigComponent dstRig, List<string> retargetMap)
    {
        string status = "";

        List<ChannelMap> translationChannels = new List<ChannelMap>();
        List<ChannelMap> rotationChannels = new List<ChannelMap>();
        List<RigTranslationOffset> translationOffsets = new List<RigTranslationOffset>();
        List<RigRotationOffset> rotationOffsets = new List<RigRotationOffset>();

        for (var mapIter = 0; mapIter < retargetMap.Count; mapIter++)
        {
            bool success = false;

            string[] splitMap = retargetMap[mapIter].Split(new char[] { ' ' }, 3);

            for (var srcBoneIter = 0; srcBoneIter < srcRig.Bones.Length; srcBoneIter++)
            {
                if (splitMap.Length > 0 && splitMap[0] == srcRig.Bones[srcBoneIter].name)
                {
                    for (var dstBoneIter = 0; dstBoneIter < dstRig.Bones.Length; dstBoneIter++)
                    {
                        if (splitMap.Length > 1 && splitMap[1] == dstRig.Bones[dstBoneIter].name)
                        {
                            if (splitMap.Length > 2)
                            {
                                var srcPath = RigGenerator.ComputeRelativePath(srcRig.Bones[srcBoneIter], srcRig.transform);
                                var dstPath = RigGenerator.ComputeRelativePath(dstRig.Bones[dstBoneIter], dstRig.transform);

                                if (splitMap[2] == "TR" || splitMap[2] == "T")
                                {
                                    var translationOffset = new RigTranslationOffset();

                                    // heuristic that computes retarget scale based on translation node (ex: hips) height (assumed to be y)
                                    translationOffset.Scale = dstRig.Bones[dstBoneIter].position.y / srcRig.Bones[srcBoneIter].position.y;
                                    translationOffset.Rotation = mathex.mul(math.conjugate(dstRig.Bones[dstBoneIter].parent.rotation), srcRig.Bones[srcBoneIter].parent.rotation);

                                    translationOffsets.Add(translationOffset);
                                    translationChannels.Add(new ChannelMap() { SourceId = srcPath, DestinationId = dstPath, OffsetIndex = translationOffsets.Count });
                                }

                                if (splitMap[2] == "TR" || splitMap[2] == "R")
                                {
                                    var rotationOffset = new RigRotationOffset();

                                    rotationOffset.PreRotation = mathex.mul(math.conjugate(dstRig.Bones[dstBoneIter].parent.rotation), srcRig.Bones[srcBoneIter].parent.rotation);
                                    rotationOffset.PostRotation = mathex.mul(math.conjugate(srcRig.Bones[srcBoneIter].rotation), dstRig.Bones[dstBoneIter].rotation);

                                    rotationOffsets.Add(rotationOffset);
                                    rotationChannels.Add(new ChannelMap() { SourceId = srcPath, DestinationId = dstPath, OffsetIndex = rotationOffsets.Count });
                                }

                                success = true;
                            }
                        }
                    }
                }
            }

            if (!success)
            {
                status = status + mapIter + " ";
            }
        }

        RigRemapQuery rigRemapQuery = new RigRemapQuery();

        rigRemapQuery.TranslationChannels = new ChannelMap[translationChannels.Count];

        for (var iter = 0; iter < translationChannels.Count; iter++)
        {
            rigRemapQuery.TranslationChannels[iter] = translationChannels[iter];
        }

        rigRemapQuery.TranslationOffsets = new RigTranslationOffset[translationOffsets.Count + 1];
        rigRemapQuery.TranslationOffsets[0] = new RigTranslationOffset();

        for (var iter = 0; iter < translationOffsets.Count; iter++)
        {
            rigRemapQuery.TranslationOffsets[iter + 1] = translationOffsets[iter];
        }

        rigRemapQuery.RotationChannels = new ChannelMap[rotationChannels.Count];

        for (var iter = 0; iter < rotationChannels.Count; iter++)
        {
            rigRemapQuery.RotationChannels[iter] = rotationChannels[iter];
        }

        rigRemapQuery.RotationOffsets = new RigRotationOffset[rotationOffsets.Count + 1];
        rigRemapQuery.RotationOffsets[0] = new RigRotationOffset();

        for (var iter = 0; iter < rotationOffsets.Count; iter++)
        {
            rigRemapQuery.RotationOffsets[iter + 1] = rotationOffsets[iter];
        }

        if (!string.IsNullOrEmpty(status))
        {
            UnityEngine.Debug.LogError("Faulty Entries : " + status);
        }

        return rigRemapQuery;
    }
}
#endif
