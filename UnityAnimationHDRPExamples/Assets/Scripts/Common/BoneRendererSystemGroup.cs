using Unity.Entities;
using Unity.Animation;

[UpdateAfter(typeof(LateAnimationSystemGroup))]
public class BoneRendererSystemGroup : ComponentSystemGroup
{
}

[UpdateInGroup(typeof(BoneRendererSystemGroup))]
public class BoneRendererMatrixSystem : ComputeBoneRenderingMatricesBase
{
}

[UpdateInGroup(typeof(BoneRendererSystemGroup))]
[UpdateAfter(typeof(BoneRendererMatrixSystem))]
public class BoneRendererRenderingSystem : RenderBonesBase
{
}
