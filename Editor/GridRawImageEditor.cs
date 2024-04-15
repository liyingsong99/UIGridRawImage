using System.Linq;
using mitaywalle.UI.Packages.GridImage.Runtime;
using UnityEditor;
using UnityEngine;

namespace mitaywalle.UI.Editor
{
	[CustomEditor(typeof(GridRawImage))]
	public class GridRawImageEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.UpdateIfRequiredOrScript();
			EditorGUI.BeginChangeCheck();
			DrawPropertiesExcludingCustom(serializedObject, "m_OnCullStateChanged", "m_RaycastPadding");
			if (EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
			}
		}

		private static void DrawPropertiesExcludingCustom(SerializedObject obj, params string[] propertyToExclude)
		{
			SerializedProperty iterator = obj.GetIterator();
			bool enterChildren = true;
			while (iterator.NextVisible(enterChildren))
			{
				enterChildren = false;
				if (!propertyToExclude.Contains(iterator.name))
				{
					if (iterator.name == "m_Script")
					{
						GUI.enabled = false;
					}
					EditorGUILayout.PropertyField(iterator, true);

					if (iterator.name == "m_Script")
					{
						GUI.enabled = true;
					}
				}
			}
		}
	}
}