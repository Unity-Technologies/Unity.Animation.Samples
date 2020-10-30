using Unity.Animation;
using Unity.Entities;


public struct WeightComponent : IComponentData
{
    public float Value;
}

public struct SampleClip : IBufferElementData
{
    public BlobAssetReference<Clip> Clip;
}
public struct SampleClipDuration : IBufferElementData
{
    public float Value;
}

public struct SampleClipBlendThreshold : IBufferElementData
{
    public float Value;
}
