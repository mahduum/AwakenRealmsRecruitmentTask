using Unity.Entities;

namespace Task3.AuthoringAndComponents
{
    public struct RangeComponent : IComponentData
    {
        public int CurrentRangeIndex;
        //public int PreviousRangeIndex;
    }
}