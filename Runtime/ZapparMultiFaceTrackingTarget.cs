using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zappar
{
    public class ZapparMultiFaceTrackingTarget : ZapparTrackingTarget, ZapparCamera.ICameraListener
    {
        public int NumberOfTrackers => FaceTrackers.Count;
        public bool HasInitialized { get; private set; }
        public bool IsMirrored { get; private set; }

        private IntPtr? m_faceTrackingPipeline = null;

        [SerializeField]
        public List<ZapparFaceTrackingAnchor> FaceTrackers = new List<ZapparFaceTrackingAnchor>();
        private List<bool> m_trackerIsTracked = new List<bool>();

        public IntPtr? FaceTrackerPipeline
        {
            get { return m_faceTrackingPipeline; }
            private set
            {
                m_faceTrackingPipeline = value;
            }
        }

        public void OnZapparInitialised(IntPtr pipeline)
        {
            IntPtr faceTracker = Z.FaceTrackerCreate(pipeline);
            Z.FaceTrackerMaxFacesSet(faceTracker, NumberOfTrackers);

#if UNITY_EDITOR
            byte[] faceTrackerData = Z.LoadRawBytes(Z.FaceTrackingModelPath());
            Z.FaceTrackerModelLoadFromMemory(faceTracker, faceTrackerData);
#else
                Z.FaceTrackerModelLoadDefault(faceTracker);
#endif
            FaceTrackerPipeline = faceTracker;
            HasInitialized = true;

            foreach (var anchor in FaceTrackers)
            {
                anchor?.InitFaceTracker();
                m_trackerIsTracked.Add(false);
            }
        }

        public void OnMirroringUpdate(bool mirrored)
        {
            IsMirrored = mirrored;
        }

        private void Start()
        {
            if (ZapparCamera.Instance != null)
                ZapparCamera.Instance.RegisterCameraListener(this, true);
        }

        private void Update()
        {
            if (!HasInitialized || FaceTrackerPipeline == null)
                return;

            int count = Z.FaceTrackerAnchorCount(FaceTrackerPipeline.Value);

            for (int i = 0; i < count; ++i)
            {
                if (Int32.TryParse(Z.FaceTrackerAnchorId(FaceTrackerPipeline.Value, i), out int id))
                {
                    var anchor = FaceTrackers.Find(ent => ent.FaceTrackerIndex == id);
                    if (anchor != null)
                    {
                        anchor.AnchorId = i;
                        m_trackerIsTracked[id] = true;
                    }
                }
            }

            for (int i = 0; i < NumberOfTrackers; ++i)
            {
                FaceTrackers[i].UpdateAnchor(m_trackerIsTracked[i]);
                m_trackerIsTracked[i] = false;
            }
        }

        private void OnDestroy()
        {
            if(HasInitialized)
            {
                if (FaceTrackerPipeline != null)
                {
                    Z.FaceTrackerDestroy(FaceTrackerPipeline.Value);
                    FaceTrackerPipeline = null;
                }
                HasInitialized = false;
            }
        }

        public void RegisterAnchor(ZapparFaceTrackingAnchor anchor, bool add)
        {
            if (add && !FaceTrackers.Contains(anchor))
            {
                FaceTrackers.Add(anchor);
            }
            else if(!add && FaceTrackers.Contains(anchor))
            {
                FaceTrackers.Remove(anchor);
            }
        }

        public override Matrix4x4 AnchorPoseCameraRelative()
        {
            if (FaceTrackers.Count > 0) 
                return FaceTrackers[0].AnchorPoseCameraRelative();

            return Matrix4x4.identity;
        }

    }
}