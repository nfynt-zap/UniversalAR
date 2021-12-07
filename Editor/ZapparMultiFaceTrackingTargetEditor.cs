using UnityEditor;
using UnityEngine;

namespace Zappar.Editor
{
    [CustomEditor(typeof(ZapparMultiFaceTrackingTarget))]
    [DisallowMultipleComponent]
    public class ZapparMultiFaceTrackingTargetEditor : UnityEditor.Editor
    {
        private ZapparMultiFaceTrackingTarget m_target = null;
        private ZapparUARSettings m_settings = null;

        class Styles
        {
            public static GUIContent TrackerCount = new GUIContent("Trackers count", "Number of face tracking anchors");
            public static GUIContent AddTracker = new GUIContent("Add New Tracker", "Add new face tracking anchor for this target");
            public static GUIContent RemoveTracker = new GUIContent("Remove Last Tracker", "Remove last face tracking anchor for this target");
            public static GUIStyle Heading1 = new GUIStyle() { richText = true, fontStyle = FontStyle.Bold, fontSize = (int)(EditorGUIUtility.singleLineHeight * 0.85f) };
            public static GUIStyle NormalText = new GUIStyle() { richText = true };
        }

        public void OnEnable()
        {
            if (Application.isPlaying) return;

            m_settings = AssetDatabase.LoadAssetAtPath<ZapparUARSettings>(ZapparUARSettings.MySettingsPathInPackage);
            if (m_settings == null)
            {
                Debug.LogError("UAR Settings not found!");
                return;
            }
            ValidateTrackersList();
        }

        public override void OnInspectorGUI()
        {
            m_target = (ZapparMultiFaceTrackingTarget)target;

            EditorGUILayout.TextField(Styles.TrackerCount, "<color=#CCCCCC>" + m_target.NumberOfTrackers.ToString() + "</color>", Styles.NormalText);

            if (Application.isPlaying) return;

            EditorGUILayout.BeginHorizontal(new GUILayoutOption[] { GUILayout.ExpandWidth(true) });

            EditorGUI.BeginDisabledGroup(m_settings.ConcurrentFaceTrackerCount <= m_target.NumberOfTrackers);            
            if (GUILayout.Button(Styles.AddTracker))
            {
                //Debug.Log("Adding new anchor");
                GameObject go = ZAssistant.GetZapparFaceTrackingAnchor();
                ZapparFaceTrackingAnchor anchor = go.GetComponent<ZapparFaceTrackingAnchor>();
                go.GetComponentInChildren<ZapparFaceDepthMask>().FaceTrackingAnchor = anchor;
                anchor.FaceTrackingTarget = m_target;
                anchor.FaceTrackerIndex = m_target.NumberOfTrackers;
                m_target.RegisterAnchor(anchor, true);
                go.transform.name += " "+anchor.FaceTrackerIndex.ToString();
                go.transform.SetParent(m_target.transform);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(m_target.NumberOfTrackers == 0);
            if(GUILayout.Button(Styles.RemoveTracker))
            {
                //Debug.Log("Removing anchor");
                ZapparFaceTrackingAnchor lAnchor = m_target.FaceTrackers[m_target.NumberOfTrackers - 1];
                m_target.RegisterAnchor(lAnchor, false);
                DestroyImmediate(lAnchor.gameObject);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("FaceTrackers"), new GUIContent("Trackers list"), true);
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }

        private void ValidateTrackersList()
        {
            ZapparMultiFaceTrackingTarget faceTarget = (ZapparMultiFaceTrackingTarget)target;
            if (faceTarget.FaceTrackers.RemoveAll(ent => ent == null) > 0)
            {
                int i = 0;
                foreach (var anchor in faceTarget.FaceTrackers)
                {
                    anchor.FaceTrackerIndex = i++;
                }
            }
        }
    }
}