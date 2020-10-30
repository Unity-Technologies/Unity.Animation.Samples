using System.Collections;
using System.Collections.Generic;
using Unity.Animation;
using Unity.Animation.Hybrid;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

#if UNITY_EDITOR
[RequireComponent(typeof(RigComponent))]
[ConverterVersion("PhaseMatchingConvertion", 18)]
[UpdateInGroup(typeof(GameObjectConversionGroup))]
[UpdateAfter(typeof(RigConversion))]
public class PhaseMatchingAuthoringComponent : MonoBehaviour, IConvertGameObjectToEntity
{
    public AnimationClip[]  Clips;
    public float[]          BlendThresholds;
    public string           MotionJointName;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (Clips == null || BlendThresholds == null)
            return;

        if (!dstManager.HasComponent<Rig>(entity))
            throw new System.InvalidOperationException("RigComponent must be converted before this component.");

        if (Clips.Length != BlendThresholds.Length)
            throw new System.InvalidOperationException("You must have the same number of clips and blendthresholds.");

        var rigComponent = GetComponent<RigComponent>() as RigComponent;
        StringHash motionId = "";
        for (var boneIter = 0; boneIter < rigComponent.Bones.Length; boneIter++)
        {
            if (MotionJointName == rigComponent.Bones[boneIter].name)
            {
                motionId = RigGenerator.ComputeRelativePath(rigComponent.Bones[boneIter], rigComponent.transform);
            }
        }

        var rigDefinition = dstManager.GetComponentData<Rig>(entity);
        var clipConfiguration = new ClipConfiguration();
        clipConfiguration.Mask = ClipConfigurationMask.LoopTime | ClipConfigurationMask.LoopValues  | ClipConfigurationMask.CycleRootMotion | ClipConfigurationMask.DeltaRootMotion;
        clipConfiguration.MotionID = motionId;

        var denseClips = new NativeArray<SampleClip>(Clips.Length, Allocator.Temp);
        var durations = new NativeArray<SampleClipDuration>(Clips.Length, Allocator.Temp);
        var thresholds = new NativeArray<SampleClipBlendThreshold>(Clips.Length, Allocator.Temp);

        for (int i = 0; i < Clips.Length; i++)
        {
            conversionSystem.DeclareAssetDependency(gameObject, Clips[i]);

            denseClips[i] = new SampleClip
            {
                Clip = UberClipNode.Bake(rigDefinition.Value, Clips[i].ToDenseClip(), clipConfiguration, 60.0f)
            };
            durations[i] = new SampleClipDuration { Value = denseClips[i].Clip.Value.Duration };
            thresholds[i] = new SampleClipBlendThreshold { Value = BlendThresholds[i] };
        }

        var synchronizeMotions = dstManager.AddBuffer<SampleClip>(entity);
        synchronizeMotions.CopyFrom(denseClips);

        var synchronizeMotionDurations = dstManager.AddBuffer<SampleClipDuration>(entity);
        synchronizeMotionDurations.CopyFrom(durations);

        var synchronizeMotionThresholds = dstManager.AddBuffer<SampleClipBlendThreshold>(entity);
        synchronizeMotionThresholds.CopyFrom(thresholds);

        dstManager.AddComponentData(entity, new WeightComponent
        {
            Value = 0.0f
        });

        denseClips.Dispose();
        durations.Dispose();
        thresholds.Dispose();
    }
}
#endif
