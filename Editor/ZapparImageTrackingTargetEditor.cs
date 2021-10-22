using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;

namespace Zappar.Editor
{
    [CustomEditor(typeof(ZapparImageTrackingTarget))]
    public class ZapparImageTrackingTargetEditor : UnityEditor.Editor
    {
        class Styles
        {
            public static GUIContent TargetContent = new GUIContent("Target", "Select the ZPT file you would like to track");
            public static GUIContent OrientationContent = new GUIContent("Orientation", "During play offset the tracker's rotation accordingly");
        }

        private string m_imgTarget;
        private int m_targetIndx;
        private ZapparImageTrackingTarget.PlaneOrientation m_orient;

        ZapparImageTrackingTarget myScript = null;
        private bool m_imgPreviewEnabled;
        List<string> zptFiles = new List<string>();

        private void OnEnable()
        {
            if (Application.isPlaying) return;
            
            var settings = AssetDatabase.LoadAssetAtPath<ZapparUARSettings>(ZapparUARSettings.MySettingsPath);
            m_imgPreviewEnabled = settings.ImageTargetPreviewEnabled;
            myScript = (ZapparImageTrackingTarget)target;
            
            UpdateZptList();

            m_imgTarget = myScript.Target;
            m_targetIndx = Mathf.Max(zptFiles.IndexOf(m_imgTarget), 0);
            m_orient = myScript.Orientation;

            ToggleImagePreview(m_imgPreviewEnabled);
        }

