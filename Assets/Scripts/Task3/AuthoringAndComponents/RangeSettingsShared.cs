using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Task3.AuthoringAndComponents
{
    [Serializable]
    public struct RangeSettingsShared : ISharedComponentData
    {
        public float PrefabRadius;
        public BlobAssetReference<BlobRanges> Ranges;
        public BlobAssetReference<BlobRangesColors> RangesColors;
    }

    public struct BlobRanges
    {
        public BlobArray<RangeLimits> Values;
    }

    public struct RangeLimits
    {
        public float Min;
        public float Max;
    }

    public struct BlobRangesColors
    {
        public BlobArray<float4> Values;
    }
}