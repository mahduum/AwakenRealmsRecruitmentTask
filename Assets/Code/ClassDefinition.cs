using System;
using UnityEngine;

namespace Code
{
	[Serializable]
	public struct ClassDefinition
	{
		public Color color;

		public ClassDefinition(Color color)
		{
			this.color = color;
		}
	}
}
