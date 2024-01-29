using System;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Code
{
	[Serializable]
	// Feel free to change it to suit your needs
	public class NaiveDataProvider : IObjectsDataProvider
	{
		[SerializeField] private uint _count = 2_001;
		private float3[] _positions;
		private float[] _radii;
		private int[] _bands; // int because of -1 is not initialized, and going from -1 to any other wont be marked as changed
		private bool[] _bandChanged;
		private ushort[] _classIndices;

		public uint Count => _count;

		public void Awake(Bounds bounds, Span<ClassDefinition> classes)
		{
			var random = new Random(69);

			_positions = new float3[_count];
			_radii = new float[_count];
			_bands = new int[_count];
			_bandChanged = new bool[_count];
			_classIndices = new ushort[_count];

			Array.Fill(_bands, -1);
			for (var i = 0; i < _count; i++)
			{
				_positions[i] = random.NextFloat3(bounds.min, bounds.max);
				_radii[i] = random.NextFloat(0.1f, 1.5f);
				_classIndices[i] = (ushort)random.NextUInt(0, (ushort)classes.Length);
			}
		}

		public void GetData(uint index, out float3 position, out float radius, out int band, out bool bandChanged, out ushort classIndex)
		{
			if (index >= _count)
			{
				position = default;
				radius = 0;
				band = 0;
				bandChanged = false;
				classIndex = 0;
				return;
			}

			position = _positions[index];
			radius = _radii[index];
			band = _bands[index];
			bandChanged = _bandChanged[index];
			classIndex = _classIndices[index];
		}

		public ref float3 GetPosition(uint index)
		{
			return ref _positions[index];
		}
	}
}
