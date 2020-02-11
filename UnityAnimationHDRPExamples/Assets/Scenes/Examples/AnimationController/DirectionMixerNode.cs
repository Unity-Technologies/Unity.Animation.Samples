using Unity.Animation;
using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Mathematics;

public class DirectionMixerNode
    : NodeDefinition<DirectionMixerNode.Data, DirectionMixerNode.SimPorts, DirectionMixerNode.KernelData, DirectionMixerNode.KernelDefs, DirectionMixerNode.Kernel>
    , IMsgHandler<Rig>
    , IMsgHandler<BlobAssetReference<Clip>>
    , IMsgHandler<ClipConfiguration>
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
    }

    public struct KernelDefs : IKernelPortDefinition
    {
        public DataInput<DirectionMixerNode, float> Time;
        public DataInput<DirectionMixerNode, float> DeltaTime;
        public DataInput<DirectionMixerNode, float> Weight;

        public DataOutput<DirectionMixerNode, Buffer<AnimatedData>> Output;
    }

    public struct Data : INodeData
    {
        public NodeHandle<KernelPassThroughNodeFloat> TimeNode;
        public NodeHandle<KernelPassThroughNodeFloat> DeltaTimeNode;
        public NodeHandle<KernelPassThroughNodeFloat> WeightNode;
        
        public NodeHandle<UberClipNode> Clip0;
        public NodeHandle<UberClipNode> Clip1;
        public NodeHandle<UberClipNode> Clip2;
        public NodeHandle<UberClipNode> Clip3;
        public NodeHandle<UberClipNode> Clip4;

        public NodeHandle<NMixerNode> NMixerNode;
        public NodeHandle<DirectionMixerComputeWeightNode> ComputeWeightNode;
    }

    public struct KernelData : IKernelData
    {
    }

    [BurstCompile]
    public struct Kernel : IGraphKernel<KernelData, KernelDefs>
    {
        public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
        {
        }
    }

    protected override void Init(InitContext ctx)
    {
        ref var nodeData = ref GetNodeData(ctx.Handle);

        nodeData.TimeNode = Set.Create<KernelPassThroughNodeFloat>();
        nodeData.DeltaTimeNode = Set.Create<KernelPassThroughNodeFloat>();
        nodeData.WeightNode = Set.Create<KernelPassThroughNodeFloat>();
        
        nodeData.Clip0 = Set.Create<UberClipNode>();
        nodeData.Clip1 = Set.Create<UberClipNode>();
        nodeData.Clip2 = Set.Create<UberClipNode>();
        nodeData.Clip3 = Set.Create<UberClipNode>();
        nodeData.Clip4 = Set.Create<UberClipNode>();

        nodeData.NMixerNode = Set.Create<NMixerNode>();
        nodeData.ComputeWeightNode = Set.Create<DirectionMixerComputeWeightNode>();

        Set.Connect(nodeData.TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, nodeData.Clip0, UberClipNode.KernelPorts.Time);
        Set.Connect(nodeData.TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, nodeData.Clip1, UberClipNode.KernelPorts.Time);
        Set.Connect(nodeData.TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, nodeData.Clip2, UberClipNode.KernelPorts.Time);
        Set.Connect(nodeData.TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, nodeData.Clip3, UberClipNode.KernelPorts.Time);
        Set.Connect(nodeData.TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, nodeData.Clip4, UberClipNode.KernelPorts.Time);
        
        Set.Connect(nodeData.DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, nodeData.Clip0, UberClipNode.KernelPorts.DeltaTime);
        Set.Connect(nodeData.DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, nodeData.Clip1, UberClipNode.KernelPorts.DeltaTime);
        Set.Connect(nodeData.DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, nodeData.Clip2, UberClipNode.KernelPorts.DeltaTime);
        Set.Connect(nodeData.DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, nodeData.Clip3, UberClipNode.KernelPorts.DeltaTime);
        Set.Connect(nodeData.DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, nodeData.Clip4, UberClipNode.KernelPorts.DeltaTime);

        Set.SetPortArraySize(nodeData.NMixerNode, NMixerNode.KernelPorts.Inputs, k_ClipCount);
        Set.Connect(nodeData.Clip0, UberClipNode.KernelPorts.Output, nodeData.NMixerNode, NMixerNode.KernelPorts.Inputs, 0);
        Set.Connect(nodeData.Clip1, UberClipNode.KernelPorts.Output, nodeData.NMixerNode, NMixerNode.KernelPorts.Inputs, 1);
        Set.Connect(nodeData.Clip2, UberClipNode.KernelPorts.Output, nodeData.NMixerNode, NMixerNode.KernelPorts.Inputs, 2);
        Set.Connect(nodeData.Clip3, UberClipNode.KernelPorts.Output, nodeData.NMixerNode, NMixerNode.KernelPorts.Inputs, 3);
        Set.Connect(nodeData.Clip4, UberClipNode.KernelPorts.Output, nodeData.NMixerNode, NMixerNode.KernelPorts.Inputs, 4);

        Set.Connect(nodeData.WeightNode, KernelPassThroughNodeFloat.KernelPorts.Output, nodeData.ComputeWeightNode, DirectionMixerComputeWeightNode.KernelPorts.Input);
        Set.SetPortArraySize(nodeData.NMixerNode, NMixerNode.KernelPorts.Weights, k_ClipCount);
        Set.Connect(nodeData.ComputeWeightNode, DirectionMixerComputeWeightNode.KernelPorts.OutWeight0, nodeData.NMixerNode, NMixerNode.KernelPorts.Weights, 0);
        Set.Connect(nodeData.ComputeWeightNode, DirectionMixerComputeWeightNode.KernelPorts.OutWeight1, nodeData.NMixerNode, NMixerNode.KernelPorts.Weights, 1);
        Set.Connect(nodeData.ComputeWeightNode, DirectionMixerComputeWeightNode.KernelPorts.OutWeight2, nodeData.NMixerNode, NMixerNode.KernelPorts.Weights, 2);
        Set.Connect(nodeData.ComputeWeightNode, DirectionMixerComputeWeightNode.KernelPorts.OutWeight3, nodeData.NMixerNode, NMixerNode.KernelPorts.Weights, 3);
        Set.Connect(nodeData.ComputeWeightNode, DirectionMixerComputeWeightNode.KernelPorts.OutWeight4, nodeData.NMixerNode, NMixerNode.KernelPorts.Weights, 4);

        ctx.ForwardInput(KernelPorts.Time, nodeData.TimeNode, KernelPassThroughNodeFloat.KernelPorts.Input);
        ctx.ForwardInput(KernelPorts.DeltaTime, nodeData.DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Input);
        ctx.ForwardInput(KernelPorts.Weight, nodeData.WeightNode, KernelPassThroughNodeFloat.KernelPorts.Input);
        ctx.ForwardOutput(KernelPorts.Output, nodeData.NMixerNode, NMixerNode.KernelPorts.Output);
    }

    protected override void Destroy(NodeHandle handle)
    {
        var nodeData = GetNodeData(handle);

        Set.Destroy(nodeData.TimeNode);
        Set.Destroy(nodeData.DeltaTimeNode);
        Set.Destroy(nodeData.WeightNode);
        
        Set.Destroy(nodeData.Clip0);
        Set.Destroy(nodeData.Clip1);
        Set.Destroy(nodeData.Clip2);
        Set.Destroy(nodeData.Clip3);
        Set.Destroy(nodeData.Clip4);
        
        Set.Destroy(nodeData.NMixerNode);
        Set.Destroy(nodeData.ComputeWeightNode);
    }

    public void HandleMessage(in MessageContext ctx, in Rig rig)
    {
        // All ports are forwarded
        var nodeData = GetNodeData(ctx.Handle);
        Set.SendMessage(nodeData.Clip0, UberClipNode.SimulationPorts.Rig, rig);
        Set.SendMessage(nodeData.Clip1, UberClipNode.SimulationPorts.Rig, rig);
        Set.SendMessage(nodeData.Clip2, UberClipNode.SimulationPorts.Rig, rig);
        Set.SendMessage(nodeData.Clip3, UberClipNode.SimulationPorts.Rig, rig);
        Set.SendMessage(nodeData.Clip4, UberClipNode.SimulationPorts.Rig, rig);
        Set.SendMessage(nodeData.NMixerNode, NMixerNode.SimulationPorts.Rig, rig);
    }

    public void HandleMessage(in MessageContext ctx, in BlobAssetReference<Clip> clip)
    {
        // All ports are forwarded
        var nodeData = GetNodeData(ctx.Handle);
        if (ctx.Port == SimulationPorts.Clip0)
        {
            Set.SendMessage(nodeData.Clip0, UberClipNode.SimulationPorts.Clip, clip);
        }
        else if (ctx.Port == SimulationPorts.Clip1)
        {
            Set.SendMessage(nodeData.Clip1, UberClipNode.SimulationPorts.Clip, clip);
        }
        else if (ctx.Port == SimulationPorts.Clip2)
        {
            Set.SendMessage(nodeData.Clip2, UberClipNode.SimulationPorts.Clip, clip);
        }
        else if (ctx.Port == SimulationPorts.Clip3)
        {
            Set.SendMessage(nodeData.Clip3, UberClipNode.SimulationPorts.Clip, clip);
        }
        else if (ctx.Port == SimulationPorts.Clip4)
        {
            Set.SendMessage(nodeData.Clip4, UberClipNode.SimulationPorts.Clip, clip);
        }
    }

    public void HandleMessage(in MessageContext ctx, in ClipConfiguration clipConfiguration)
    {
        var nodeData = GetNodeData(ctx.Handle);
        Set.SendMessage(nodeData.Clip0, UberClipNode.SimulationPorts.Configuration, clipConfiguration);
        Set.SendMessage(nodeData.Clip1, UberClipNode.SimulationPorts.Configuration, clipConfiguration);
        Set.SendMessage(nodeData.Clip2, UberClipNode.SimulationPorts.Configuration, clipConfiguration);
        Set.SendMessage(nodeData.Clip3, UberClipNode.SimulationPorts.Configuration, clipConfiguration);
        Set.SendMessage(nodeData.Clip4, UberClipNode.SimulationPorts.Configuration, clipConfiguration);
    }
}

public class DirectionMixerComputeWeightNode
    : NodeDefinition<DirectionMixerComputeWeightNode.Data, DirectionMixerComputeWeightNode.SimPorts, DirectionMixerComputeWeightNode.KernelData, DirectionMixerComputeWeightNode.KernelDefs, DirectionMixerComputeWeightNode.Kernel>
{
    public struct SimPorts : ISimulationPortDefinition { }

    public struct KernelDefs : IKernelPortDefinition
    {
        public DataInput<DirectionMixerComputeWeightNode, float> Input;

        public DataOutput<DirectionMixerComputeWeightNode, float> OutWeight0;
        public DataOutput<DirectionMixerComputeWeightNode, float> OutWeight1;
        public DataOutput<DirectionMixerComputeWeightNode, float> OutWeight2;
        public DataOutput<DirectionMixerComputeWeightNode, float> OutWeight3;
        public DataOutput<DirectionMixerComputeWeightNode, float> OutWeight4;
    }

    public struct Data : INodeData { }

    public struct KernelData : IKernelData { }

    [BurstCompile]
    public struct Kernel : IGraphKernel<KernelData, KernelDefs>
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
