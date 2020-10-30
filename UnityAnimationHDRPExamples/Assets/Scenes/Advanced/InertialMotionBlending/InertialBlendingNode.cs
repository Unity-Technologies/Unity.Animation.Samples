using System;
using System.Diagnostics;
using Unity.Animation;
using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

/// <summary>
/// This node will blend two clips using the inertial motion blending algorithm.
/// The node will by default use <c>Input0</c> as the pose, and <c>Input1</c> is ignored.
/// Once the <c>ChangeClip</c> value is set, the node will start blending from
/// <c>Input0</c> to <c>Input1</c>. The moment the blend is started, <c>Input0</c> is completely
/// ignored, and only <c>Input1</c> is used. Setting <c>ChangeClip</c> again will switch from <c>Input1</c> to <c>Input0</c> in the
/// same way.
/// </summary>
public class InertialBlendingNode
    : SimulationKernelNodeDefinition<InertialBlendingNode.SimPorts, InertialBlendingNode.KernelDefs>
    , IRigContextHandler<InertialBlendingNode.Data>
{
    public struct SimPorts : ISimulationPortDefinition
    {
        public MessageInput<InertialBlendingNode, Rig> Rig;

        public MessageInput<InertialBlendingNode, bool> ClipSource;
        public MessageInput<InertialBlendingNode, float> Duration;
    }

    public struct KernelDefs : IKernelPortDefinition
    {
        public DataInput<InertialBlendingNode, float> DeltaTime;
        public DataInput<InertialBlendingNode, float> Time;

        public DataInput<InertialBlendingNode, Buffer<AnimatedData>> Input0;
        public DataInput<InertialBlendingNode, Buffer<AnimatedData>> Input1;

        public DataOutput<InertialBlendingNode, Buffer<AnimatedData>> Output;
    }

    struct Data
        : INodeData
        , IInit
        , IMsgHandler<Rig>
        , IMsgHandler<bool>
        , IMsgHandler<float>
    {
        KernelData m_KernelData;

        public void Init(InitContext ctx)
        {
            m_KernelData.ClipSource = 0;
            m_KernelData.Duration = 1f;
            ctx.UpdateKernelData(m_KernelData);
        }

        public void HandleMessage(in MessageContext ctx, in Rig rig)
        {
            m_KernelData.Rig = rig;

            var bufferLength = rig.Value.IsCreated ? rig.Value.Value.Bindings.StreamSize : 0;
            var positionLength = rig.Value.IsCreated ? rig.Value.Value.Bindings.TranslationBindings.Length : 0;
            var rotationLength = rig.Value.IsCreated ? rig.Value.Value.Bindings.RotationBindings.Length : 0;
            var scaleLength = rig.Value.IsCreated ? rig.Value.Value.Bindings.ScaleBindings.Length : 0;
            var floatLength = rig.Value.IsCreated ? rig.Value.Value.Bindings.FloatBindings.Length : 0;
            var interpolationFactorsLength = positionLength + rotationLength + scaleLength + floatLength;
            var interpolationDirectionsLength = positionLength + rotationLength + scaleLength;
            var handle = ctx.Set.CastHandle<InertialBlendingNode>(ctx.Handle);
            ctx.Set.SetBufferSize(handle, KernelPorts.Output, Buffer<AnimatedData>.SizeRequest(bufferLength));

            var buffer = new NativeArray<AnimatedData>(bufferLength, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var stream = AnimationStream.Create(rig, buffer);
            stream.ResetToDefaultValues();

            ctx.UpdateKernelBuffers(new Kernel
            {
                LastPoseOutput = ctx.UploadRequest(buffer),
                SecondLastPoseOutput = ctx.UploadRequest(buffer),
                InterpolationFactors = Buffer<InertialBlendingCoefficients>.SizeRequest(interpolationFactorsLength),
                InterpolationDirections = Buffer<float3>.SizeRequest(interpolationDirectionsLength)
            });


            ctx.UpdateKernelData(m_KernelData);
        }

        public void HandleMessage(in MessageContext ctx, in bool msg)
        {
            var newClipSource = math.select(0, 1, msg);
            if (m_KernelData.ClipSource != newClipSource)
                m_KernelData.BlendRequestedFlag = !m_KernelData.BlendRequestedFlag;
            m_KernelData.ClipSource = newClipSource;

            ctx.UpdateKernelData(m_KernelData);
        }

        public void HandleMessage(in MessageContext ctx, in float msg)
        {
            m_KernelData.Duration = msg;
            ctx.UpdateKernelData(m_KernelData);
        }
    }

    struct KernelData : IKernelData
    {
        public int ClipSource;
        public BlobAssetReference<RigDefinition> Rig;
        public float Duration;
        // This is a weird parameter. Essentially, flip the value to request a blend
        public bool BlendRequestedFlag;
    }

    [BurstCompile]
    struct Kernel : IGraphKernel<KernelData, KernelDefs>
    {
        bool m_OldBlendRequestedFlag;
        float m_RemainingTime;

        internal Buffer<AnimatedData> LastPoseOutput;
        internal Buffer<AnimatedData> SecondLastPoseOutput;
        internal Buffer<InertialBlendingCoefficients> InterpolationFactors;
        internal Buffer<float3> InterpolationDirections;

        // Note: you can't call this twice :/
        bool ComputeIsBlendRequested(KernelData data)
        {
            var blendRequested = data.BlendRequestedFlag != m_OldBlendRequestedFlag;
            m_OldBlendRequestedFlag = data.BlendRequestedFlag;
            return blendRequested;
        }

        public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
        {
            var blendRequested = ComputeIsBlendRequested(data);

            if (!data.Rig.IsCreated)
                return;
            var inputPort = data.ClipSource == 0 ? ports.Input0 : ports.Input1;
            var inputPose = AnimationStream.CreateReadOnly(data.Rig, context.Resolve(inputPort));

            var interpolationFactors = InterpolationFactors.ToNative(context);
            var interpolationDirections = InterpolationDirections.ToNative(context);

            var lastPose = AnimationStream.Create(data.Rig, LastPoseOutput.ToNative(context));
            var secondLastPose = AnimationStream.Create(data.Rig, SecondLastPoseOutput.ToNative(context));

            var outputPose = AnimationStream.Create(data.Rig, context.Resolve(ref ports.Output));
            ValidateIsNotNull(ref outputPose);

            if (blendRequested)
            {
                var deltaTime = context.Resolve(ports.DeltaTime);
                Core.ComputeInertialBlendingCoefficients(
                    ref inputPose,
                    ref lastPose,
                    ref secondLastPose,
                    deltaTime,
                    data.Duration,
                    interpolationFactors,
                    interpolationDirections);
                m_RemainingTime = data.Duration;
            }

            if (m_RemainingTime > 0)
            {
                var deltaTime = context.Resolve(ports.DeltaTime);
                Core.InertialBlend(ref inputPose, ref outputPose, interpolationFactors, interpolationDirections, data.Duration, m_RemainingTime);
                m_RemainingTime -= deltaTime;
            }
            else
            {
                outputPose.CopyFrom(ref inputPose);
            }

            // Cycle the poses
            secondLastPose.CopyFrom(ref lastPose);
            lastPose.CopyFrom(ref outputPose);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateIsNotNull(ref AnimationStream stream)
        {
            if (stream.IsNull)
                throw new ArgumentNullException("AnimationStream is null.");
        }
    }

    InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
        (InputPortID)SimulationPorts.Rig;
}
