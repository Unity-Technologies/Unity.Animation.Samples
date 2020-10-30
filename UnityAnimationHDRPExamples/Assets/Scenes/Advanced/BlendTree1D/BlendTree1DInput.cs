using UnityEngine;
using Unity.Mathematics;

public class BlendTree1DInput : AnimationInputBase<BlendTree1DData>
{
    public float ParamXStep = 0.1f;

    protected override void UpdateComponentData(ref BlendTree1DData data)
    {
        var delta = Input.GetAxisRaw("Vertical");

        data.paramX = math.clamp(data.paramX + delta * ParamXStep, 0.0f, 1.0f);
    }
}
