using System;
using UnityEngine;
using Zappar;

public class ZapparFaceLandmark : MonoBehaviour
{
    public enum Face_Landmark_Name {
        LeftEye = 0,
        RightEye,
        LeftEar,
        RightEar,
        NoseBridge,
        NoseTip,
        NoseBase,
        LipTop,
        LipBottom,
        MouthCenter,
        Chin,
        LeftEyebrow,
        RightEyebrow
    };
    
    public ZapparFaceTrackingTarget FaceTracker;

    public Face_Landmark_Name LandmarkName;
    
    private Face_Landmark_Name m_currentLandmark;
    private bool m_isMirrored;
    private IntPtr m_faceTrackerPipeline;
    private int m_faceTrackerId;
    private IntPtr m_faceLandmarkPtr = IntPtr.Zero;

    private const int NumIdentityCoefficients = 50;
    private const int NumExpressionCoefficients = 29;

    private float[] m_identity;
    private float[] m_expression;

    void Start()
    {
        ZapparFaceTrackingManager.RegisterPipelineCallback(OnFaceTrackingPipelineInitialised);
    }

    void Update()
    {
        if (m_faceLandmarkPtr == IntPtr.Zero) return;

        if (LandmarkName != m_currentLandmark)
            InitFaceLandmark();

        Z.FaceTrackerAnchorUpdateIdentityCoefficients(m_faceTrackerPipeline, m_faceTrackerId, ref m_identity);
        Z.FaceTrackerAnchorUpdateExpressionCoefficients(m_faceTrackerPipeline, m_faceTrackerId, ref m_expression);

        Z.FaceLandmarkUpdate(m_faceLandmarkPtr, m_identity, m_expression, m_isMirrored);

        var matrix = Z.ConvertToUnityPose(Z.FaceLandmarkAnchorPose(m_faceLandmarkPtr));
        transform.localPosition = Z.GetPosition(matrix);
        transform.localRotation = Z.GetRotation(matrix);
    }
    
    void OnDestroy()
    {
        if(m_faceLandmarkPtr != IntPtr.Zero)
            Z.FaceLandmarkDestroy(m_faceLandmarkPtr);
        ZapparFaceTrackingManager.DeRegisterPipelineCallback(OnFaceTrackingPipelineInitialised);
    }

    public void OnFaceTrackingPipelineInitialised(IntPtr pipeline, bool mirrored)
    {
        if (FaceTracker == null)
        {
            Debug.Log("Warning: The face landmark will not work unless a Face Tracker is assigned.");
            return;
        }

        InitFaceLandmark();
        m_faceTrackerPipeline = pipeline;
        m_faceTrackerId = FaceTracker.FaceTrackingId;
        m_isMirrored = mirrored;
    }

    void InitFaceLandmark()
    {
        if (m_faceLandmarkPtr != IntPtr.Zero)
            Z.FaceLandmarkDestroy(m_faceLandmarkPtr);
        m_faceLandmarkPtr = Z.FaceLandmarkCreate((uint)LandmarkName);
        m_currentLandmark = LandmarkName;

        m_identity = new float[NumIdentityCoefficients];
        m_expression = new float[NumExpressionCoefficients];
        for (int i = 0; i < NumIdentityCoefficients; ++i) m_identity[i] = 0.0f;
        for (int i = 0; i < NumExpressionCoefficients; ++i) m_expression[i] = 0.0f;
    }

}