using Unity.Animation;
using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.Entities;

public struct DeltaTime : IComponentData
{
    public float Value;
}

[UpdateBefore(typeof(DefaultAnimationSystemGroup))]
public class UpdateAnimationDeltaTime : SystemBase
{
    protected override void OnUpdate()
    {
        var worldDeltaTime = World.Time.DeltaTime;
        Entities.ForEach((Entity Entity, ref DeltaTime dt) =>
        {
            dt.Value = worldDeltaTime;
        }).ScheduleParallel();
    }
}

public class ConvertDeltaTimeToFloatNode : ConvertToBase<
    ConvertDeltaTimeToFloatNode,
    DeltaTime,
    float,
    ConvertDeltaTimeToFloatNode.Kernel
>
{
    [BurstCompile]
    public struct Kernel : IGraphKernel<KernelData, KernelDefs>
    {
        public void Execute(RenderContext ctx, KernelData data, ref KernelDefs ports) =>
            ctx.Resolve(ref ports.Output) = ctx.Resolve(ports.Input).Value;
    }
}
