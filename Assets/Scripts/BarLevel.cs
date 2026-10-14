using System.Collections;
using UnityEngine;

// The bar the player starts in and comes back to. It is loaded fresh on every visit: Play() rises out of
// the floor (or fades in on the first visit), runs until the elevator rope is pulled, then sinks the camera
// so the shell can unload the bar and load the level below.
public class BarLevel : MonoBehaviour, ILevel
{
    public Camera BarCamera;
    public Player Player;
    [Tooltip("The elevator in the bar - Elevator sounds come from here while the camera sinks or rises")]
    public Transform BarElevator;

    [Header("During Ride")]
    [Tooltip("Turned off while the camera rises or sinks, so input and camera scripts don't fight the ride.")]
    public Behaviour[] aDisableDuringRide;
    public GameObject[] aHideDuringRide;

    [Header("Ride")]
    [Tooltip("Bar camera sinking into the floor on the way down.")]
    [SerializeField] float fDescentDuration = 1.5f;
    [Tooltip("Bar camera coming back up after the level.")]
    [SerializeField] float fAscentDuration = 0.75f;
    [SerializeField] float fDescentDistance = 2.0f;
    [SerializeField] float fFadeDuration = 0.35f;

    LevelLoader m_Elevator;
    bool bInBar = false;

    public IEnumerator Play(LevelLoader elevator)
    {
        m_Elevator = elevator;
        SetBarActive(false);

        if(elevator.ArrivedByElevator)
        {
            // The elevator is in the kitchen, next to the armory, so that's where the player comes back up.
            Player.SnapToRoom(Rooms.Kitchen);
            yield return RaiseCamera();
        }
        else
        {
            yield return FadeIn();
        }

        SetBarActive(true);
        bInBar = true;
        yield return new WaitWhile(() => bInBar);

        SetBarActive(false);
        m_Elevator.StartRideSound(BarElevator);
        yield return SinkCamera();
    }

    public void Leave()
    {
        bInBar = false;
    }

    IEnumerator FadeIn()
    {
        CanvasGroup fader = m_Elevator.Fader;
        for(float fElapsed = 0.0f; fElapsed < fFadeDuration; fElapsed += Time.deltaTime)
        {
            fader.alpha = 1.0f - fElapsed / fFadeDuration;
            yield return null;
        }
        fader.alpha = 0.0f;
    }

    IEnumerator SinkCamera()
    {
        CanvasGroup fader = m_Elevator.Fader;
        Transform cameraTransform = BarCamera.transform;
        Vector3 vTop = cameraTransform.position;

        Vector3 vBottom = vTop + Vector3.down * fDescentDistance;
        for(float fElapsed = 0.0f; fElapsed < fDescentDuration; fElapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0.0f, 1.0f, fElapsed / fDescentDuration);
            Vector3 vRumble = new Vector3(Mathf.PerlinNoise(fElapsed * 25.0f, 0.0f) - 0.5f, Mathf.PerlinNoise(0.0f, fElapsed * 25.0f) - 0.5f, 0.0f) * 0.04f;
            cameraTransform.position = Vector3.Lerp(vTop, vBottom, t) + vRumble;
            fader.alpha = Mathf.InverseLerp(fDescentDuration - fFadeDuration, fDescentDuration, fElapsed);
            yield return null;
        }
        fader.alpha = 1.0f;
    }

    IEnumerator RaiseCamera()
    {
        CanvasGroup fader = m_Elevator.Fader;
        Transform cameraTransform = BarCamera.transform;
        Vector3 vTop = cameraTransform.position;
        Vector3 vBottom = vTop + Vector3.down * fDescentDistance;

        m_Elevator.StartRideSound(BarElevator);
        for(float fElapsed = 0.0f; fElapsed < fAscentDuration; fElapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0.0f, 1.0f, fElapsed / fAscentDuration);
            cameraTransform.position = Vector3.Lerp(vBottom, vTop, t);
            fader.alpha = 1.0f - Mathf.InverseLerp(0.0f, fFadeDuration, fElapsed);
            yield return null;
        }
        cameraTransform.position = vTop;
        fader.alpha = 0.0f;
        m_Elevator.StopRideSound();
    }

    void SetBarActive(bool bActive)
    {
        foreach(Behaviour behaviour in aDisableDuringRide)
        {
            if(behaviour != null)
            {
                behaviour.enabled = bActive;
            }
        }

        foreach(GameObject go in aHideDuringRide)
        {
            if(go != null)
            {
                go.SetActive(bActive);
            }
        }
    }
}
