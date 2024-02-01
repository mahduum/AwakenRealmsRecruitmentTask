using System;
using System.Linq;
using Task3.Aspects;
using Task3.AuthoringAndComponents;
using Task3.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Task3.Authoring
{
    public class RangeObjectAuthoring : MonoBehaviour
    {
        [SerializeField] private float _rangesMultiplier;
        [SerializeField] [Range(0,1)] private float[] _normalizedRanges;

        private void OnValidate()
        {
            if (_normalizedRanges == null || _normalizedRanges.Length < 1)
            {
                return;
            }

            bool updateBake = false;
            float prevMin = _normalizedRanges[0];
            for (int i = 1; i < _normalizedRanges.Length; i++)
            {
                float currentMin = _normalizedRanges[i];
                if (currentMin < prevMin)
                {
                    _normalizedRanges[i] = prevMin;
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

                var ranges = authoring._normalizedRanges;
                var multiplier = authoring._rangesMultiplier;
                
                AddRangesSettingsWithSharedComponent(authoring, ranges, multiplier, entity);

                AddSharedComponent(entity, new TranslationBoundsSharedComponent());
                
                AddComponent(entity, new HeadingComponent()
                {
                });
                
                AddComponent<IsUninitializedTag>(entity);
            }

            private void AddRangesSettingsWithSharedComponent(RangeObjectAuthoring authoring, float[] ranges, float multiplier,
                Entity entity)
            {
                if (ranges.Length < 1)
                {
                    Debug.LogError("Some range object prefab is missing ranges valid settings!");
                    return;
                }
                
                var blobRangesBuilder = new BlobBuilder(Allocator.Temp);
                ref BlobRanges blobRanges = ref blobRangesBuilder.ConstructRoot<BlobRanges>();
                var arrayRangesBuilder = blobRangesBuilder.Allocate(ref blobRanges.Values, ranges.Length);

                for (int i = 0, j = 0; i < ranges.Length + 1; i += 2, j++)
                {
                    var limits = new RangeLimits()
                    {
                        Min = i - 1 < 0 ? 0 :
                            i - 1 < ranges.Length ? ranges[i - 1] * multiplier : ranges[^1] * multiplier,
                        Max = i < ranges.Length ? ranges[i] * multiplier : float.PositiveInfinity
                    };
                    
                    arrayRangesBuilder[j] = limits;
                }
                
                var blobAssetRanges = blobRangesBuilder.CreateBlobAssetReference<BlobRanges>(Allocator.Persistent);
                //blobRangesBuilder.Dispose();
                
                var blobRangesColorsBuilder = new BlobBuilder(Allocator.Temp);
                ref var blobRangesColors = ref blobRangesColorsBuilder.ConstructRoot<BlobRangesColors>();
                var arrayRangesColorsBuilder = blobRangesColorsBuilder.Allocate(ref blobRangesColors.Values, ranges.Length);
                
                for (int i = 0; i < ranges.Length; i++)
                {
                    arrayRangesColorsBuilder[i] = new float4(
                        Random.value,
                        Random.value,
                        Random.value,
                        1.0f);
                }
                
                BlobAssetReference<BlobRangesColors> blobAssetRangesColors =
                    blobRangesColorsBuilder.CreateBlobAssetReference<BlobRangesColors>(Allocator.Persistent);
                //blobRangesColorsBuilder.Dispose();
                
                AddSharedComponent(entity, new RangeSettingsShared()
                {
                    Radius = authoring.GetComponent<MeshFilter>().sharedMesh.bounds.extents.magnitude,
                    Ranges = blobAssetRanges,
                    RangesColors = blobAssetRangesColors
                });
                
                Debug.Log($"Blob asset: {blobAssetRangesColors.IsCreated}, count: {blobAssetRangesColors.Value.Values.Length}, new array count: {arrayRangesColorsBuilder.Length}");
                
                AddBlobAsset<BlobRanges>(ref blobAssetRanges, out var hash);
                //AddBlobAsset<BlobRangesColors>(ref blobAssetRangesColors, out var hashColors);
                
                blobRangesBuilder.Dispose();
                blobRangesColorsBuilder.Dispose();

                
                AddComponent(entity, new RangeComponent());
                
                AddComponent(entity, new RangeReferenceDistanceSqComponent());
                
                AddComponent(entity, new ChangedRangeComponent());
                SetComponentEnabled<ChangedRangeComponent>(entity, false);
            }
        }
    }
}