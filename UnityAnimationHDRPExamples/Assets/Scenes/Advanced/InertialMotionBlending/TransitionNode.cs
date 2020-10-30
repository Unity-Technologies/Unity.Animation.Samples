using System;
using Unity.Animation;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Mathematics;
using AnimationCurve = Unity.Animation.AnimationCurve;

public class TransitionByBoolNode
    : SimulationKernelNodeDefinition<TransitionByBoolNode.SimPorts, TransitionByBoolNode.KernelDefs>
    , IRigContextHandler<TransitionByBoolNode.Data>
{
    public enum TransitionType
    {
        Crossfade,
        Inertial
    }

    public struct SimPorts : ISimulationPortDefinition
    {
        public MessageInput<TransitionByBoolNode, Rig> Rig;

        public MessageInput<TransitionByBoolNode, bool> ClipSource;
        public MessageInput<TransitionByBoolNode, float> Duration;
        public MessageInput<TransitionByBoolNode, TransitionType> TransitionType;
        public MessageInput<TransitionByBoolNode, BlobAssetReference<AnimationCurveBlob>> BlendCurve;

#pragma warning disable 0649
        internal MessageOutput<TransitionByBoolNode, Rig> m_OutRig;
        internal MessageOutput<TransitionByBoolNode, int> m_OutStreamSize;
        internal MessageOutput<TransitionByBoolNode, bool> m_OutClipSource;
        internal MessageOutput<TransitionByBoolNode, float> m_OutDuration;
        internal MessageOutput<TransitionByBoolNode, AnimationCurve> m_OutBlendCurve;
#pragma warning restore 0649
    }

    public struct KernelDefs : IKernelPortDefinition
    {
        public DataInput<TransitionByBoolNode, float> DeltaTime;
        public DataInput<TransitionByBoolNode, float> Time;
        public DataInput<TransitionByBoolNode, Buffer<AnimatedData>> FalseInput;
        public DataInput<TransitionByBoolNode, Buffer<AnimatedData>> TrueInput;
        public DataOutput<TransitionByBoolNode, Buffer<AnimatedData>> Output;
    }

    struct Data
        : INodeData
        , IInit
        , IDestroy
        , IMsgHandler<Rig>
        , IMsgHandler<float>
        , IMsgHandler<bool>
        , IMsgHandler<TransitionType>
        , IMsgHandler<BlobAssetReference<AnimationCurveBlob>>
    {
        NodeHandle<KernelPassThroughNodeFloat>         m_DeltaTimeNode;
        NodeHandle<KernelPassThroughNodeFloat>         m_TimeNode;
        NodeHandle<KernelPassThroughNodeBufferFloat>   m_FalseInputNode;
        NodeHandle<KernelPassThroughNodeBufferFloat>   m_TrueInputNode;
        NodeHandle<KernelPassThroughNodeBufferFloat>   m_OutputNode;

        NodeHandle<WeightAccumulatorNode> m_WeightAccumulatorNode;
        NodeHandle<EvaluateCurveNode> m_AnimationCurveEvaluationNode;
        NodeHandle<MixerNode> m_MixerNode;

        NodeHandle<InertialBlendingNode> m_InertialBlendingNode;

        Rig m_Rig;
        bool m_ClipSelector;
        float m_Duration;
        TransitionType m_TransitionType;
        AnimationCurve m_BlendCurve;

        public void Init(InitContext ctx)
        {
            m_TransitionType = TransitionType.Crossfade;

            m_DeltaTimeNode = ctx.Set.Create<KernelPassThroughNodeFloat>();
            m_TimeNode = ctx.Set.Create<KernelPassThroughNodeFloat>();
            m_FalseInputNode = ctx.Set.Create<KernelPassThroughNodeBufferFloat>();
            m_TrueInputNode = ctx.Set.Create<KernelPassThroughNodeBufferFloat>();
            m_OutputNode = ctx.Set.Create<KernelPassThroughNodeBufferFloat>();

            var thisHandle = ctx.Set.CastHandle<TransitionByBoolNode>(ctx.Handle);
            ctx.Set.Connect(thisHandle, SimulationPorts.m_OutStreamSize, m_OutputNode, KernelPassThroughNodeBufferFloat.SimulationPorts.BufferSize);
            ctx.Set.Connect(thisHandle, SimulationPorts.m_OutStreamSize, m_FalseInputNode, KernelPassThroughNodeBufferFloat.SimulationPorts.BufferSize);
            ctx.Set.Connect(thisHandle, SimulationPorts.m_OutStreamSize, m_TrueInputNode, KernelPassThroughNodeBufferFloat.SimulationPorts.BufferSize);

            BuildNodes(thisHandle, ctx.Set);

            ctx.ForwardInput(KernelPorts.DeltaTime, m_DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Input);
            ctx.ForwardInput(KernelPorts.Time, m_TimeNode, KernelPassThroughNodeFloat.KernelPorts.Input);
            ctx.ForwardInput(KernelPorts.FalseInput, m_FalseInputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Input);
            ctx.ForwardInput(KernelPorts.TrueInput, m_TrueInputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Input);
            ctx.ForwardOutput(KernelPorts.Output, m_OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Output);
        }

        public void Destroy(DestroyContext ctx)
        {
            ClearNodes(ctx.Set);

            ctx.Set.Destroy(m_DeltaTimeNode);
            ctx.Set.Destroy(m_TimeNode);
            ctx.Set.Destroy(m_FalseInputNode);
            ctx.Set.Destroy(m_TrueInputNode);
            ctx.Set.Destroy(m_OutputNode);
        }

        void BuildNodes(NodeHandle<TransitionByBoolNode> thisHandle, NodeSetAPI set)
        {
            if (m_TransitionType == TransitionType.Crossfade)
            {
                m_MixerNode = set.Create<MixerNode>();
                m_WeightAccumulatorNode = set.Create<WeightAccumulatorNode>();

                set.Connect(m_DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_WeightAccumulatorNode, WeightAccumulatorNode.KernelPorts.DeltaTime);
                if (m_BlendCurve.IsCreated)
                {
                    m_AnimationCurveEvaluationNode = set.Create<EvaluateCurveNode>();

                    set.Connect(m_WeightAccumulatorNode, WeightAccumulatorNode.KernelPorts.BlendWeight, m_AnimationCurveEvaluationNode, EvaluateCurveNode.KernelPorts.Time);
                    set.Connect(m_AnimationCurveEvaluationNode, EvaluateCurveNode.KernelPorts.Output, m_MixerNode, MixerNode.KernelPorts.Weight);
                    set.Connect(thisHandle, SimulationPorts.m_OutBlendCurve, m_AnimationCurveEvaluationNode, EvaluateCurveNode.SimulationPorts.AnimationCurve);
                }
                else
                {
                    set.Connect(m_WeightAccumulatorNode, WeightAccumulatorNode.KernelPorts.BlendWeight, m_MixerNode, MixerNode.KernelPorts.Weight);
                }
                set.Connect(m_FalseInputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Output, m_MixerNode, MixerNode.KernelPorts.Input0);
                set.Connect(m_TrueInputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Output, m_MixerNode, MixerNode.KernelPorts.Input1);
                set.Connect(m_MixerNode, MixerNode.KernelPorts.Output, m_OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Input);

                set.Connect(thisHandle, SimulationPorts.m_OutRig, m_MixerNode, MixerNode.SimulationPorts.Rig);
                set.Connect(thisHandle, SimulationPorts.m_OutDuration, m_WeightAccumulatorNode, WeightAccumulatorNode.SimulationPorts.Duration);
                set.Connect(thisHandle, SimulationPorts.m_OutClipSource, m_WeightAccumulatorNode, WeightAccumulatorNode.SimulationPorts.ClipSource);
            }
            else if (m_TransitionType == TransitionType.Inertial)
            {
                m_InertialBlendingNode = set.Create<InertialBlendingNode>();

                set.Connect(m_DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_InertialBlendingNode, InertialBlendingNode.KernelPorts.DeltaTime);
                set.Connect(m_TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_InertialBlendingNode, InertialBlendingNode.KernelPorts.Time);
                set.Connect(m_FalseInputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Output, m_InertialBlendingNode, InertialBlendingNode.KernelPorts.Input0);
                set.Connect(m_TrueInputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Output, m_InertialBlendingNode, InertialBlendingNode.KernelPorts.Input1);
                set.Connect(m_InertialBlendingNode, InertialBlendingNode.KernelPorts.Output, m_OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Input);

                set.Connect(thisHandle, SimulationPorts.m_OutRig, m_InertialBlendingNode, InertialBlendingNode.SimulationPorts.Rig);
                set.Connect(thisHandle, SimulationPorts.m_OutDuration, m_InertialBlendingNode, InertialBlendingNode.SimulationPorts.Duration);
                set.Connect(thisHandle, SimulationPorts.m_OutClipSource, m_InertialBlendingNode, InertialBlendingNode.SimulationPorts.ClipSource);
            }
            else
            {
                throw new NotSupportedException($"The TransitionByBoolNode class does not support the '{m_TransitionType}' transition type.");
            }
        }

        void ClearNodes(NodeSetAPI set)
        {
            if (set.Exists(m_WeightAccumulatorNode))
                set.Destroy(m_WeightAccumulatorNode);

            if (set.Exists(m_MixerNode))
                set.Destroy(m_MixerNode);

            if (set.Exists(m_AnimationCurveEvaluationNode))
                set.Destroy(m_AnimationCurveEvaluationNode);

            if (set.Exists(m_InertialBlendingNode))
                set.Destroy(m_InertialBlendingNode);
        }

        public void EmitAllOutMessages(in MessageContext ctx)
        {
            ctx.EmitMessage(SimulationPorts.m_OutRig, m_Rig);
            ctx.EmitMessage(SimulationPorts.m_OutStreamSize, m_Rig.Value.Value.Bindings.StreamSize);
            ctx.EmitMessage(SimulationPorts.m_OutClipSource, m_ClipSelector);
            ctx.EmitMessage(SimulationPorts.m_OutDuration, m_Duration);
            ctx.EmitMessage(SimulationPorts.m_OutBlendCurve, m_BlendCurve);
        }

        public void HandleMessage(in MessageContext ctx, in Rig msg)
        {
            m_Rig = msg;
            ctx.EmitMessage(SimulationPorts.m_OutRig, m_Rig);
            ctx.EmitMessage(SimulationPorts.m_OutStreamSize, m_Rig.Value.Value.Bindings.StreamSize);
        }

        public void HandleMessage(in MessageContext ctx, in float msg)
        {
            m_Duration = msg;
            ctx.EmitMessage(SimulationPorts.m_OutDuration, m_Duration);
        }

        public void HandleMessage(in MessageContext ctx, in bool msg)
        {
            m_ClipSelector = msg;
            ctx.EmitMessage(SimulationPorts.m_OutClipSource, m_ClipSelector);
        }

        public void HandleMessage(in MessageContext ctx, in TransitionType msg)
        {
            if (m_TransitionType == msg)
                return;

            m_TransitionType = msg;
            ClearNodes(ctx.Set);
            BuildNodes(ctx.Set.CastHandle<TransitionByBoolNode>(ctx.Handle), ctx.Set);
            EmitAllOutMessages(ctx);
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<AnimationCurveBlob> msg)
        {
            if (m_TransitionType == TransitionType.Crossfade)
            {
                if (m_BlendCurve.IsCreated != msg.IsCreated)
                {
                    m_BlendCurve.SetAnimationCurveBlobAssetRef(msg);
                    ClearNodes(ctx.Set);
                    BuildNodes(ctx.Set.CastHandle<TransitionByBoolNode>(ctx.Handle), ctx.Set);
                    EmitAllOutMessages(ctx);
                }
                else
                {
                    m_BlendCurve.SetAnimationCurveBlobAssetRef(msg);
                    ctx.EmitMessage(SimulationPorts.m_OutBlendCurve, m_BlendCurve);
                }
            }
            else
            {
                // BlendCurve is not considered with InertialBlending, no need to emit a message just update local cache
                m_BlendCurve.SetAnimationCurveBlobAssetRef(msg);
            }
        }
    }

    struct KernelData : IKernelData {}

    struct Kernel : IGraphKernel<KernelData, KernelDefs>
    {
        public void Execute(RenderContext ctx, KernelData data, ref KernelDefs ports) {}
    }

    InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
        (InputPortID)SimulationPorts.Rig;
}

