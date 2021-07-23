using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using System.IO;
using System.Runtime.InteropServices;

namespace Zappar.Editor
{
    [Serializable]
    public class ZImageTrainerParams
    {
        public string imgPath = "";
        public int maxWidth = 512;
        public int maxHeight = 512;
        public bool allowOverwrite = false;
        public bool encodePreviewImage = true;
    }

    public class ZImageTrainer : EditorWindow
    {
        public static ZImageTrainerParams myParams = null;
        private string zptPath;

        [MenuItem("Zappar/Editor/Open Image Trainer", false, 1)]
        private static void InitializeTrainerWindows()
        {
            ZImageTrainer window = (ZImageTrainer)EditorWindow.GetWindow(typeof(ZImageTrainer));
            window.titleContent = new GUIContent() { text = "Zappar Image Trainer" };

            if(myParams==null)
            {
                myParams = new ZImageTrainerParams();
            }

            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Image Trainer Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            myParams.imgPath = EditorGUILayout.TextField("Source image path", myParams.imgPath);
            if(GUILayout.Button("...",GUILayout.Width(20)))
            {
                string dir = myParams.imgPath.Length > 0 ? Path.GetDirectoryName(myParams.imgPath) : Application.dataPath;
                string path = EditorUtility.OpenFilePanel("Select an image", dir, "png,jpg,jpeg");
                if (path.Length>0)
                {
                    myParams.imgPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();
            myParams.maxWidth = EditorGUILayout.IntField("Max Width", myParams.maxWidth);
            myParams.maxHeight = EditorGUILayout.IntField("Max Height", myParams.maxHeight);
            myParams.allowOverwrite = EditorGUILayout.Toggle("Allow ZPT overwrite", myParams.allowOverwrite);
            myParams.encodePreviewImage = EditorGUILayout.Toggle("Encode image preview in ZPT", myParams.encodePreviewImage);

            if (GUILayout.Button("Start train process"))
            {
                EditorCoroutineUtility.StartCoroutine(StartTraining(), this);
            }
        }

        IEnumerator StartTraining()
        {
            if(string.IsNullOrEmpty(myParams.imgPath) || !File.Exists(myParams.imgPath))
            {
                Debug.LogError("Invalid image path: " + myParams.imgPath);
                yield break;
            }

            zptPath = Application.streamingAssetsPath;

            yield return new WaitForEndOfFrame();

            byte[] data = File.ReadAllBytes(myParams.imgPath);
            Z.FileData src = new Z.FileData();
            Z.FileData zpt = new Z.FileData();
            Z.FileData preview = new Z.FileData();
            Debug.Log("source image data len: " + data.Length);

            src.length = data.Length;
            src.data = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(byte)) * data.Length);
            Marshal.Copy(data, 0, src.data, data.Length);

            Debug.Log("Starting image train process");

            yield return new WaitForEndOfFrame();

            if (myParams.encodePreviewImage)
            {
                preview.data = src.data;
                preview.length = src.length;
            }

            if (Z.TrainImageCompressedWithMax(ref src, ref zpt, ref preview, FileIsJpg(myParams.imgPath) ? 1 : 0, myParams.maxWidth, myParams.maxHeight) == 1)
            {
                Debug.Log("Success in training image");

                yield return new WaitForEndOfFrame();

                if (zpt.data != null && zpt.length > 0)
                {
                    byte[] zdata = new byte[zpt.length];
                    Marshal.Copy(zpt.data, zdata, 0, zpt.length);
                    string filename = Path.GetFileNameWithoutExtension(myParams.imgPath) + ".zpt";
                    string zptFile = Path.Combine(zptPath, filename);
                    if (myParams.allowOverwrite || !File.Exists(zptFile))
                    {
                        File.WriteAllBytes(zptFile, zdata);
                        Debug.Log("zpt saved at: " + zptFile);
                    }
                    else
                    {
                        Debug.LogError("File already exists! Please enable overwrite option if you wish to replace the older version.\n" + zptFile);
                    }
                }
                else
                {
                    Debug.Log("No zpt file generated!");
                }

            }
            else
            {
                Debug.LogError("Failed to train image");
            }

            yield return new WaitForEndOfFrame();

            if (zpt.data != null && zpt.length > 0)
                Z.TrainImageFreeFileData(ref zpt);

            Marshal.FreeHGlobal(src.data);

            Debug.Log("Finished image train process");
        }

        private static bool FileIsJpg(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            return (ext == "jpg") || (ext == "jpeg");
        }
    }
}