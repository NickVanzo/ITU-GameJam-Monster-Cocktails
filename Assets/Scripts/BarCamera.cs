using UnityEngine;

// Makes the bar camera feel alive: it leans toward the cursor, sways with a bit of noise,
// wobbles its head like a bobblehead when it slides to another room, and zooms while right click is held.
// Player moves the camera's position, this only touches its rotation and field of view.
[RequireComponent(typeof(Camera))]
public class BarCamera : MonoBehaviour
{
    [Header("Look")]
    [Tooltip("Yaw and pitch in degrees when the cursor is at the edge of the screen.")]
    [SerializeField] Vector2 vLookAngles = new(6.0f, 4.0f);
    [Tooltip("Same, but while fully zoomed. Wider so the zoomed view can still reach the whole room.")]
    [SerializeField] Vector2 vZoomedLookAngles = new(22.0f, 16.0f);
    [SerializeField] float fLookSharpness = 5.0f;

    [Header("Noise")]
    [Tooltip("Pitch, yaw and roll sway in degrees.")]
    [SerializeField] Vector3 vNoiseAngles = new(0.35f, 0.5f, 0.3f);
    [SerializeField] float fNoiseFrequency = 0.25f;

    [Header("Room Switch Tilt")]
    [Tooltip("Roughly the peak roll in degrees when the camera starts sliding to another room.")]
    [SerializeField] float fTiltAngle = 14.0f;
    [Tooltip("Wobbles per second.")]
    [SerializeField] float fTiltFrequency = 2.2f;
    [Tooltip("Lower = wobbles longer.")]
    [Range(0.05f, 1.0f)] [SerializeField] float fTiltDamping = 0.22f;
    [SerializeField] float fMoveThreshold = 1.0f;

    [Header("Zoom")]
    [SerializeField] CameraZoom Zoom = new();

    Camera m_Camera;
    Quaternion m_qBaseRotation;
    float m_fBaseFieldOfView;
    Vector3 m_vLastPosition;
    Vector2 m_vLook;
    float m_fNoiseTime;
    float m_fTilt;
    float m_fTiltVelocity;
    int m_nMoveDirection;
    float m_fStillTime;

    void Awake()
    {
        m_Camera = GetComponent<Camera>();
        m_qBaseRotation = transform.localRotation;
        m_fBaseFieldOfView = m_Camera.fieldOfView;
    }

    void OnEnable()
    {
        // The FPS level moves the camera while this is disabled, don't read that as a room switch.
        m_vLastPosition = transform.position;
        m_nMoveDirection = 0;
        Zoom.Reset(m_fBaseFieldOfView);
        m_Camera.fieldOfView = m_fBaseFieldOfView;
    }

    void LateUpdate()
    {
        float fDeltaTime = Time.deltaTime;
        if(fDeltaTime <= 0.0f)
        {
            return;
        }

        m_Camera.fieldOfView = Zoom.Update(Input.GetMouseButton(1), fDeltaTime);

        UpdateLook(fDeltaTime);
        UpdateTilt(fDeltaTime);
        m_fNoiseTime += fDeltaTime * fNoiseFrequency;

        float fPitch = m_vLook.y + Noise(0.0f) * vNoiseAngles.x;
        float fYaw = m_vLook.x + Noise(10.0f) * vNoiseAngles.y;
        float fRoll = m_fTilt + Noise(20.0f) * vNoiseAngles.z;
        transform.localRotation = Quaternion.Euler(0.0f, fYaw, 0.0f) * m_qBaseRotation * Quaternion.Euler(fPitch, 0.0f, fRoll);
    }

    void UpdateLook(float fDeltaTime)
    {
        Vector2 vCursor = new(
            Mathf.Clamp01(Input.mousePosition.x / Screen.width) * 2.0f - 1.0f,
            Mathf.Clamp01(Input.mousePosition.y / Screen.height) * 2.0f - 1.0f);

        Vector2 vAngles = Vector2.Lerp(vLookAngles, vZoomedLookAngles, Zoom.Amount);
        Vector2 vTarget = new(vCursor.x * vAngles.x, -vCursor.y * vAngles.y);
        m_vLook = Vector2.Lerp(m_vLook, vTarget, 1.0f - Mathf.Exp(-fLookSharpness * fDeltaTime));
    }

    // Kicks a damped spring each time the camera starts sliding sideways, so the head whips and wobbles back.
    void UpdateTilt(float fDeltaTime)
    {
        float fSideSpeed = Vector3.Dot(transform.position - m_vLastPosition, transform.right) / fDeltaTime;
        m_vLastPosition = transform.position;

        int nDirection = Mathf.Abs(fSideSpeed) > fMoveThreshold ? (int)Mathf.Sign(fSideSpeed) : 0;
        if(nDirection != 0)
        {
            if(nDirection != m_nMoveDirection)
            {
                m_fTiltVelocity -= nDirection * fTiltAngle * fTiltFrequency * 2.0f * Mathf.PI;
            }

            m_nMoveDirection = nDirection;
            m_fStillTime = 0.0f;
        }
        else if((m_fStillTime += fDeltaTime) > 0.2f)
        {
            // Only count as stopped after a moment, so a hitch mid-slide doesn't kick twice.
            m_nMoveDirection = 0;
        }

        float fOmega = fTiltFrequency * 2.0f * Mathf.PI;
        fDeltaTime = Mathf.Min(fDeltaTime, 0.05f);
        m_fTiltVelocity += (-fOmega * fOmega * m_fTilt - 2.0f * fTiltDamping * fOmega * m_fTiltVelocity) * fDeltaTime;
        m_fTilt += m_fTiltVelocity * fDeltaTime;
    }

    float Noise(float fSeed)
    {
        return Mathf.PerlinNoise(m_fNoiseTime, fSeed) * 2.0f - 1.0f;
    }
}
