using UnityEngine;
using Unity.Mathematics;

public class BlendTree2DInput : AnimationInputBase<BlendTree2DData>
{
    public float ParamXStep = 0.1f;
    public float ParamYStep = 0.1f;

    protected override void UpdateComponentData(ref BlendTree2DData data)
    {
        var deltaX = Input.GetAxisRaw("Horizontal");
        var deltaY = Input.GetAxisRaw("Vertical");

        data.paramX = math.clamp(data.paramX + deltaX * ParamXStep, -1.0f, 1.0f);
        data.paramY = math.clamp(data.paramY + deltaY * ParamYStep, -1.0f, 1.0f);
    }
}
