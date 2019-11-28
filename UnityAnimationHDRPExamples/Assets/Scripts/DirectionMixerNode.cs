using Unity.Animation;
using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Mathematics;

public class DirectionMixerNode
    : NodeDefinition<DirectionMixerNode.Data, DirectionMixerNode.SimPorts, DirectionMixerNode.KernelData, DirectionMixerNode.KernelDefs, DirectionMixerNode.Kernel>
        , IMsgHandler<BlobAssetReference<ClipInstance>>
        , IMsgHandler<ClipConfiguration>
        , IMsgHandler<float>
{ 
    public struct SimPorts : ISimulationPortDefinition
    {
        public MessageInput<DirectionMixerNode, ClipConfiguration> ClipConfiguration;

        public MessageInput<DirectionMixerNode, BlobAssetReference<ClipInstance>> Clip0;
        public MessageInput<DirectionMixerNode, BlobAssetReference<ClipInstance>> Clip1;
        public MessageInput<DirectionMixerNode, BlobAssetReference<ClipInstance>> Clip2;
        public MessageInput<DirectionMixerNode, BlobAssetReference<ClipInstance>> Clip3;
        public MessageInput<DirectionMixerNode, BlobAssetReference<ClipInstance>> Clip4;
        
        public MessageInput<DirectionMixerNode, float> Blend;
    }

    public struct KernelDefs : IKernelPortDefinition
    {
        public DataInput<DirectionMixerNode, float> Time;
        public DataInput<DirectionMixerNode, float> DeltaTime;
        public DataOutput<DirectionMixerNode, Buffer<float>> Output;
    }

    public struct Data : INodeData
    {
        public NodeHandle<KernelPassThroughNodeFloat> TimeNode;
        public NodeHandle<KernelPassThroughNodeFloat> DeltaTimeNode;
        
        public NodeHandle<UberClipNode> Clip0;
        public NodeHandle<UberClipNode> Clip1;
        public NodeHandle<UberClipNode> Clip2;
        public NodeHandle<UberClipNode> Clip3;
        public NodeHandle<UberClipNode> Clip4;
        
        public NodeHandle<MixerBeginNode> MixerBeginNode;
        public NodeHandle<MixerAddNode> MixerAddNode0;
        public NodeHandle<MixerAddNode> MixerAddNode1;
        public NodeHandle<MixerAddNode> MixerAddNode2;
        public NodeHandle<MixerAddNode> MixerAddNode3;
        public NodeHandle<MixerAddNode> MixerAddNode4;
        public NodeHandle<MixerEndNode> MixerEndNode;
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

    public override void Init(InitContext ctx)
    {
        ref var nodeData = ref GetNodeData(ctx.Handle);

        nodeData.TimeNode = Set.Create<KernelPassThroughNodeFloat>();
        nodeData.DeltaTimeNode = Set.Create<KernelPassThroughNodeFloat>();
        
        nodeData.Clip0 = Set.Create<UberClipNode>();
        nodeData.Clip1 = Set.Create<UberClipNode>();
        nodeData.Clip2 = Set.Create<UberClipNode>();
        nodeData.Clip3 = Set.Create<UberClipNode>();
        nodeData.Clip4 = Set.Create<UberClipNode>();
        
        nodeData.MixerBeginNode = Set.Create<MixerBeginNode>();
        nodeData.MixerAddNode0 = Set.Create<MixerAddNode>();
        nodeData.MixerAddNode1 = Set.Create<MixerAddNode>();
        nodeData.MixerAddNode2 = Set.Create<MixerAddNode>();
        nodeData.MixerAddNode3 = Set.Create<MixerAddNode>();
        nodeData.MixerAddNode4 = Set.Create<MixerAddNode>();
        nodeData.MixerEndNode = Set.Create<MixerEndNode>();
        
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
        
        Set.Connect(nodeData.Clip0, UberClipNode.KernelPorts.Output, nodeData.MixerAddNode0, MixerAddNode.KernelPorts.Add);
        Set.Connect(nodeData.Clip1, UberClipNode.KernelPorts.Output, nodeData.MixerAddNode1, MixerAddNode.KernelPorts.Add);
        Set.Connect(nodeData.Clip2, UberClipNode.KernelPorts.Output, nodeData.MixerAddNode2, MixerAddNode.KernelPorts.Add);
        Set.Connect(nodeData.Clip3, UberClipNode.KernelPorts.Output, nodeData.MixerAddNode3, MixerAddNode.KernelPorts.Add);
        Set.Connect(nodeData.Clip4, UberClipNode.KernelPorts.Output, nodeData.MixerAddNode4, MixerAddNode.KernelPorts.Add);
        
        Set.Connect(nodeData.MixerBeginNode, MixerBeginNode.KernelPorts.SumWeight, nodeData.MixerAddNode0, MixerAddNode.KernelPorts.SumWeightInput);
        Set.Connect(nodeData.MixerAddNode0, MixerAddNode.KernelPorts.SumWeightOutput, nodeData.MixerAddNode1, MixerAddNode.KernelPorts.SumWeightInput);
        Set.Connect(nodeData.MixerAddNode1, MixerAddNode.KernelPorts.SumWeightOutput, nodeData.MixerAddNode2, MixerAddNode.KernelPorts.SumWeightInput);
        Set.Connect(nodeData.MixerAddNode2, MixerAddNode.KernelPorts.SumWeightOutput, nodeData.MixerAddNode3, MixerAddNode.KernelPorts.SumWeightInput);
        Set.Connect(nodeData.MixerAddNode3, MixerAddNode.KernelPorts.SumWeightOutput, nodeData.MixerAddNode4, MixerAddNode.KernelPorts.SumWeightInput);
        Set.Connect(nodeData.MixerAddNode4, MixerAddNode.KernelPorts.SumWeightOutput, nodeData.MixerEndNode, MixerEndNode.KernelPorts.SumWeight);

        Set.Connect(nodeData.MixerBeginNode, MixerBeginNode.KernelPorts.Output, nodeData.MixerAddNode0, MixerAddNode.KernelPorts.Input);
        Set.Connect(nodeData.MixerAddNode0, MixerAddNode.KernelPorts.Output, nodeData.MixerAddNode1, MixerAddNode.KernelPorts.Input);
        Set.Connect(nodeData.MixerAddNode1, MixerAddNode.KernelPorts.Output, nodeData.MixerAddNode2, MixerAddNode.KernelPorts.Input);
        Set.Connect(nodeData.MixerAddNode2, MixerAddNode.KernelPorts.Output, nodeData.MixerAddNode3, MixerAddNode.KernelPorts.Input);
        Set.Connect(nodeData.MixerAddNode3, MixerAddNode.KernelPorts.Output, nodeData.MixerAddNode4, MixerAddNode.KernelPorts.Input);
        Set.Connect(nodeData.MixerAddNode4, MixerAddNode.KernelPorts.Output, nodeData.MixerEndNode, MixerEndNode.KernelPorts.Input);

        ctx.ForwardInput(KernelPorts.Time, nodeData.TimeNode, KernelPassThroughNodeFloat.KernelPorts.Input);
        ctx.ForwardInput(KernelPorts.DeltaTime, nodeData.DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Input);
        ctx.ForwardOutput(KernelPorts.Output, nodeData.MixerEndNode, MixerEndNode.KernelPorts.Output);
    }

    public override void Destroy(NodeHandle handle)
    {
        var nodeData = GetNodeData(handle);

        Set.Destroy(nodeData.TimeNode);
        Set.Destroy(nodeData.DeltaTimeNode);
        
        Set.Destroy(nodeData.Clip0);
        Set.Destroy(nodeData.Clip1);
        Set.Destroy(nodeData.Clip2);
        Set.Destroy(nodeData.Clip3);
        Set.Destroy(nodeData.Clip4);
        
        Set.Destroy(nodeData.MixerBeginNode);
        Set.Destroy(nodeData.MixerAddNode0);
        Set.Destroy(nodeData.MixerAddNode1);
        Set.Destroy(nodeData.MixerAddNode2);
        Set.Destroy(nodeData.MixerAddNode3);
        Set.Destroy(nodeData.MixerAddNode4);
        Set.Destroy(nodeData.MixerEndNode);
    }

    public void HandleMessage(in MessageContext ctx, in BlobAssetReference<ClipInstance> clipInstance)
    {
        var nodeData = GetNodeData(ctx.Handle);

        if (ctx.Port == SimulationPorts.Clip0)
        {
            Set.SendMessage(nodeData.Clip0, UberClipNode.SimulationPorts.ClipInstance, clipInstance);
            
            Set.SendMessage(nodeData.MixerBeginNode, MixerBeginNode.SimulationPorts.RigDefinition, clipInstance.Value.RigDefinition);
            Set.SendMessage(nodeData.MixerAddNode0, MixerAddNode.SimulationPorts.RigDefinition, clipInstance.Value.RigDefinition);
            Set.SendMessage(nodeData.MixerAddNode1, MixerAddNode.SimulationPorts.RigDefinition, clipInstance.Value.RigDefinition);
            Set.SendMessage(nodeData.MixerAddNode2, MixerAddNode.SimulationPorts.RigDefinition, clipInstance.Value.RigDefinition);
            Set.SendMessage(nodeData.MixerAddNode3, MixerAddNode.SimulationPorts.RigDefinition, clipInstance.Value.RigDefinition);
            Set.SendMessage(nodeData.MixerAddNode4, MixerAddNode.SimulationPorts.RigDefinition, clipInstance.Value.RigDefinition);
            Set.SendMessage(nodeData.MixerEndNode, MixerEndNode.SimulationPorts.RigDefinition, clipInstance.Value.RigDefinition);
        }
        else if (ctx.Port == SimulationPorts.Clip1)
        {
            Set.SendMessage(nodeData.Clip1, UberClipNode.SimulationPorts.ClipInstance, clipInstance);
        }
        else if (ctx.Port == SimulationPorts.Clip2)
        {
            Set.SendMessage(nodeData.Clip2, UberClipNode.SimulationPorts.ClipInstance, clipInstance);   
    
        }
        else if (ctx.Port == SimulationPorts.Clip3)
        {
            Set.SendMessage(nodeData.Clip3, UberClipNode.SimulationPorts.ClipInstance, clipInstance);   
        }
        else if (ctx.Port == SimulationPorts.Clip4)
        {
            Set.SendMessage(nodeData.Clip4, UberClipNode.SimulationPorts.ClipInstance, clipInstance);
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
    
    public void HandleMessage(in MessageContext ctx, in float msg)
    {
        ref var nodeData = ref GetNodeData(ctx.Handle);

        Set.SetData(nodeData.MixerAddNode0, MixerAddNode.KernelPorts.Weight, 0);
        Set.SetData(nodeData.MixerAddNode1, MixerAddNode.KernelPorts.Weight, 0);
        Set.SetData(nodeData.MixerAddNode2, MixerAddNode.KernelPorts.Weight, 0);
        Set.SetData(nodeData.MixerAddNode3, MixerAddNode.KernelPorts.Weight, 0);
        Set.SetData(nodeData.MixerAddNode4, MixerAddNode.KernelPorts.Weight, 0);

        float index = 0;
        var weight1 = math.modf(math.clamp(msg, 0, 4), out index);
        var weight0 = 1 - weight1;
        
        if (index < 1)
        {
            Set.SetData(nodeData.MixerAddNode0, MixerAddNode.KernelPorts.Weight, weight0);
            Set.SetData(nodeData.MixerAddNode1, MixerAddNode.KernelPorts.Weight, weight1);
        }
        else if (index < 2)
        {
            Set.SetData(nodeData.MixerAddNode1, MixerAddNode.KernelPorts.Weight, weight0);
            Set.SetData(nodeData.MixerAddNode2, MixerAddNode.KernelPorts.Weight, weight1);
        }
        else if (index < 3)
        {
            Set.SetData(nodeData.MixerAddNode2, MixerAddNode.KernelPorts.Weight, weight0);
            Set.SetData(nodeData.MixerAddNode3, MixerAddNode.KernelPorts.Weight, weight1);
        }
        else if (index < 4)
        {
            Set.SetData(nodeData.MixerAddNode3, MixerAddNode.KernelPorts.Weight, weight0);
            Set.SetData(nodeData.MixerAddNode4, MixerAddNode.KernelPorts.Weight, weight1);
        }
        else
        {
            Set.SetData(nodeData.MixerAddNode4, MixerAddNode.KernelPorts.Weight, 1);
        }
    }
}
