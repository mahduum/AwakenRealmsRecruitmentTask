using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Task3.AuthoringAndComponents
{
#region Components

    public struct ChangedRangeComponent : IComponentData, IEnableableComponent
    {
        public int ChangedRangeIndex;
    }

    public struct RangeComponent : IComponentData
    {
        public int CurrentRangeIndex;
    }

    public struct HeadingComponent : IComponentData
    {
        public float3 Value;
    }
    
    public struct RangeReferenceDistanceSqComponent : IComponentData
    {
        public float Value;
    }
    
    /*Unused testing component, more detailed description in UpdateDeltaTimeSystem.cs*/
    // public struct RangeDeltaTimeComponent : IComponentData, IEnableableComponent
    // {
    //     public int UpdateFrame;
    //     public int CurrentFrame;
    //     public float DeltaTime;
    // }

    public struct HighPrecisionRange : IComponentData, IEnableableComponent
    {
    }
    
    public struct MediumPrecisionRange : IComponentData, IEnableableComponent
    {
    }
    
    public struct LowPrecisionRange : IComponentData, IEnableableComponent
    {
    }
    
    public struct LowestPrecisionRange : IComponentData, IEnableableComponent
    {
    }
    
#endregion

#region Authoring

    [Serializable]
    public class RangeSettings
    {
        [Range(0, 1)] public float NormalizedRange;
        public Color RangeColor;
    }
    
    public class RangeObjectAuthoring : MonoBehaviour
    {
        [SerializeField] private float _rangesMultiplier;
        [SerializeField] private RangeSettings[] _rangeSettings;
        [SerializeField] private Color _infinityColor;

        private void OnValidate()
        {
            if (_rangeSettings == null || _rangeSettings.Length < 1)
            {
                return;
            }

            bool updateBake = false;
            float prevMin = _rangeSettings[0].NormalizedRange;
            for (int i = 1; i < _rangeSettings.Length; i++)
            {
                float currentMin = _rangeSettings[i].NormalizedRange;
                if (currentMin < prevMin)
                {
                    _rangeSettings[i].NormalizedRange = prevMin;
                    updateBake = true;
                }
                else
                {
                    prevMin = currentMin;
                }
            }

            if (updateBake)
            {
                OnValidate();
            }
        }

        public class RangeObjectBaker : Baker<RangeObjectAuthoring>
        {
            public override void Bake(RangeObjectAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                var ranges = authoring._rangeSettings;
                var multiplier = authoring._rangesMultiplier;
                
                AddRangesSettingsWithSharedComponent(authoring, ranges, multiplier, entity);

                AddSharedComponent(entity, new TranslationBoundsSharedComponent());
                
                AddComponent(entity, new HeadingComponent()
                {
                });
            }

            private void AddRangesSettingsWithSharedComponent(RangeObjectAuthoring authoring, RangeSettings[] ranges, float multiplier,
                Entity entity)
            {
                if (ranges.Length < 1)
                {
                    Debug.LogError("Some range object prefab is missing ranges valid settings!");
                    return;
                }
                
                var blobRangesBuilder = CreateBlobAssetRanges(ranges, multiplier, out var blobAssetRanges);

                var blobRangesColorsBuilder = CreateBlobAssetColorRanges(authoring, ranges, out var blobAssetRangesColors);

                AddSharedComponent(entity, new RangeSettingsShared()
                {
                    PrefabRadius = authoring.GetComponent<MeshFilter>().sharedMesh.bounds.extents.magnitude,
                    Ranges = blobAssetRanges,
                    RangesColors = blobAssetRangesColors
                });
                
                AddBlobAsset<BlobRanges>(ref blobAssetRanges, out var hash);
                AddBlobAsset<BlobRangesColors>(ref blobAssetRangesColors, out var hashColors);
                
                blobRangesBuilder.Dispose();
                blobRangesColorsBuilder.Dispose();

                
                AddComponent(entity, new RangeComponent());
                
                AddComponent(entity, new RangeReferenceDistanceSqComponent());
                
                AddComponent(entity, new ChangedRangeComponent());
                SetComponentEnabled<ChangedRangeComponent>(entity, true);
                
                AddComponent(entity, new HighPrecisionRange());
                SetComponentEnabled<HighPrecisionRange>(entity, true);

                AddComponent(entity, new MediumPrecisionRange());
                SetComponentEnabled<MediumPrecisionRange>(entity, false);

                AddComponent(entity, new LowPrecisionRange());
                SetComponentEnabled<LowPrecisionRange>(entity, false);

                AddComponent(entity, new LowestPrecisionRange());
                SetComponentEnabled<LowestPrecisionRange>(entity, false);

            }

            private static BlobBuilder CreateBlobAssetColorRanges(RangeObjectAuthoring authoring, RangeSettings[] ranges,
                out BlobAssetReference<BlobRangesColors> blobAssetRangesColors)
            {
                var blobRangesColorsBuilder = new BlobBuilder(Allocator.Temp);
                ref var blobRangesColors = ref blobRangesColorsBuilder.ConstructRoot<BlobRangesColors>();
                var arrayRangesColorsBuilder = blobRangesColorsBuilder.Allocate(ref blobRangesColors.Values, ranges.Length + 1);
                
                for (int i = 0; i < ranges.Length; i++)
                {
                    arrayRangesColorsBuilder[i] = new float4(
                        ranges[i].RangeColor.r,
                        ranges[i].RangeColor.g,
                        ranges[i].RangeColor.b,
                        ranges[i].RangeColor.a);
                }
                
                arrayRangesColorsBuilder[^1] = new float4(
                    authoring._infinityColor.r,
                    authoring._infinityColor.g,
                    authoring._infinityColor.b,
                    authoring._infinityColor.a);
                
                blobAssetRangesColors = blobRangesColorsBuilder.CreateBlobAssetReference<BlobRangesColors>(Allocator.Persistent);
                return blobRangesColorsBuilder;
            }

            private static BlobBuilder CreateBlobAssetRanges(RangeSettings[] ranges, float multiplier,
                out BlobAssetReference<BlobRanges> blobAssetRanges)
            {
                var blobRangesBuilder = new BlobBuilder(Allocator.Temp);
                ref BlobRanges blobRanges = ref blobRangesBuilder.ConstructRoot<BlobRanges>();
                var arrayRangesBuilder = blobRangesBuilder.Allocate(ref blobRanges.Values, ranges.Length + 1);
                
                for (int i = 0; i <= ranges.Length; i ++)
                {
                    int? maxIndex = i < ranges.Length ? i : null;
                    int? minIndex = i - 1 < 0 ? null : maxIndex.HasValue ? i - 1 : ranges.Length - 1;
                    var limits = new RangeLimits()
                    {
                        Min = minIndex.HasValue ? ranges[minIndex.Value].NormalizedRange * multiplier : 0,
                        Max = maxIndex.HasValue ? ranges[maxIndex.Value].NormalizedRange * multiplier : float.PositiveInfinity
                    };
                    
                    arrayRangesBuilder[i] = limits;
                }
                
                blobAssetRanges = blobRangesBuilder.CreateBlobAssetReference<BlobRanges>(Allocator.Persistent);
                return blobRangesBuilder;
            }
        }
    }
#endregion
}