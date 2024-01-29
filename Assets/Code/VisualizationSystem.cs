using System.Text;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Code
{
	public class VisualizationSystem : MonoBehaviour
	{
		[SerializeField] private Bounds _bounds = new Bounds(Vector3.zero, Vector3.one*100);
		[SerializeReference, SubclassSelector] private IObjectsDataProvider _dataProvider;
		[SerializeField] private ClassDefinition[] _classes = new[]
		{
			new ClassDefinition(new Color(0.10f, 0.10f, 0.44f)),
			new ClassDefinition(new Color(0.00f, 0.39f, 0.00f)),
			new ClassDefinition(new Color(1.00f, 0.84f, 0.00f)),
			new ClassDefinition(new Color(1.00f, 0.00f, 0.00f)),
			new ClassDefinition(new Color(0.00f, 1.00f, 0.00f)),
			new ClassDefinition(new Color(0.00f, 1.00f, 1.00f)),
			new ClassDefinition(new Color(1.00f, 0.00f, 1.00f)),
			new ClassDefinition(new Color(1.00f, 0.71f, 0.76f)),
		};
		ObjectsMovement _movement;

		private void Awake()
		{
			_dataProvider.Awake(_bounds, _classes);
			_movement = new ObjectsMovement(_bounds, _dataProvider);
		}

		private void Update()
		{
			_movement.Update();
		}

		private void OnDrawGizmosSelected()
		{
			if (!Application.isPlaying)
			{
				return;
			}

			if (_dataProvider == null)
			{
				return;
			}

			for (uint i = 0; i < _dataProvider.Count; i++)
			{
				_dataProvider.GetData(i, out var position, out var radius, out _, out _, out var classIndex);

				var color = _classes[classIndex].color;
				Gizmos.color = color;
				Gizmos.DrawSphere(position, radius);
			}
		}

		[CustomEditor(typeof(VisualizationSystem))]
		class VisualizationSystemEditor : Editor
		{
			private bool _showInfo;
			private float _infoDistance = 5;

			private readonly GUIContent _textContent = new GUIContent();
			private readonly StringBuilder _infoBuilder = new StringBuilder();
			private GUIStyle _labelStyle;

			private void OnEnable()
			{
				_labelStyle = new GUIStyle(EditorStyles.label);
			}

			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();
				_showInfo = EditorGUILayout.Toggle("Show info", _showInfo);
				if (_showInfo)
				{
					_infoDistance = EditorGUILayout.Slider("Info distance", _infoDistance, 0, 50);
				}
			}

			private void OnSceneGUI()
			{
				if (!Application.isPlaying)
				{
					return;
				}
				var system = (VisualizationSystem)target;
				if (system._dataProvider == null)
				{
					return;
				}

				var cameraTransform = SceneView.lastActiveSceneView.camera.transform;
				var cameraPosition = (float3)cameraTransform.position;
				var cameraForward = (float3)cameraTransform.forward;
				var infoDistanceSq = _infoDistance * _infoDistance;

				for (uint i = 0; i < system._dataProvider.Count; i++)
				{
					system._dataProvider.GetData(i, out var position, out _, out var band, out var changed, out var classIndex);

					if (!_showInfo || math.distancesq(cameraPosition, position) > infoDistanceSq || math.dot(cameraForward, position - cameraPosition) < 0.1f)
					{
						continue;
					}

					var color = system._classes[classIndex].color;
					Handles.color = color;

					_infoBuilder.Append("I");
					_infoBuilder.Append(i);
					_infoBuilder.Append("/CI");
					_infoBuilder.Append(classIndex);
					_infoBuilder.AppendLine();
					_infoBuilder.Append("B");
					_infoBuilder.Append(band);
					_infoBuilder.Append(" ");
					_infoBuilder.Append(changed ? "Changed" : "Same");
					DrawSceneLabel(position, _infoBuilder.ToString(), color);
					_infoBuilder.Clear();
				}
			}

			void DrawSceneLabel(Vector3 position, string text, Color textColor)
			{
				Handles.BeginGUI();
				_textContent.text = text;
				var size = _labelStyle.CalcSize(_textContent);
				var pos = HandleUtility.WorldToGUIPoint(position);
				var rect = new Rect(pos.x, pos.y-size.y/2, size.x, size.y);
				EditorGUI.DrawRect(rect, Color.black);
				Handles.EndGUI();
				_labelStyle.normal.textColor = textColor;
				Handles.Label(position, _textContent, _labelStyle);
			}
		}
	}
}