        public override void OnInspectorGUI()
        {
            if (!Application.isPlaying)
            {
                if (zptFiles?.Count > 0)
                {
                    m_targetIndx = Mathf.Max(zptFiles.IndexOf(m_imgTarget), 0);
                    int index = EditorGUILayout.Popup(Styles.TargetContent, m_targetIndx, zptFiles.ToArray());
                    if(index!= m_targetIndx)
                    {
                        m_imgTarget = zptFiles[index];
                        m_targetIndx = index;
                        OnZPTFilenameChange(m_imgTarget); 
                        EditorUtility.SetDirty(myScript.gameObject);
                    }                    
                }
                else
                {
                    EditorGUILayout.LabelField("<color=#CC0011>No ZPT files found!</color>", new GUIStyle() { richText=true });
                }

                m_orient = (ZapparImageTrackingTarget.PlaneOrientation)EditorGUILayout.EnumPopup(Styles.OrientationContent, myScript.Orientation);
                if(m_orient != myScript.Orientation)
                {
                    myScript.Orientation = m_orient;
                    if (myScript.m_ImageTracker == IntPtr.Zero)
                        OnZPTFilenameChange(m_imgTarget);
                    else
                        SetupImagePreview();
                    EditorUtility.SetDirty(myScript.gameObject);
                }
            }
            //base.OnInspectorGUI();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnSeenEvent"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnNotSeenEvent"));
            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void ToggleImagePreview(bool enable)
        {
            if (myScript == null) return;

            if (enable)
            {
                if (!Z.HasInitialized() || (myScript.PreviewImagePlane != null))
                    return;

                OnZPTFilenameChange(myScript.Target);
            }
            else
            {
                //clear the preview
                if (myScript.m_ImageTracker != IntPtr.Zero) Z.ImageTrackerDestroy(myScript.m_ImageTracker);
                if (myScript.m_Pipeline != IntPtr.Zero) Z.PipelineDestroy(myScript.m_Pipeline);

                if (myScript.PreviewImagePlane != null)
                {
                    DestroyImmediate(myScript.PreviewImagePlane);
                    EditorUtility.SetDirty(myScript.gameObject);
                }
            }
        }

        private void OnZPTFilenameChange(string newTarget)
        {
            if (myScript==null || !myScript.gameObject.activeInHierarchy)
            {
                Debug.Log("Could not start LoadZPTTarget Coroutine as gameobject is inactive.");
                return;
            }

            if (myScript.Target == "No ZPT files available." || string.IsNullOrEmpty(myScript.Target))
                return;
            myScript.Target = newTarget;

            if (!m_imgPreviewEnabled) return;

            myScript.m_Pipeline = Z.PipelineCreate();
            myScript.m_ImageTracker = Z.ImageTrackerCreate(myScript.m_Pipeline);
            EditorCoroutineUtility.StartCoroutine(Z.LoadZPTTarget(newTarget, TargetDataAvailableCallback), myScript);
        }

        private void SetupImagePreview()
        { 
            if (myScript==null || !m_imgPreviewEnabled) return;
            
            int previewWidth = Z.ImageTrackerTargetPreviewRgbaWidth(myScript.m_ImageTracker, 0);
            int previewHeight = Z.ImageTrackerTargetPreviewRgbaHeight(myScript.m_ImageTracker, 0);

            Debug.Log("Preview image res: " + previewWidth + "x" + previewHeight);

            if (previewWidth == 0 || previewHeight == 0)
                return;

            if (myScript.PreviewImagePlane == null)
            {
                GameObject plane = null;
                for (int i = 0; i < myScript.transform.childCount; ++i)
                {
                    if (myScript.transform.GetChild(i).gameObject.name == "Preview Image")
                    {
                        plane = myScript.transform.GetChild(i).gameObject;
                        if (plane.GetComponent<MeshFilter>() == null) plane = null;
                    }
                }
                if (plane == null)
                {
                    myScript.PreviewImagePlane = GameObject.CreatePrimitive(PrimitiveType.Quad) as GameObject;
                    Undo.RegisterCreatedObjectUndo(myScript.PreviewImagePlane, "New preview object");
                    myScript.PreviewImagePlane.name = "Preview Image";
                    myScript.PreviewImagePlane.transform.SetParent(myScript.transform);
                }
                else
                {
                    myScript.PreviewImagePlane = plane;
                    EditorUtility.SetDirty(myScript.gameObject);
                }
            }

            myScript.PreviewImagePlane.transform.localEulerAngles = myScript.Orientation == ZapparImageTrackingTarget.PlaneOrientation.Flat ?
                new Vector3(90, 0, 180) : new Vector3(0,0,180);
            myScript.PreviewImagePlane.transform.localPosition = Vector3.zero;

            float aspectRatio = (float)previewWidth / (float)previewHeight;
            const float scaleFactor = 2f;   // check for better estimator than rough scaling
            myScript.PreviewImagePlane.transform.localScale = new Vector3(aspectRatio, 1.0f, 1.0f) * scaleFactor;

            byte[] previewData = Z.ImageTrackerTargetPreviewRgba(myScript.m_ImageTracker, 0);

            Texture2D texture = new Texture2D(previewWidth, previewHeight, TextureFormat.RGBA32, false);
            texture.LoadRawTextureData(previewData);
            texture.Apply();

            Material material = new Material(Shader.Find("Unlit/Texture"));
#if ZAPPAR_SRP
            //material.SetTextureScale("_MainTex", new Vector2(-1, 1));
            material.mainTexture = texture;
            Vector3 scale = myScript.PreviewImagePlane.transform.localScale;
            myScript.PreviewImagePlane.transform.localScale = new Vector3(scale.x * -1, scale.y, scale.z);
#else
            material.SetTextureScale("_MainTex", new Vector2(-1, 1));
            material.mainTexture = texture;
#endif
            myScript.PreviewImagePlane.GetComponent<Renderer>().material = material;
        }

        private void TargetDataAvailableCallback(byte[] data)
        {
            if (myScript.m_ImageTracker!=IntPtr.Zero)
            {
                Z.ImageTrackerTargetLoadFromMemory(myScript.m_ImageTracker, data);
                SetupImagePreview();
            }
            else
            {
                Debug.LogError("No image tracker found to enable preview");
            }
        }

        private void UpdateZptList()
        {
            zptFiles.Clear();
            try
            {
                DirectoryInfo directory = new DirectoryInfo(Application.streamingAssetsPath);
                FileInfo[] files = directory.GetFiles("*.zpt");
                foreach (FileInfo file in files)
                {
                    zptFiles.Add(file.Name);
                }
            }
            catch (Exception e)
            {
                // Unable to check streaming assets path
                Debug.LogError("Unable to check streaming assets path! Exception: " + e.Message);
            }
        }

    }
}