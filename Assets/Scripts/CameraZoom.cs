using UnityEngine;

// Hold-to-zoom field of view with an exponential ease-out, shared by the bar and FPS cameras.
//
//      Zoom.Reset(fBaseFieldOfView);
//      camera.fieldOfView = Zoom.Update(bHoldingZoom, Time.deltaTime);
[System.Serializable]
public class CameraZoom
{
    [Tooltip("How much bigger things look while zoomed.")]
    [SerializeField] float fMagnification = 1.8f;
    [SerializeField] float fDuration = 0.25f;

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
            Sfx.Play(bZoomed ? Sfx.Sounds.ZoomIn : Sfx.Sounds.ZoomOut);
        }

        m_fElapsed += fDeltaTime;
        FieldOfView = Mathf.LerpUnclamped(m_fFrom, m_fTo, EaseOutExpo(m_fElapsed / Mathf.Max(fDuration, 0.001f)));
        return FieldOfView;
    }

    float ZoomedFieldOfView()
    {
        float fHalfAngle = Mathf.Tan(m_fBaseFieldOfView * 0.5f * Mathf.Deg2Rad) / fMagnification;
        return 2.0f * Mathf.Atan(fHalfAngle) * Mathf.Rad2Deg;
    }

    static float EaseOutExpo(float t)
    {
        return t >= 1.0f ? 1.0f : (1.0f - Mathf.Pow(2.0f, -10.0f * t)) / (1.0f - Mathf.Pow(2.0f, -10.0f));
    }
}
