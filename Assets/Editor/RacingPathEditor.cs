using OriginalKartRacer.Race;
using UnityEditor;
using UnityEngine;

namespace OriginalKartRacer.EditorTools
{
    /// <summary>在 Scene View 直接拖动路径节点，也可将当前选择物体的坐标追加为节点。</summary>
    [CustomEditor(typeof(RacingPath))]
    public sealed class RacingPathEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            RacingPath path = (RacingPath)target;
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Scene View 中可拖动青色节点。选中场景物体后点 Add Selected，将其位置加入赛道路径。", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Selected"))
                {
                    if (Selection.activeTransform == null)
                    {
                        EditorUtility.DisplayDialog("Racing Path", "请先在 Hierarchy 或 Scene View 中选择一个物体。", "OK");
                        return;
                    }
                    Undo.RecordObject(path, "Add Racing Path Point");
                    path.AddWorldPoint(Selection.activeTransform.position);
                    EditorUtility.SetDirty(path);
                }
                if (GUILayout.Button("Remove Last"))
                {
                    Undo.RecordObject(path, "Remove Racing Path Point");
                    path.RemoveLastPoint();
                    EditorUtility.SetDirty(path);
                }
            }
        }

        private void OnSceneGUI()
        {
            RacingPath path = (RacingPath)target;
            for (int i = 0; i < path.Count; i++)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 moved = Handles.PositionHandle(path.GetPoint(i), Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(path, "Move Racing Path Point");
                    // 该小型编辑器不暴露内部序列化字段给运行时代码，使用 SerializedObject 精确修改节点。
                    SerializedObject serialized = new SerializedObject(path);
                    SerializedProperty points = serialized.FindProperty("localPoints");
                    points.GetArrayElementAtIndex(i).vector3Value = path.transform.InverseTransformPoint(moved);
                    serialized.ApplyModifiedProperties();
                }
                Handles.Label(path.GetPoint(i) + Vector3.up * 0.55f, i.ToString());
            }
        }
    }
}
