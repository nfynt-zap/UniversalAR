using System;
using UnityEngine;
using UnityEngine.Events;

namespace Zappar
{
    internal static class ZapparFaceTrackingManager
    {
        public static int NumberOfTrackers { get; private set; } = 0;
        public static IntPtr FaceTrackerPipeline = IntPtr.Zero;
        public static bool HasInitialized = false;

        public static void RegisterTracker(ZapparFaceTrackingTarget target)
        {
            NumberOfTrackers++;
        }
    }

    public class ZapparFaceTrackingTarget : ZapparTrackingTarget, ZapparCamera.ICameraListener
    {
        public UnityEvent m_OnSeenEvent;
        public UnityEvent m_OnNotSeenEvent;

        [SerializeField, HideInInspector]
        private int m_faceNumber = 0;
        private bool m_hasInitialised = false;
        private bool m_isMirrored;
        private bool m_isVisible = false;

        public IntPtr FaceTrackingPipeline => ZapparFaceTrackingManager.FaceTrackerPipeline;
        public int FaceTrackingId
        {
            get { return m_faceNumber; }
            set { m_faceNumber = (value < 0 ? 0 : value); }
        }

        void Start()
        {
            ZapparFaceTrackingManager.RegisterTracker(this);

            if (m_OnSeenEvent == null)
                m_OnSeenEvent = new UnityEvent();

            if (m_OnNotSeenEvent == null)
                m_OnNotSeenEvent = new UnityEvent();

            ZapparCamera.Instance.RegisterCameraListener(this);
        }

        public void OnZapparInitialised(IntPtr pipeline)
        {
            if (!ZapparFaceTrackingManager.HasInitialized)
            {
                ZapparFaceTrackingManager.FaceTrackerPipeline = Z.FaceTrackerCreate(pipeline);
                Z.FaceTrackerMaxFacesSet(ZapparFaceTrackingManager.FaceTrackerPipeline, ZapparFaceTrackingManager.NumberOfTrackers);
                
#if UNITY_EDITOR
                byte[] faceTrackerData = Z.LoadRawBytes(Z.FaceTrackingModelPath());
                Z.FaceTrackerModelLoadFromMemory(ZapparFaceTrackingManager.FaceTrackerPipeline, faceTrackerData);
#else
                Z.FaceTrackerModelLoadDefault(ZapparFaceTrackingManager.FaceTrackerPipeline);
#endif
                ZapparFaceTrackingManager.HasInitialized = true;
            }

            m_hasInitialised = true;
        }

        public void OnMirroringUpdate(bool mirrored)
        {
            m_isMirrored = mirrored;
        }

        void UpdateTargetPose()
        {
            Matrix4x4 cameraPose = ZapparCamera.Instance.GetPose();
            Matrix4x4 facePose = Z.FaceTrackerAnchorPose(FaceTrackingPipeline, m_faceNumber, cameraPose, m_isMirrored);
            Matrix4x4 targetPose = Z.ConvertToUnityPose(facePose);

            transform.localPosition = Z.GetPosition(targetPose);
            transform.localRotation = Z.GetRotation(targetPose);
            transform.localScale = Z.GetScale(targetPose);
        }

        void Update()
        {
            if (!m_hasInitialised)
            {
                return;
            }
            if (Z.FaceTrackerAnchorCount(FaceTrackingPipeline) > m_faceNumber)
            {
                if (!m_isVisible)
                {
                    m_isVisible = true;
                    m_OnSeenEvent.Invoke();
                }
                UpdateTargetPose();
            }
            else
            {
                if (m_isVisible)
                {
                    m_isVisible = false;
                    m_OnNotSeenEvent.Invoke();
                }
            }
        }

        void OnDestroy()
        {
            if (m_hasInitialised && m_faceNumber==0)
            {
                //Destroy face tracking pipeline while destroying last tracker
                if (ZapparFaceTrackingManager.FaceTrackerPipeline != IntPtr.Zero)
                {
                    Z.FaceTrackerDestroy(ZapparFaceTrackingManager.FaceTrackerPipeline);
                    ZapparFaceTrackingManager.FaceTrackerPipeline = IntPtr.Zero;
                }
                ZapparFaceTrackingManager.HasInitialized = false;
            }
            m_hasInitialised = false;
        }

        public override Matrix4x4 AnchorPoseCameraRelative()
        {
            if (Z.FaceTrackerAnchorCount(FaceTrackingPipeline) > m_faceNumber)
            {
                return Z.FaceTrackerAnchorPoseCameraRelative(FaceTrackingPipeline, m_faceNumber, m_isMirrored);
            }
            return Matrix4x4.identity;
        }
    }
}