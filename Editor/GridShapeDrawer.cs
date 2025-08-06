using mitaywalle.UI.Packages.GridImage.Runtime;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic; // Added for Dictionary

namespace mitaywalle.UI.Packages.GridImage.Editor
{
	[CustomPropertyDrawer(typeof(GridShape))]
	public class GridShapeDrawer : PropertyDrawer
	{
		// 添加缓存机制
		private static readonly Dictionary<int, (Vector2Int, uint)[,]> _indexCache = new Dictionary<int, (Vector2Int, uint)[,]>();
		private static readonly Dictionary<int, int> _lastSizeCache = new Dictionary<int, int>();

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => 0;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUILayout.PropertyField(property);

			//property.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(property.isExpanded, label);
			if (property.isExpanded)
			{
				EditorGUI.indentLevel += 2;
				SerializedProperty size = property.FindPropertyRelative("_size");
				SerializedProperty prop = property.FindPropertyRelative("_bitArray");
				// EditorGUILayout.PropertyField(property.FindPropertyRelative("_readable"));
				// EditorGUILayout.PropertyField(size);

				EditorGUI.BeginChangeCheck();

				BitArray256 bitArray = (BitArray256)prop.boxedValue;
				float last = EditorGUIUtility.labelWidth;
				EditorGUIUtility.labelWidth = 0;

				// 修复：使用serializedObject.targetObject.GetInstanceID()替代property.GetInstanceID()
				int instanceId = property.serializedObject.targetObject.GetInstanceID();
				(Vector2Int, uint)[,] cachedValues = GetCachedIndexValues(size.vector2IntValue, instanceId);

				for (int j = 0; j < size.vector2IntValue.y; j++)
				{
					GUILayout.BeginHorizontal();

					for (int i = 0; i < size.vector2IntValue.x; i++)
					{
						var (_, index) = cachedValues[i, j];
						bool value = bitArray[index];
						bitArray[index] = EditorGUILayout.Toggle(GUIContent.none, value, GUILayout.Width(20));
					}
					GUILayout.EndHorizontal();
				}

				EditorGUIUtility.labelWidth = last;
				if (EditorGUI.EndChangeCheck())
				{
					foreach (Object targetObject in property.serializedObject.targetObjects)
					{
						Undo.RecordObject(targetObject, "GridShape flags");
						prop.boxedValue = bitArray;
					}
				}
				EditorGUI.indentLevel -= 2;
			}

			EditorGUILayout.EndFoldoutHeaderGroup();
		}

		// 添加缓存方法
		private (Vector2Int, uint)[,] GetCachedIndexValues(Vector2Int size, int instanceId)
		{
			int sizeHash = size.x * 1000 + size.y;

			// 检查是否需要重新计算
			if (!_indexCache.ContainsKey(instanceId) ||
				!_lastSizeCache.ContainsKey(instanceId) ||
				_lastSizeCache[instanceId] != sizeHash)
			{
				// 重新计算索引
				var newValues = new (Vector2Int, uint)[size.x, size.y];

				for (int y = 0; y < size.y; y++)
				{
					for (int x = 0; x < size.x; x++)
					{
						Vector2Int position = new(x, size.y - y - 1); // 反转Y轴
						uint index = (uint)(position.y * size.x + position.x);
						newValues[x, y] = (position, index);
					}
				}

				_indexCache[instanceId] = newValues;
				_lastSizeCache[instanceId] = sizeHash;
			}

			return _indexCache[instanceId];
		}
	}
}
