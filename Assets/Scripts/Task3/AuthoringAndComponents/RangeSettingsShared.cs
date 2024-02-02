using System;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine.Serialization;

namespace Task3.AuthoringAndComponents
{
    [Serializable]
    //[WriteGroup(typeof(LocalToWorld))]//queries that require all entities with RangeSettingsShared will be processed exclusively by this one system
    //no other system that processes entities with this component will be able to write to the local to world
    //or this component will not be able to write to it?
    public struct RangeSettingsShared : ISharedComponentData
    {
        public float PrefabRadius;//todo must include for precision
        public BlobAssetReference<BlobRanges> Ranges;
        public BlobAssetReference<BlobRangesColors> RangesColors;
    }

    public struct BlobRanges
    {
        public BlobArray<RangeLimits> Values;//not like this
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