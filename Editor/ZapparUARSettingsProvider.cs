using System.IO;
using UnityEngine;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
namespace Zappar.Editor
{
    static class ZapparUARSettingsProvider
    {
        class Styles
        {
            public static GUIContent ImageTargetPreview = new GUIContent("Enable Image Tracker Preview", "Add image preview of target in editor");
            public static GUIContent ConcurrentFaceTracker = new GUIContent("Concurrent Face Trackers", "Number of faces to track at the same time");
            public static GUIContent RealtimeReflections = new GUIContent("Enable Realtime Reflection", "Use ZCV camera source for realtime reflection");
            public static GUIContent DebugMode = new GUIContent("ZCV debug mode", "write logs to editor or to a file ");
            public static GUIContent LogLevel = new GUIContent("ZCV log level", "Log levels");
            public static GUIStyle Heading1 = new GUIStyle() { richText=true, fontStyle = FontStyle.Bold, fontSize = (int)(EditorGUIUtility.singleLineHeight * 0.85f) };
            public static Color Background = new Color(1f, 1f, 1f, 0.05f);
        }

        struct Constants
        {
            public const string ImagePreviewProp = "m_EnableImageTargetPreview";
            public const string FaceTrackerProp = "m_ConcurrentFaceTrackerCount";
            public const string RealtimeReflectionProp = "m_EnableRealtimeReflections";
            public const string DebugModeProp = "m_DebugMode";
            public const string LogLevelProp = "m_LogLevel";
        }

        public static void GUIHandler(string searchContext, SerializedObject settings)
        {
            settings.Update();

            PackageInfo info = PackageInfo.FindForAssetPath("Packages/com.zappar.uar/package.json");

            EditorGUILayout.HelpBox("Version: " + info.version,MessageType.Info);

            EditorGUILayout.Space(10);
            GUILayout.Label("<color=#CCCCCC>Runtime Settings</color>", Styles.Heading1);
            float labelWidth = GUILayoutUtility.GetLastRect().width;
            Rect runRect = EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
            EditorGUI.BeginChangeCheck();
            EditorGUIUtility.labelWidth = labelWidth / 2;
            EditorGUILayout.PropertyField(settings.FindProperty(Constants.FaceTrackerProp), Styles.ConcurrentFaceTracker);
            if(EditorGUI.EndChangeCheck())
            {
                int val = settings.FindProperty(Constants.FaceTrackerProp).intValue;
                if (val < 1 || val > 10)
                {
                    Debug.Log("An ideal range for tracker would be [1-5]");
                    if (val < 1) settings.FindProperty(Constants.FaceTrackerProp).intValue = 1;
                }
            }
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(settings.FindProperty(Constants.RealtimeReflectionProp), Styles.RealtimeReflections);
            if (EditorGUI.EndChangeCheck())
            {
                bool add = settings.FindProperty(Constants.RealtimeReflectionProp).boolValue;
                if (add) ZapparUtilities.CreateLayer(ZapparReflectionProbe.ReflectionLayer); else ZapparUtilities.RemoveLayer(ZapparReflectionProbe.ReflectionLayer);
                if (add && !QualitySettings.realtimeReflectionProbes)
                {
                    Debug.LogError("Please enable Realtime reflections from project Quality settings as well!");
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUI.DrawRect(runRect, Styles.Background);
            //GUI.Box(runRect, GUIContent.none);

            EditorGUILayout.Space(15);

            GUILayout.Label("<color=#CCCCCC>Editor Settings</color>", Styles.Heading1);
            Rect edRect = EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
            EditorGUILayout.PropertyField(settings.FindProperty(Constants.ImagePreviewProp), Styles.ImageTargetPreview);
            if (!settings.FindProperty(Constants.ImagePreviewProp).boolValue)
            {
                Rect rect = EditorGUILayout.BeginVertical();
                EditorGUILayout.TextArea("<color=white>Note: Already added preview images would still be present unless removed manually</color>", new GUIStyle() { fontSize = (int)EditorGUIUtility.singleLineHeight/2, wordWrap = true, richText = true });
                EditorGUILayout.EndVertical();
                GUI.Box(rect, GUIContent.none);
            }
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(settings.FindProperty(Constants.DebugModeProp), Styles.DebugMode);
            EditorGUILayout.PropertyField(settings.FindProperty(Constants.LogLevelProp), Styles.LogLevel);

#if ZAPPAR_SRP
            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox("Scriptable Pipeline Enabled for ZCV", MessageType.None, true);
#endif
            EditorGUILayout.EndVertical();
            EditorGUI.DrawRect(edRect, Styles.Background);

            settings.ApplyModifiedProperties();
        }

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            if (!IsSettingsAvailable())
            {
                GetSerializedSettings();
            }

            var provider = new SettingsProvider("Project/ZapparUARSettings", SettingsScope.Project)
            {
                label = "Zappar Universal AR",
                guiHandler = (searchContext) =>
                {
                    var settings = GetSerializedSettings();
                    GUIHandler(searchContext, settings);
                },
                // Automatically extract all keywords from the Styles.
                keywords = SettingsProvider.GetSearchKeywordsFromGUIContentProperties<Styles>()
            };

            return provider;
        }

        private static bool IsSettingsAvailable()
        {
            return File.Exists(ZapparUARSettings.MySettingsPath);
        }


        public static ZapparUARSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ZapparUARSettings>(ZapparUARSettings.MySettingsPath);
            if (settings == null)
            {
                if (Directory.Exists(Path.GetDirectoryName(ZapparUARSettings.MySettingsPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(ZapparUARSettings.MySettingsPath));
                }
                settings = ScriptableObject.CreateInstance<ZapparUARSettings>();
                settings.ImageTargetPreviewEnabled = true;
                settings.ConcurrentFaceTrackerCount = 1;
                settings.EnableRealtimeReflections = false;
                settings.DebugMode = Z.DebugMode.UnityLog;
                settings.LogLevel = Z.LogLevel.WARNING;
                AssetDatabase.CreateAsset(settings, ZapparUARSettings.MySettingsPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        private static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
    }
}