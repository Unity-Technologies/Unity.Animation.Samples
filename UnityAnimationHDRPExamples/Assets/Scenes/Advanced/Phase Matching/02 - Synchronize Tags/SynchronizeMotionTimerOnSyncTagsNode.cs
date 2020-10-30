using Unity.Burst;
using Unity.Mathematics;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.Collections;
using Unity.Animation;

public class SynchronizeMotionTimerOnSyncTagsNode
    : SimulationKernelNodeDefinition<SynchronizeMotionTimerOnSyncTagsNode.SimPorts, SynchronizeMotionTimerOnSyncTagsNode.KernelDefs>
{
    public struct SimPorts : ISimulationPortDefinition
    {
        public MessageInput<SynchronizeMotionTimerOnSyncTagsNode, int>          MotionCount;
        public MessageInput<SynchronizeMotionTimerOnSyncTagsNode, StringHash>   SynchronizationTagType;
    }

    struct Data : INodeData, IMsgHandler<StringHash>, IMsgHandler<int>
    {
        public void HandleMessage(in MessageContext ctx, in StringHash synchronizationTagType)
        {
            ctx.UpdateKernelData(new KernelData
            {
                SynchronizationTagType = synchronizationTagType
            });
        }

        public void HandleMessage(in MessageContext ctx, in int motionCount)
        {
            ctx.Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.SampleClipTimersOutput, Buffer<SampleClipTime>.SizeRequest(motionCount));
            ctx.Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Timers, Buffer<float>.SizeRequest(motionCount));
            ctx.Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Weights, Buffer<float>.SizeRequest(motionCount));
            ctx.Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.DeltaTimesOutput, Buffer<float>.SizeRequest(motionCount));
        }
    }

    public struct KernelDefs : IKernelPortDefinition
    {
        public DataInput<SynchronizeMotionTimerOnSyncTagsNode, Buffer<SampleClipTime>>              SampleClipTimersInput;
        public DataInput<SynchronizeMotionTimerOnSyncTagsNode, Buffer<SampleClip>>                  Motions;
        public DataInput<SynchronizeMotionTimerOnSyncTagsNode, Buffer<SampleClipDuration>>          Durations;
        public DataInput<SynchronizeMotionTimerOnSyncTagsNode, Buffer<SampleClipBlendThreshold>>    WeightThresholds;
        public DataInput<SynchronizeMotionTimerOnSyncTagsNode, float>                               DeltaTime;
        public DataInput<SynchronizeMotionTimerOnSyncTagsNode, WeightComponent>                     WeightComponent;

        public DataOutput<SynchronizeMotionTimerOnSyncTagsNode, Buffer<SampleClipTime>>             SampleClipTimersOutput;
        public DataOutput<SynchronizeMotionTimerOnSyncTagsNode, Buffer<float>>                      Timers;
        public DataOutput<SynchronizeMotionTimerOnSyncTagsNode, Buffer<float>>                      DeltaTimesOutput;
        public DataOutput<SynchronizeMotionTimerOnSyncTagsNode, Buffer<float>>                      Weights;
    }

    struct KernelData : IKernelData
    {
        public StringHash SynchronizationTagType;
    }

    [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
    struct Kernel : IGraphKernel<KernelData, KernelDefs>
    {
        struct SyncRatio
        {
            public StringHash SynchronizationTagType;
            public int StartId;
            public int EndId;
            public float Ratio;
        }

        public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
        {
            var weight = context.Resolve(ports.WeightComponent).Value;
            var deltaTime = context.Resolve(ports.DeltaTime);
            var motions = context.Resolve(ports.Motions);
            var durations = context.Resolve(ports.Durations).Reinterpret<float>();
            var motionTimers = context.Resolve(ports.SampleClipTimersInput).Reinterpret<float>();
            var outMotionTimers = context.Resolve(ref ports.SampleClipTimersOutput).Reinterpret<float>();
            var outDeltaTimes = context.Resolve(ref ports.DeltaTimesOutput).Reinterpret<float>();
            var weightThresholds = context.Resolve(ports.WeightThresholds).Reinterpret<float>();
            var timers = context.Resolve(ref ports.Timers);
            var outputWeights = context.Resolve(ref ports.Weights);

            outMotionTimers.CopyFrom(motionTimers);

            ComputeWeights(weight, weightThresholds, ref outputWeights);

            var masterIndex = GetMasterTimerIndex(outputWeights);

            var effectiveDuration = ComputeEffectiveDuration(durations, outputWeights);

            var deltaTimeRatio = deltaTime * (1.0f / effectiveDuration);

            outMotionTimers[masterIndex] += deltaTimeRatio;
            outDeltaTimes[masterIndex] = deltaTimeRatio;

            var syncRatio = GetSyncRatio(data.SynchronizationTagType, outMotionTimers[masterIndex], ref motions[masterIndex].Clip.Value.SynchronizationTags);

            for (int index = 0; index < outMotionTimers.Length; ++index)
            {
                if (index == masterIndex)
                    continue;

                outMotionTimers[index] = ComputeSyncTime(syncRatio, outMotionTimers[index], deltaTimeRatio, ref motions[index].Clip.Value.SynchronizationTags);
                var currentTime = math.modf(outMotionTimers[index], out float _);
                var prevTime = math.modf(motionTimers[index], out float _);
                if (prevTime > currentTime)
                    outDeltaTimes[index] = 1.0f + currentTime - prevTime;
                else
                    outDeltaTimes[index] = currentTime - prevTime;
            }

            timers.CopyFrom(outMotionTimers);
        }

        float WeightForIndex(NativeArray<float> thresholds, int length, int index, float blend)
        {
            if (blend >= thresholds[index])
            {
                if (index + 1 == length)
                {
                    return 1.0f;
                }
                else if (thresholds[index + 1] < blend)
                {
                    return 0.0f;
                }
                else
                {
                    if (thresholds[index] - thresholds[index + 1] != 0)
                    {
                        return (blend - thresholds[index + 1]) / (thresholds[index] - thresholds[index + 1]);
                    }
                    else
                    {
                        return 1.0f;
                    }
                }
            }
            else
            {
                if (index == 0)
                {
                    return 1.0f;
                }
                else if (thresholds[index - 1] > blend)
                {
                    return 0.0f;
                }
                else
                {
                    if ((thresholds[index] - thresholds[index - 1]) != 0)
                    {
                        return (blend - thresholds[index - 1]) / (thresholds[index] - thresholds[index - 1]);
                    }
                    else
                    {
                        return 1.0f;
                    }
                }
            }
        }

        void ComputeWeights(float blendParameter, NativeArray<float> thresholds, ref NativeArray<float> outWeights)
        {
            var length = thresholds.Length;

            var blend = math.clamp(blendParameter, thresholds[0], thresholds[length - 1]);
            for (int i = 0; i < length; i++)
            {
                outWeights[i] = WeightForIndex(thresholds, length, i, blend);
            }
        }

        static public float ComputeEffectiveDuration(NativeArray<float> durations, NativeArray<float> weights)
        {
            var length = durations.Length;

            var duration = 0.0f;
            for (int i = 0; i < length; i++)
            {
                duration += weights[i] * durations[i];
            }

            return duration;
        }

        int GetMasterTimerIndex(NativeArray<float> weights)
        {
            int masterIndex = 0;
            float greatestWeight = 0.0f;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] > greatestWeight)
                    masterIndex = i;
            }
            return masterIndex;
        }

        SyncRatio GetSyncRatio(StringHash synchronizationTagType, float normalizedTime, ref BlobArray<SynchronizationTag> tags)
        {
            normalizedTime = math.modf(normalizedTime, out float _);
            var indexTagPastRatio = GetIndexTagPastRatio(synchronizationTagType, ref tags, normalizedTime);
            var syncRatio = new SyncRatio();
            if (tags.Length == 0)
                return syncRatio;

            if (indexTagPastRatio == 0 || indexTagPastRatio == tags.Length)
            {
                var ratioFromLastToEndOfAnim = 1.0f - tags[tags.Length - 1].NormalizedTime;
                var ratioRangeBetweenTags = ratioFromLastToEndOfAnim + tags[0].NormalizedTime;
                float normalizedCurrentRatioBetweenTags = 0.0f;
                if (indexTagPastRatio == 0)
                    normalizedCurrentRatioBetweenTags = (normalizedTime + ratioFromLastToEndOfAnim) / ratioRangeBetweenTags;
                else
                    normalizedCurrentRatioBetweenTags = (normalizedTime - tags[tags.Length - 1].NormalizedTime) / ratioRangeBetweenTags;
                syncRatio = new SyncRatio()
                {
                    SynchronizationTagType = synchronizationTagType,
                    StartId = tags[tags.Length - 1].State,
                    EndId = tags[0].State,
                    Ratio = normalizedCurrentRatioBetweenTags
                };
            }
            else
            {
                var ratioRangeBetweenTags = tags[indexTagPastRatio].NormalizedTime - tags[indexTagPastRatio - 1].NormalizedTime;
                float normalizedCurrentRatioBetweenTags = (normalizedTime - tags[indexTagPastRatio - 1].NormalizedTime) / ratioRangeBetweenTags;
                syncRatio = new SyncRatio()
                {
                    SynchronizationTagType = synchronizationTagType,
                    StartId = tags[indexTagPastRatio - 1].State,
                    EndId = tags[indexTagPastRatio].State,
                    Ratio = normalizedCurrentRatioBetweenTags
                };
            }

            return syncRatio;
        }

        int GetIndexTagPastRatio(StringHash synchronizationTagType, ref BlobArray<SynchronizationTag> tags, float normalizedTime)
        {
            int indexTagPastRatio = 0;
            while (indexTagPastRatio < tags.Length && tags[indexTagPastRatio].Type == synchronizationTagType && tags[indexTagPastRatio].NormalizedTime < normalizedTime)
            {
                ++indexTagPastRatio;
            }

            return indexTagPastRatio;
        }

        float ComputeSyncTime(SyncRatio syncRatio, float normalizedTime, float deltaTime, ref BlobArray<SynchronizationTag> tags)
        {
            normalizedTime = math.modf(normalizedTime, out float _);
            int indexTagPastRatio = GetIndexTagPastRatio(syncRatio.SynchronizationTagType, ref tags, normalizedTime);
            int indexToCompareEndTag = indexTagPastRatio;
            if (indexToCompareEndTag == tags.Length)
                indexToCompareEndTag = 0;
            while (tags.Length > 0)
            {
                if (syncRatio.EndId == tags[indexToCompareEndTag].State)
                {
                    int indexPreviousTag = indexToCompareEndTag - 1;
                    if (indexPreviousTag < 0)
                        indexPreviousTag += tags.Length;
                    if (syncRatio.StartId == tags[indexPreviousTag].State)
                    {
                        float rangeBetweenTags = 0.0f;
                        float ratioStartTag = tags[indexPreviousTag].NormalizedTime;
                        if (indexPreviousTag < indexToCompareEndTag) // normal case, we're not between last tag and first tag in a looping clip
                        {
                            rangeBetweenTags = tags[indexToCompareEndTag].NormalizedTime - ratioStartTag;
                        }
                        else
                        {
                            rangeBetweenTags = 1.0f - ratioStartTag + tags[indexToCompareEndTag].NormalizedTime;
                        }

                        var resultingRatio = (syncRatio.Ratio * rangeBetweenTags) + ratioStartTag;

                        // leaving looping at the parent level. What does it means to sync a non looping animation beyond the tag pair anyway
                        //                        if (resultingRatio > 1.0f) // target ratio makes us loop
                        //                            resultingRatio -= 1.0f;
                        return resultingRatio;
                    }
                }

                ++indexToCompareEndTag;
                if (indexToCompareEndTag == tags.Length) // need to loop
                    indexToCompareEndTag = 0;
                if (indexToCompareEndTag == indexTagPastRatio) // if we got back to the original tag index, it means we never found the tag pair in this animation
                    break;
            }
            return normalizedTime + deltaTime;
        }
    }
}
