using Unity.Burst;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.Collections;

namespace Unity.Animation
{
    public class ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode
        : SimulationKernelNodeDefinition<ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode.SimPorts, ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode.KernelDefs>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode, int>          MotionCount;
        }

        struct Data : INodeData, IMsgHandler<int>
        {
            public void HandleMessage(in MessageContext ctx, in int motionCount) =>
                ctx.Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Weights, Buffer<float>.SizeRequest(motionCount));
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode, NormalizedTimeComponent>            NormalizedTimeComponentInput;
            public DataInput<ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode, WeightComponent>                    WeightComponent;
            public DataInput<ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode, Buffer<SampleClipBlendThreshold>>   WeightThresholds;
            public DataInput<ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode, Buffer<SampleClipDuration>>         Durations;
            public DataInput<ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode, float>                              DeltaTime;

            public DataOutput<ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode, NormalizedTimeComponent>           NormalizedTimeComponentOutput;
            public DataOutput<ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode, float>                             NormalizedTime;
            public DataOutput<ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode, Buffer<float>>                     Weights;
            public DataOutput<ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode, float>                             DeltaTimeOutput;
        }

        struct KernelData : IKernelData
        {
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                var normalizedTime = context.Resolve(ports.NormalizedTimeComponentInput);
                var weight = context.Resolve(ports.WeightComponent).Value;
                var deltaTime = context.Resolve(ports.DeltaTime);
                var durations = context.Resolve(ports.Durations).Reinterpret<float>();
                var weightThresholds = context.Resolve(ports.WeightThresholds).Reinterpret<float>();
                var outputWeights = context.Resolve(ref ports.Weights);

                ComputeWeights(weight, weightThresholds, ref outputWeights);

                var effectiveDuration = ComputeEffectiveDuration(durations, outputWeights);
                var speed = 1.0f / effectiveDuration;
                var deltaTimeRatio = deltaTime * speed;

                normalizedTime.Value = math.modf(normalizedTime.Value + deltaTimeRatio, out float _);

                context.Resolve(ref ports.NormalizedTimeComponentOutput) = normalizedTime;
                context.Resolve(ref ports.NormalizedTime) = normalizedTime.Value;
                context.Resolve(ref ports.DeltaTimeOutput) = deltaTimeRatio;
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

            float ComputeEffectiveDuration(NativeArray<float> durations, NativeArray<float> weights)
            {
                var length = durations.Length;

                var duration = 0.0f;
                for (int i = 0; i < length; i++)
                {
                    duration += weights[i] * durations[i];
                }

                return duration;
            }
        }
    }
}
