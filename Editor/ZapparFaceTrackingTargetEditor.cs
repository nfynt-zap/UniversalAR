using UnityEngine;
using UnityEditor;

namespace Zappar.Editor
{
    [CustomEditor(typeof(ZapparFaceTrackingTarget))]
    [DisallowMultipleComponent]
    public class ZapparFaceTrackingTargetEditor : UnityEditor.Editor
    {
        ZapparFaceTrackingTarget myScript = null;
        int maxTrackerAllowed = 1;
        GUIContent m_idGui;
        
        private void OnEnable()
        {
            if (Application.isPlaying) return;

            var settings = AssetDatabase.LoadAssetAtPath<ZapparUARSettings>(ZapparUARSettings.MySettingsPath);
            myScript = (ZapparFaceTrackingTarget)target;
            maxTrackerAllowed = settings.ConcurrentFaceTrackerCount;

            m_idGui = new GUIContent("Face Number", "Unique id for face tracker [0-" + (maxTrackerAllowed - 1) + "]");
            
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (Application.isPlaying) return;

            int id = EditorGUILayout.IntField(m_idGui, myScript.FaceTrackingId);
            if (id < 0 || id > maxTrackerAllowed) { Debug.Log("Please update UAR settings to fit the range!"); }
            else if (id != myScript.FaceTrackingId) { myScript.FaceTrackingId = id; EditorUtility.SetDirty(myScript.gameObject); }
        }
    }
}