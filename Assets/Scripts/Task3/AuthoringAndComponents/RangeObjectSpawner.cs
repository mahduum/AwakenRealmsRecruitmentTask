using Unity.Entities;

namespace Task3.AuthoringAndComponents
{
    public struct RangeObjectSpawner : IComponentData
    {
        public BlobAssetReference<BlobEntityPrefabTypes> EntityPrefabs;
        public int NumberToSpawn;
    }

    public struct BlobEntityPrefabTypes
    {
        public BlobArray<Entity> EntityPrefabs;
    }
}