using Unity.Animation;
using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Mathematics;

public class DirectionMixerNode
    : SimulationKernelNodeDefinition<DirectionMixerNode.SimPorts, DirectionMixerNode.KernelDefs>
{
    const ushort k_ClipCount = 5;

    public struct SimPorts : ISimulationPortDefinition
    {
        public MessageInput<DirectionMixerNode, ClipConfiguration> ClipConfiguration;
        public MessageInput<DirectionMixerNode, Rig> Rig;

        public MessageInput<DirectionMixerNode, BlobAssetReference<Clip>> Clip0;
        public MessageInput<DirectionMixerNode, BlobAssetReference<Clip>> Clip1;
        public MessageInput<DirectionMixerNode, BlobAssetReference<Clip>> Clip2;
        public MessageInput<DirectionMixerNode, BlobAssetReference<Clip>> Clip3;
        public MessageInput<DirectionMixerNode, BlobAssetReference<Clip>> Clip4;

#pragma warning disable 0649
        internal MessageOutput<DirectionMixerNode, Rig> m_OutRig;
        internal MessageOutput<DirectionMixerNode, ClipConfiguration> m_OutConfiguration;
        internal PortArray<MessageOutput<DirectionMixerNode, BlobAssetReference<Clip>>> m_OutClips;
#pragma warning restore 0649
    }

    public struct KernelDefs : IKernelPortDefinition
    {
        public DataInput<DirectionMixerNode, float> Time;
        public DataInput<DirectionMixerNode, float> DeltaTime;
        public DataInput<DirectionMixerNode, float> Weight;

        public DataOutput<DirectionMixerNode, Buffer<AnimatedData>> Output;
    }

    struct Data
        : INodeData
        , IInit
        , IDestroy
        , IMsgHandler<Rig>
        , IMsgHandler<BlobAssetReference<Clip>>
        , IMsgHandler<ClipConfiguration>
    {
        NodeHandle<KernelPassThroughNodeFloat> m_TimeNode;
        NodeHandle<KernelPassThroughNodeFloat> m_DeltaTimeNode;
        NodeHandle<KernelPassThroughNodeFloat> m_WeightNode;

        NodeHandle<UberClipNode> m_Clip0;
        NodeHandle<UberClipNode> m_Clip1;
        NodeHandle<UberClipNode> m_Clip2;
        NodeHandle<UberClipNode> m_Clip3;
        NodeHandle<UberClipNode> m_Clip4;

        NodeHandle<NMixerNode> m_NMixerNode;
        NodeHandle<DirectionMixerComputeWeightNode> m_ComputeWeightNode;

        public void Init(InitContext ctx)
        {
            var thisHandle = ctx.Set.CastHandle<DirectionMixerNode>(ctx.Handle);

            m_TimeNode = ctx.Set.Create<KernelPassThroughNodeFloat>();
            m_DeltaTimeNode = ctx.Set.Create<KernelPassThroughNodeFloat>();
            m_WeightNode = ctx.Set.Create<KernelPassThroughNodeFloat>();

            m_Clip0 = ctx.Set.Create<UberClipNode>();
            m_Clip1 = ctx.Set.Create<UberClipNode>();
            m_Clip2 = ctx.Set.Create<UberClipNode>();
            m_Clip3 = ctx.Set.Create<UberClipNode>();
            m_Clip4 = ctx.Set.Create<UberClipNode>();

            m_NMixerNode = ctx.Set.Create<NMixerNode>();
            m_ComputeWeightNode = ctx.Set.Create<DirectionMixerComputeWeightNode>();

            ctx.Set.Connect(m_TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_Clip0, UberClipNode.KernelPorts.Time);
            ctx.Set.Connect(m_TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_Clip1, UberClipNode.KernelPorts.Time);
            ctx.Set.Connect(m_TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_Clip2, UberClipNode.KernelPorts.Time);
            ctx.Set.Connect(m_TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_Clip3, UberClipNode.KernelPorts.Time);
            ctx.Set.Connect(m_TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_Clip4, UberClipNode.KernelPorts.Time);

            ctx.Set.Connect(m_DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_Clip0, UberClipNode.KernelPorts.DeltaTime);
            ctx.Set.Connect(m_DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_Clip1, UberClipNode.KernelPorts.DeltaTime);
            ctx.Set.Connect(m_DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_Clip2, UberClipNode.KernelPorts.DeltaTime);
            ctx.Set.Connect(m_DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_Clip3, UberClipNode.KernelPorts.DeltaTime);
            ctx.Set.Connect(m_DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_Clip4, UberClipNode.KernelPorts.DeltaTime);

            ctx.Set.SetPortArraySize(m_NMixerNode, NMixerNode.KernelPorts.Inputs, k_ClipCount);
            ctx.Set.Connect(m_Clip0, UberClipNode.KernelPorts.Output, m_NMixerNode, NMixerNode.KernelPorts.Inputs, 0);
            ctx.Set.Connect(m_Clip1, UberClipNode.KernelPorts.Output, m_NMixerNode, NMixerNode.KernelPorts.Inputs, 1);
            ctx.Set.Connect(m_Clip2, UberClipNode.KernelPorts.Output, m_NMixerNode, NMixerNode.KernelPorts.Inputs, 2);
            ctx.Set.Connect(m_Clip3, UberClipNode.KernelPorts.Output, m_NMixerNode, NMixerNode.KernelPorts.Inputs, 3);
            ctx.Set.Connect(m_Clip4, UberClipNode.KernelPorts.Output, m_NMixerNode, NMixerNode.KernelPorts.Inputs, 4);

            ctx.Set.Connect(m_WeightNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_ComputeWeightNode, DirectionMixerComputeWeightNode.KernelPorts.Input);
            ctx.Set.SetPortArraySize(m_NMixerNode, NMixerNode.KernelPorts.Weights, k_ClipCount);
            ctx.Set.Connect(m_ComputeWeightNode, DirectionMixerComputeWeightNode.KernelPorts.OutWeight0, m_NMixerNode, NMixerNode.KernelPorts.Weights, 0);
            ctx.Set.Connect(m_ComputeWeightNode, DirectionMixerComputeWeightNode.KernelPorts.OutWeight1, m_NMixerNode, NMixerNode.KernelPorts.Weights, 1);
            ctx.Set.Connect(m_ComputeWeightNode, DirectionMixerComputeWeightNode.KernelPorts.OutWeight2, m_NMixerNode, NMixerNode.KernelPorts.Weights, 2);
            ctx.Set.Connect(m_ComputeWeightNode, DirectionMixerComputeWeightNode.KernelPorts.OutWeight3, m_NMixerNode, NMixerNode.KernelPorts.Weights, 3);
            ctx.Set.Connect(m_ComputeWeightNode, DirectionMixerComputeWeightNode.KernelPorts.OutWeight4, m_NMixerNode, NMixerNode.KernelPorts.Weights, 4);

            // Setup internal message connections
            ctx.Set.Connect(thisHandle, SimulationPorts.m_OutRig, m_Clip0, UberClipNode.SimulationPorts.Rig);
            ctx.Set.Connect(thisHandle, SimulationPorts.m_OutRig, m_Clip1, UberClipNode.SimulationPorts.Rig);
            ctx.Set.Connect(thisHandle, SimulationPorts.m_OutRig, m_Clip2, UberClipNode.SimulationPorts.Rig);
            ctx.Set.Connect(thisHandle, SimulationPorts.m_OutRig, m_Clip3, UberClipNode.SimulationPorts.Rig);
            ctx.Set.Connect(thisHandle, SimulationPorts.m_OutRig, m_Clip4, UberClipNode.SimulationPorts.Rig);
            ctx.Set.Connect(thisHandle, SimulationPorts.m_OutRig, m_NMixerNode, NMixerNode.SimulationPorts.Rig);

            ctx.Set.SetPortArraySize(thisHandle, SimulationPorts.m_OutClips, k_ClipCount);
            ctx.Set.Connect(thisHandle, SimulationPorts.m_OutClips, 0, m_Clip0, UberClipNode.SimulationPorts.Clip);
            ctx.Set.Connect(thisHandle, SimulationPorts.m_OutClips, 1, m_Clip1, UberClipNode.SimulationPorts.Clip);
            ctx.Set.Connect(thisHandle, SimulationPorts.m_OutClips, 2, m_Clip2, UberClipNode.SimulationPorts.Clip);
            ctx.Set.Connect(thisHandle, SimulationPorts.m_OutClips, 3, m_Clip3, UberClipNode.SimulationPorts.Clip);
            ctx.Set.Connect(thisHandle, SimulationPorts.m_OutClips, 4, m_Clip4, UberClipNode.SimulationPorts.Clip);

            ctx.Set.Connect(thisHandle, SimulationPorts.m_OutConfiguration, m_Clip0, UberClipNode.SimulationPorts.Configuration);
            ctx.Set.Connect(thisHandle, SimulationPorts.m_OutConfiguration, m_Clip1, UberClipNode.SimulationPorts.Configuration);
            ctx.Set.Connect(thisHandle, SimulationPorts.m_OutConfiguration, m_Clip2, UberClipNode.SimulationPorts.Configuration);
            ctx.Set.Connect(thisHandle, SimulationPorts.m_OutConfiguration, m_Clip3, UberClipNode.SimulationPorts.Configuration);
            ctx.Set.Connect(thisHandle, SimulationPorts.m_OutConfiguration, m_Clip4, UberClipNode.SimulationPorts.Configuration);

            // Setup port forwarding
            ctx.ForwardInput(KernelPorts.Time, m_TimeNode, KernelPassThroughNodeFloat.KernelPorts.Input);
            ctx.ForwardInput(KernelPorts.DeltaTime, m_DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Input);
            ctx.ForwardInput(KernelPorts.Weight, m_WeightNode, KernelPassThroughNodeFloat.KernelPorts.Input);
            ctx.ForwardOutput(KernelPorts.Output, m_NMixerNode, NMixerNode.KernelPorts.Output);
        }

        public void Destroy(DestroyContext ctx)
        {
            ctx.Set.Destroy(m_TimeNode);
            ctx.Set.Destroy(m_DeltaTimeNode);
            ctx.Set.Destroy(m_WeightNode);

            ctx.Set.Destroy(m_Clip0);
            ctx.Set.Destroy(m_Clip1);
            ctx.Set.Destroy(m_Clip2);
            ctx.Set.Destroy(m_Clip3);
            ctx.Set.Destroy(m_Clip4);

            ctx.Set.Destroy(m_NMixerNode);
            ctx.Set.Destroy(m_ComputeWeightNode);
        }

        public void HandleMessage(in MessageContext ctx, in Rig rig) =>
            ctx.EmitMessage(SimulationPorts.m_OutRig, rig);

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<Clip> clip)
        {
            if (ctx.Port == SimulationPorts.Clip0)
            {
                ctx.EmitMessage(SimulationPorts.m_OutClips, 0, clip);
            }
            else if (ctx.Port == SimulationPorts.Clip1)
            {
                ctx.EmitMessage(SimulationPorts.m_OutClips, 1, clip);
            }
            else if (ctx.Port == SimulationPorts.Clip2)
            {
                ctx.EmitMessage(SimulationPorts.m_OutClips, 2, clip);
            }
            else if (ctx.Port == SimulationPorts.Clip3)
            {
                ctx.EmitMessage(SimulationPorts.m_OutClips, 3, clip);
            }
            else if (ctx.Port == SimulationPorts.Clip4)
            {
                ctx.EmitMessage(SimulationPorts.m_OutClips, 4, clip);
            }
        }

        public void HandleMessage(in MessageContext ctx, in ClipConfiguration clipConfiguration) =>
            ctx.EmitMessage(SimulationPorts.m_OutConfiguration, clipConfiguration);
    }

    struct KernelData : IKernelData
    {
    }

    [BurstCompile]
    struct Kernel : IGraphKernel<KernelData, KernelDefs>
    {
        public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
        {
        }
    }
}

