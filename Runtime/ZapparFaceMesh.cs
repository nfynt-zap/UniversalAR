using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zappar
{
    internal abstract class ZapparFaceMesh : MonoBehaviour, ZapparCamera.ICameraListener
    {
        protected IntPtr m_faceMesh = IntPtr.Zero;
        private bool m_hasInitialised = false;

        public bool useDefaultFullHead = true;
        public bool fillEyeLeft;
        public bool fillEyeRight;
        public bool fillMouth;
        public bool fillNeck;

        public ZapparFaceTrackingTarget faceTracker;

        protected Mesh m_mesh = null;
        private bool m_haveInitialisedFaceMesh = false;

        private bool m_isMirrored;

        private IntPtr m_faceTrackingTargetPtr;
        private int m_faceTrackingTargetId;
        private bool m_haveFaceTracker = false;

        public const int m_numIdentityCoefficients = 50;
        public const int m_numExpressionCoefficients = 29;

        private float[] m_identity = null;
        private float[] m_expression = null;
        private float[] m_faceVertices = null;
        private float[] m_faceNormals = null;

        public void InitCoeffs()
        {
            m_identity = m_identity ?? new float[m_numIdentityCoefficients];
            m_expression = m_expression ?? new float[m_numExpressionCoefficients];
            for (int i = 0; i < m_numIdentityCoefficients; ++i) m_identity[i] = 0.0f;
            for (int i = 0; i < m_numExpressionCoefficients; ++i) m_expression[i] = 0.0f;
        }

        public void OnZapparInitialised(IntPtr pipeline)
        {
            if (faceTracker != null)
            {
                m_faceTrackingTargetPtr = faceTracker.FaceTrackingPipeline;
                m_faceTrackingTargetId = faceTracker.FaceTrackingId;
                m_haveFaceTracker = true;
            }
            else
            {
                Debug.Log("Warning: The face mesh will not update its vertices unless a Face Tracker is assigned.");
            }

            m_hasInitialised = true;
            m_haveInitialisedFaceMesh = false;

            if (m_faceMesh == IntPtr.Zero)
            {
                m_faceMesh = Z.FaceMeshCreate();
                CreateMesh();
            }
        }

        public void OnMirroringUpdate(bool mirrored)
        {
            m_isMirrored = mirrored;
            m_haveInitialisedFaceMesh = false;
        }

        protected void CreateMesh(bool force=false)
        {
            if (m_mesh != null && !force)
                return;

            DestroyUnityMesh();
            LoadMeshData();

            m_mesh = new Mesh();
            m_mesh.name = "ZFaceMesh" + (useDefaultFullHead ? "_Full" : "");
            gameObject.GetComponent<MeshFilter>().sharedMesh = m_mesh;

            UpdateMeshData();
            UpdateMaterial();
        }

        public abstract void UpdateMaterial();

        private void LoadMeshData()
        {
#if UNITY_EDITOR
            string filename;
            if (useDefaultFullHead)
                filename = Z.FaceMeshFullHeadSimplifiedModelPath();
            else
                filename = Z.FaceMeshFaceModelPath();
            byte[] data = Z.LoadRawBytes(filename);
            Z.FaceMeshLoadFromMemory(m_faceMesh, data, fillMouth, fillEyeLeft, fillEyeRight, fillNeck);
#else
        if (useDefaultFullHead)
        {
            Z.FaceMeshLoadDefaultFullHeadSimplified(m_faceMesh, fillMouth, fillEyeLeft, fillEyeRight, fillNeck);
        } else {
            if (!fillEyeLeft && !fillEyeRight && !fillMouth) 
            {
                Z.FaceMeshLoadDefault(m_faceMesh);
            }
            else Z.FaceMeshLoadDefaultFace(m_faceMesh, fillEyeLeft, fillEyeRight, fillMouth);
        }
#endif            
        }

        private void UpdateMeshData()
        {
            if (m_faceMesh == null)
                return;

#if !UNITY_EDITOR
        if (!m_haveFaceTracker)
        {
            return;
        }
#endif
            if (!m_haveInitialisedFaceMesh)
            {
                InitCoeffs();
            }
            else
            {
                Z.FaceTrackerAnchorUpdateIdentityCoefficients(m_faceTrackingTargetPtr, m_faceTrackingTargetId, ref m_identity);
                Z.FaceTrackerAnchorUpdateExpressionCoefficients(m_faceTrackingTargetPtr, m_faceTrackingTargetId, ref m_expression);
            }

            Z.FaceMeshUpdate(m_faceMesh, m_identity, m_expression, m_isMirrored);

            if (m_faceVertices==null || m_faceVertices.Length == 0)
            {
                m_faceVertices = new float[Z.FaceMeshVerticesSize(m_faceMesh)];
                m_faceNormals = new float[Z.FaceMeshNormalsSize(m_faceMesh)];
            }

            Z.UpdateFaceMeshVertices(m_faceMesh, ref m_faceVertices);
            Z.UpdateFaceMeshNormals(m_faceMesh, ref m_faceNormals);
            m_mesh.vertices = Z.UpdateFaceMeshVerticesForUnity(m_faceVertices);
            m_mesh.normals = Z.UpdateFaceMeshNormalsForUnity(m_faceNormals);

            if (!m_haveInitialisedFaceMesh)
            {
                m_mesh.triangles = Z.UpdateFaceMeshTrianglesForUnity(Z.FaceMeshIndices(m_faceMesh));
                m_mesh.uv = Z.UpdateFaceMeshUVsForUnity(Z.FaceMeshUvs(m_faceMesh));
                m_haveInitialisedFaceMesh = true;
            }
        }

        void Update()
        {
            if (!m_hasInitialised || m_faceTrackingTargetPtr == IntPtr.Zero || Z.FaceTrackerAnchorCount(m_faceTrackingTargetPtr) <= m_faceTrackingTargetId)
                return;

            UpdateMeshData();
        }

        void OnDestroy()
        {
            if (m_faceMesh != null && Application.isPlaying)
                Z.FaceMeshDestroy(m_faceMesh);

            DestroyUnityMesh();
            m_hasInitialised = false;
        }

        protected void DestroyUnityMesh()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                Destroy(m_mesh);
            else
                DestroyImmediate(m_mesh);
#else
            Destroy(m_mesh);
#endif
            m_mesh = null;
            m_faceVertices = null;
            m_faceNormals = null;
            m_haveInitialisedFaceMesh = false;
        }
    }
}