using System;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Code
{
	// Straightforward implementation of movement, slow and ugly, but it's not the point of the test
	[Serializable]
	public class ObjectsMovement
	{
		private IObjectsDataProvider _dataProvider;
		private Bounds _bounds;

		private float3[] _directions;

		public ObjectsMovement(Bounds bounds, IObjectsDataProvider dataProvider)
		{
			_dataProvider = dataProvider;
			_bounds = bounds;
			_directions = new float3[_dataProvider.Count];

			var random = new Random(70);
			for (var i = 0u; i < _directions.Length; i++)
			{
				_directions[i] = random.NextFloat3Direction() * random.NextFloat(2f, 5f);
			}
		}

		public void Update()
		{
			for (var i = 0u; i < _directions.Length; i++)
			{
				ref var position = ref _dataProvider.GetPosition(i);
				ref var direction = ref _directions[i];
				position += direction * Time.deltaTime;
				if (position.x > _bounds.max.x || position.x < _bounds.min.x)
				{
					direction.x *= -1;
				}
				if (position.y > _bounds.max.y || position.y < _bounds.min.y)
				{
					direction.y *= -1;
				}
				if (position.z > _bounds.max.z || position.z < _bounds.min.z)
				{
					direction.z *= -1;
				}
			}
		}
	}
}