public class DirectionMixerComputeWeightNode
    : KernelNodeDefinition<DirectionMixerComputeWeightNode.KernelDefs>
{
    public struct KernelDefs : IKernelPortDefinition
    {
        public DataInput<DirectionMixerComputeWeightNode, float> Input;

        public DataOutput<DirectionMixerComputeWeightNode, float> OutWeight0;
        public DataOutput<DirectionMixerComputeWeightNode, float> OutWeight1;
        public DataOutput<DirectionMixerComputeWeightNode, float> OutWeight2;
        public DataOutput<DirectionMixerComputeWeightNode, float> OutWeight3;
        public DataOutput<DirectionMixerComputeWeightNode, float> OutWeight4;
    }

    struct KernelData : IKernelData {}

    [BurstCompile]
    struct Kernel : IGraphKernel<KernelData, KernelDefs>
    {
        public void Execute(RenderContext ctx, KernelData data, ref KernelDefs ports)
        {
            ctx.Resolve(ref ports.OutWeight0) = 0f;
            ctx.Resolve(ref ports.OutWeight1) = 0f;
            ctx.Resolve(ref ports.OutWeight2) = 0f;
            ctx.Resolve(ref ports.OutWeight3) = 0f;
            ctx.Resolve(ref ports.OutWeight4) = 0f;

            var w1 = math.modf(math.clamp(ctx.Resolve(ports.Input), 0f, 4f), out float index);
            var w0 = 1f - w1;

            if (index < 1f)
            {
                ctx.Resolve(ref ports.OutWeight0) = w0;
                ctx.Resolve(ref ports.OutWeight1) = w1;
            }
            else if (index < 2f)
            {
                ctx.Resolve(ref ports.OutWeight1) = w0;
                ctx.Resolve(ref ports.OutWeight2) = w1;
            }
            else if (index < 3f)
            {
                ctx.Resolve(ref ports.OutWeight2) = w0;
                ctx.Resolve(ref ports.OutWeight3) = w1;
            }
            else if (index < 4f)
            {
                ctx.Resolve(ref ports.OutWeight3) = w0;
                ctx.Resolve(ref ports.OutWeight4) = w1;
            }
            else
            {
                ctx.Resolve(ref ports.OutWeight4) = 1f;
            }
        }
    }
}
