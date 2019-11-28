using Unity.Animation;
using Unity.Entities;

public struct CurveBlobComponent : IComponentData
{
    public BlobAssetReference<KeyframeCurveBlob> CurveBlobRef;
}

public struct CurveTag : IComponentData
{
}

