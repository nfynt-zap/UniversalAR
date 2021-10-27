using System;
using UnityEngine;

namespace Zappar
{
    public class ZapparInstantTrackingTarget : ZapparTrackingTarget, ZapparCamera.ICameraListener
    {
        public IntPtr? InstantTracker = null;
        private bool m_userHasPlaced = false;
        private bool m_hasInitialised = false;
        private bool m_isMirrored = false;

        void Start()
        {
            if (ZapparCamera.Instance != null)
                ZapparCamera.Instance.RegisterCameraListener(this, true);
        }

        public void OnZapparInitialised(IntPtr pipeline)
        {
            InstantTracker = Z.InstantWorldTrackerCreate(pipeline);
            m_hasInitialised = true;
        }

        public void OnMirroringUpdate(bool mirrored)
        {
            m_isMirrored = mirrored;
        }

        void UpdateTargetPose()
        {
            Matrix4x4 cameraPose = ZapparCamera.Instance.GetCameraPose;
            Matrix4x4 instantTrackerPose = Z.InstantWorldTrackerAnchorPose(InstantTracker.Value, cameraPose, m_isMirrored);
            Matrix4x4 targetPose = Z.ConvertToUnityPose(instantTrackerPose);

            transform.localPosition = Z.GetPosition(targetPose);
            transform.localRotation = Z.GetRotation(targetPose);
            transform.localScale = Z.GetScale(targetPose);
        }

        void Update()
        {
            if (!m_hasInitialised || InstantTracker==null)
            {
                return;
            }

            if (!m_userHasPlaced)
            {
                Z.InstantWorldTrackerAnchorPoseSetFromCameraOffset(InstantTracker.Value, 0, 0, -5, Z.InstantTrackerTransformOrientation.MINUS_Z_AWAY_FROM_USER);
            }

            if (Input.touchCount > 0)
            {
                m_userHasPlaced = true;
            }

            UpdateTargetPose();
        }

        void OnDestroy()
        {
            if (m_hasInitialised)
            {
                if (InstantTracker != null)
                {
                    Z.InstantWorldTrackerDestroy(InstantTracker.Value);
                    InstantTracker = null;
                }
            }
            if (ZapparCamera.Instance != null)
                ZapparCamera.Instance.RegisterCameraListener(this, false);
        }

        public override Matrix4x4 AnchorPoseCameraRelative()
        {
            return Z.InstantWorldTrackerAnchorPoseCameraRelative(InstantTracker.Value, m_isMirrored);
        }
    }
}