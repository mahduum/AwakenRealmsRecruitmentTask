using System;
using Unity.Mathematics;
using UnityEngine;

namespace Code
{
	// You can change it to suit your needs
	public interface IObjectsDataProvider
	{
		uint Count { get; }

		void Awake(Bounds bounds, Span<ClassDefinition> classes);
		void GetData(uint index, out float3 position, out float radius, out int band, out bool bandChanged, out ushort classIndex);
		ref float3 GetPosition(uint index);
	}
}
