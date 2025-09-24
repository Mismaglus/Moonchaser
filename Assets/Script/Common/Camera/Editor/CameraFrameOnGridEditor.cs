#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Game.Common;

[CustomEditor(typeof(CameraFrameOnGrid))]
public class CameraFrameOnGridEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();

        var comp = (CameraFrameOnGrid)target;

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Frame Grid Now", GUILayout.Height(28)))
            {
                if (comp != null)
                {
                    // ��¼�������༭���»���Ч������ʱ���ԣ�
                    if (comp.cam)
                    {
                        Undo.RecordObject(comp.cam.transform, "Frame Grid Camera");
                        Undo.RecordObject(comp.cam, "Frame Grid Camera");
                    }
                    comp.FrameNow();
                    EditorUtility.SetDirty(comp);
                    if (comp.cam) EditorUtility.SetDirty(comp.cam);
                }
            }
        }
    }
}
#endif
