﻿using System;
using UnityEngine;

namespace Zappar
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    internal class ZapparFaceDepthMask : ZapparFaceMesh
    {
        public Material faceMaterial;

        void Start()
        {
            InitCoeffs();

            if (ZapparCamera.Instance != null)
                ZapparCamera.Instance.RegisterCameraListener(this);
        }

        public void OnEnable()
        {
            if (faceMaterial != null)
                gameObject.GetComponent<MeshRenderer>().sharedMaterial = faceMaterial;
            if (faceTracker == null)
                faceTracker = GetComponentInParent<ZapparFaceTrackingTarget>();
        }

        public override void UpdateMaterial()
        {
            if (faceMaterial != null)
                gameObject.GetComponent<MeshRenderer>().sharedMaterial = faceMaterial;
        }
    }
}