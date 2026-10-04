using UnityEngine;

// Hold-to-zoom field of view eased along a curve, shared by the bar and FPS cameras.
//
//      Zoom.Reset(fBaseFieldOfView);
//      camera.fieldOfView = Zoom.Update(bHoldingZoom, Time.deltaTime);
[System.Serializable]
public class CameraZoom
{
    [Tooltip("How much bigger things look while zoomed.")]
    [SerializeField] float fMagnification = 1.8f;
    [SerializeField] float fDuration = 0.25f;
    [Tooltip("Zoom progress (0 to 1) over the duration (0 to 1). Values above 1 overshoot. Defaults to an exponential ease-out.")]
    [SerializeField] AnimationCurve Curve = ExpoOutCurve();

    float m_fBaseFieldOfView = 60.0f;
    float m_fFrom;
    float m_fTo;
    float m_fElapsed;
    bool bZoomed;

    public float FieldOfView { get; private set; } = 60.0f;

    // 1 when not zoomed, smaller while zoomed. Multiply look sensitivity by it.
    public float Scale => FieldOfView / m_fBaseFieldOfView;

    // 0 when not zoomed, 1 when fully zoomed.
    public float Amount => Mathf.InverseLerp(m_fBaseFieldOfView, ZoomedFieldOfView(), FieldOfView);

    public void Reset(float fBaseFieldOfView)
    {
        m_fBaseFieldOfView = fBaseFieldOfView;
        FieldOfView = m_fFrom = m_fTo = fBaseFieldOfView;
        m_fElapsed = fDuration;
        bZoomed = false;
    }

    public float Update(bool bWantZoom, float fDeltaTime)
    {
        if(bWantZoom != bZoomed)
        {
            bZoomed = bWantZoom;
            m_fFrom = FieldOfView;
            m_fTo = bZoomed ? ZoomedFieldOfView() : m_fBaseFieldOfView;
            m_fElapsed = 0.0f;

            // Cut whatever zoom sound is still going so the new one starts right away.
            Sfx.Stop(Sfx.Sounds.ZoomIn);
            Sfx.Stop(Sfx.Sounds.ZoomOut);
            Sfx.Play(bZoomed ? Sfx.Sounds.ZoomIn : Sfx.Sounds.ZoomOut);
        }

        m_fElapsed += fDeltaTime;
        float t = Mathf.Clamp01(m_fElapsed / Mathf.Max(fDuration, 0.001f));
        FieldOfView = Mathf.LerpUnclamped(m_fFrom, m_fTo, Curve.Evaluate(t));
        return FieldOfView;
    }

    float ZoomedFieldOfView()
    {
        float fHalfAngle = Mathf.Tan(m_fBaseFieldOfView * 0.5f * Mathf.Deg2Rad) / fMagnification;
        return 2.0f * Mathf.Atan(fHalfAngle) * Mathf.Rad2Deg;
    }

    // 1 - 2^(-10t), sampled with matching tangents so the curve follows it closely without overshooting.
    static AnimationCurve ExpoOutCurve()
    {
        float[] aTimes = { 0.0f, 0.05f, 0.15f, 0.3f, 0.5f, 0.7f, 1.0f };
        float fNormalize = 1.0f / (1.0f - Mathf.Pow(2.0f, -10.0f));

        var aKeys = new Keyframe[aTimes.Length];
        for(int i = 0; i < aTimes.Length; ++i)
        {
            float t = aTimes[i];
            float fValue = (1.0f - Mathf.Pow(2.0f, -10.0f * t)) * fNormalize;
            float fSlope = 10.0f * Mathf.Log(2.0f) * Mathf.Pow(2.0f, -10.0f * t) * fNormalize;
            aKeys[i] = new Keyframe(t, fValue, fSlope, fSlope);
        }

        return new AnimationCurve(aKeys);
    }
}
