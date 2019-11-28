using Unity.Entities;
using Unity.Animation;

[UpdateInGroup(typeof(AnimationSystemGroup))]
[UpdateAfter(typeof(RigComputeMatricesSystem))]
public class BoneRendererSystemGroup : ComponentSystemGroup
{
}

[UpdateInGroup(typeof(BoneRendererSystemGroup))]
public class BoneRendererMatrixSystem : BoneRendererMatrixSystemBase
{
}

[UpdateInGroup(typeof(BoneRendererSystemGroup))]
[UpdateAfter(typeof(BoneRendererMatrixSystem))]
public class BoneRendererRenderingSystem : BoneRendererRenderingSystemBase
{
}