/// <summary>
/// Returns a weight to ensure a smooth transition with a duration
/// We need a specific node to make the weight move like the inertial motion blending node,
/// Essentially the weight moves towards 1 (with a speed depending on duration) and when ChangeClip
/// is set, the weight starts moving towards 0. If ChangeClip is set again the weight starts moving
/// towards 1 again.
/// </summary>
public class WeightAccumulatorNode
    : SimulationKernelNodeDefinition<WeightAccumulatorNode.SimPorts, WeightAccumulatorNode.KernelDefs>
{
    public struct SimPorts : ISimulationPortDefinition
    {
        public MessageInput<WeightAccumulatorNode, float> Duration;
        public MessageInput<WeightAccumulatorNode, bool> ClipSource;
    }

    public struct KernelDefs : IKernelPortDefinition
    {
        public DataInput<WeightAccumulatorNode, float> DeltaTime;
        public DataOutput<WeightAccumulatorNode, float> BlendWeight;
    }

    struct Data
        : INodeData
        , IInit
        , IMsgHandler<float>
        , IMsgHandler<bool>
    {
        float m_Duration;
        bool m_Target;

        public void Init(InitContext ctx)
        {
            m_Duration = 1;
            ctx.UpdateKernelData(RecalculateSpeed());
        }

        public void HandleMessage(in MessageContext ctx, in float msg)
        {
            m_Duration = msg;
            ctx.UpdateKernelData(RecalculateSpeed());
        }

        public void HandleMessage(in MessageContext ctx, in bool msg)
        {
            m_Target = msg;
            ctx.UpdateKernelData(RecalculateSpeed());
        }

        KernelData RecalculateSpeed() =>
            new KernelData { Speed = math.select(-1, 1, m_Target) / m_Duration };
    }

    struct KernelData : IKernelData
    {
        public float Speed;
    }

    struct Kernel : IGraphKernel<KernelData, KernelDefs>
    {
        public void Execute(RenderContext ctx, KernelData data, ref KernelDefs ports)
        {
            var deltaWeight = data.Speed * ctx.Resolve(ports.DeltaTime);
            ref var blendWeight = ref ctx.Resolve(ref ports.BlendWeight);
            blendWeight = math.clamp(blendWeight + deltaWeight, 0, 1);
        }
    }
}
