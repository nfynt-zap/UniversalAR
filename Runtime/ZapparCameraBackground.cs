using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Zappar
{
    public class ZapparCameraBackground : MonoBehaviour
    {
        private Material m_CameraMaterial=null;

        private bool m_Initialised = false;
        private Texture2D m_CamTexture = null;

        public Texture2D GetCameraTexture => m_CamTexture;

        private void Awake()
        {
            m_CameraMaterial = new Material(Shader.Find("Zappar/CameraBackgroundShader"));
            if(m_CameraMaterial==null)
            {
                Debug.LogError("Can't render camera texture: Missing Zappar/CameraBackgroundShader!");
            }
        }

        void Point(float x, float y)
        {
            GL.TexCoord2(x, y);
            GL.Vertex3(x, y, -1);
        }

#if ZAPPAR_SRP
        private void Start()
        {
            RenderPipelineManager.endCameraRendering += RenderPipelineManager_endCameraRendering;
        }

        private void OnDestroy()
        {
            RenderPipelineManager.endCameraRendering -= RenderPipelineManager_endCameraRendering;
        }

        private void RenderPipelineManager_endCameraRendering(ScriptableRenderContext arg1, Camera arg2)
        {
            if (arg2.depth != -1)
                return;
            m_CameraMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.LoadProjectionMatrix(Matrix4x4.Ortho(0, 1, 0, 1, 0, 1));
            GL.Begin(GL.QUADS);
            Point(0, 0);
            Point(0, 1);
            Point(1, 1);
            Point(1, 0);
            GL.End();
            GL.PopMatrix();
        }
#else
        void OnPostRender()
        {
            m_CameraMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.LoadProjectionMatrix(Matrix4x4.Ortho(0, 1, 0, 1, 0, 1));
            GL.Begin(GL.QUADS);
            Point(0, 0);
            Point(0, 1);
            Point(1, 1);
            Point(1, 0);
            GL.End();
            GL.PopMatrix();
        }

#endif
        void Update()
        {
            if (!m_Initialised || m_CameraMaterial == null)
            {
                if (Z.HasInitialized())
                    m_Initialised = true;
                return;
            }

            GetComponent<Camera>().projectionMatrix = Z.PipelineProjectionMatrix(ZapparCamera.Instance.GetPipeline, Screen.width, Screen.height);

            Matrix4x4 textureMatrix = Z.PipelineCameraFrameTextureMatrix(ZapparCamera.Instance.GetPipeline, Screen.width, Screen.height, ZapparCamera.Instance.IsMirrored);
            m_CameraMaterial.SetMatrix("_nativeTextureMatrix", textureMatrix);

            m_CamTexture = Z.PipelineCameraFrameTexture(ZapparCamera.Instance.GetPipeline);
            if (m_CamTexture != null)
                m_CameraMaterial.mainTexture = m_CamTexture;

        }
    }
}