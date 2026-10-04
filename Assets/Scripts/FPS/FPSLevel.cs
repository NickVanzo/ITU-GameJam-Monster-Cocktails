using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class FPSLevel : MonoBehaviour
{
    public Spawner spawner;
    public FPSController Player;
    public Transform ArenaElevator;
    [Tooltip("The elevator in the bar. Elevator sounds come from here while the camera sinks or rises.")]
    public Transform BarElevator;
    public Transform PlayerSpawn;
    public CinemachineCamera FPSCamera;
    public CinemachineBrain Brain;

    [Header("Bar")]
    public Behaviour[] aDisableDuringFPS;
    public GameObject[] aHideDuringFPS;

    [Header("UI")]
    public GameObject Hud;
    public TMP_Text HudText;
    public TMP_Text PromptText;
    public Image Crosshair;
    public Image DamageFlash;
    public CanvasGroup Fader;
    public TMP_Text FaderText;

    [Header("Elevator")]
    [Tooltip("Bar camera sinking into the floor on the way down.")]
    [SerializeField] float fDescentDuration = 1.5f;
    [Tooltip("Bar camera coming back up after the arena.")]
    [SerializeField] float fAscentDuration = 1.0f;
    [SerializeField] float fDescentDistance = 4.0f;
    [SerializeField] float fRideDownDuration = 1.8f;
    [SerializeField] float fRideUpDuration = 1.2f;
    [SerializeField] float fRideHeight = 4.0f;
    [SerializeField] float fFadeDuration = 0.35f;
    [SerializeField] float fMessageDuration = 1.2f;

    [Header("Harvest")]
    [SerializeField] bool bLoseHaulOnDeath = true;
    [SerializeField] float fDamageFlashAlpha = 0.35f;
    [SerializeField] Color HeadshotColor = new(1.0f, 0.2f, 0.2f);

    [Header("Music")]
    public AudioSource Music;
    [SerializeField] float fMusicFadeDuration = 1.0f;

    readonly Dictionary<IngredientType, int> m_Haul = new();
    bool bInEncounter = false;
    bool bTransitioning = false;
    Camera m_Camera;
    Vector3 m_vBarCameraPosition;
    Quaternion m_qBarCameraRotation;
    float m_fBarFieldOfView;
    float m_fBarNearClip;
    Vector3 m_vElevatorRest;
    float m_fCrosshairFlash;
    bool bHeadshotFlash;
    AudioSource m_ElevatorLoop;
    Transform m_ElevatorSound;
    Transform m_ElevatorSoundSource;
    float m_fMusicVolume;
    Coroutine m_MusicFade;

    void Awake()
    {
        MaterialInventory.Clear();
        m_Camera = Brain.GetComponent<Camera>();
        m_vElevatorRest = ArenaElevator.position;

        // Elevator sounds follow this, and it sits on whichever elevator is moving.
        m_ElevatorSound = new GameObject("ElevatorSound").transform;
        m_ElevatorSound.SetParent(transform, false);
        Brain.enabled = false;
        FPSCamera.gameObject.SetActive(false);
        Player.gameObject.SetActive(false);
        Hud.SetActive(false);
        Fader.alpha = 0.0f;
        FaderText.text = "";
        SetFlashAlpha(0.0f);

        if(Music != null)
        {
            m_fMusicVolume = Music.volume;
            Music.Stop();
        }
    }

    void OnEnable()
    {
        spawner.OnMaterialCollected += HandleMaterialCollected;
        Player.OnHurt += HandlePlayerHurt;
        Player.OnDied += HandlePlayerDied;
        Player.OnHitEnemy += HandleHitEnemy;
    }

    void OnDisable()
    {
        spawner.OnMaterialCollected -= HandleMaterialCollected;
        Player.OnHurt -= HandlePlayerHurt;
        Player.OnDied -= HandlePlayerDied;
        Player.OnHitEnemy -= HandleHitEnemy;

        Sfx.StopLoop(m_ElevatorLoop);
        m_ElevatorLoop = null;
    }

    public void Enter()
    {
        if(bInEncounter || bTransitioning)
        {
            return;
        }

        StartCoroutine(EnterRoutine());
    }

    public void Leave()
    {
        if(!bInEncounter)
        {
            return;
        }

        bInEncounter = false;
        StartCoroutine(ExitRoutine(true));
    }

    IEnumerator EnterRoutine()
    {
        bTransitioning = true;
        SetBarActive(false);
        StartElevatorSound(BarElevator);

        Transform cameraTransform = m_Camera.transform;
        m_vBarCameraPosition = cameraTransform.position;
        m_qBarCameraRotation = cameraTransform.rotation;
        m_fBarFieldOfView = m_Camera.fieldOfView;
        m_fBarNearClip = m_Camera.nearClipPlane;

        Vector3 vBottom = m_vBarCameraPosition + Vector3.down * fDescentDistance;
        for(float fElapsed = 0.0f; fElapsed < fDescentDuration; fElapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0.0f, 1.0f, fElapsed / fDescentDuration);
            Vector3 vRumble = new Vector3(Mathf.PerlinNoise(fElapsed * 25.0f, 0.0f) - 0.5f, Mathf.PerlinNoise(0.0f, fElapsed * 25.0f) - 0.5f, 0.0f) * 0.04f;
            cameraTransform.position = Vector3.Lerp(m_vBarCameraPosition, vBottom, t) + vRumble;
            Fader.alpha = Mathf.InverseLerp(fDescentDuration - fFadeDuration, fDescentDuration, fElapsed);
            yield return null;
        }
        Fader.alpha = 1.0f;

        Vector3 vTop = m_vElevatorRest + Vector3.up * fRideHeight;
        ArenaElevator.position = vTop;
        StartEncounter();
        SetElevatorSoundSource(ArenaElevator);

        yield return RideElevator(vTop, m_vElevatorRest, fRideDownDuration, true);
        StopElevatorSound();

        Player.SetCanMove(true);
        spawner.StartSpawning();
        bInEncounter = true;
        bTransitioning = false;
    }

    public void StartEncounter()
    {
        m_Haul.Clear();
        Player.Spawn(PlayerSpawn.position, PlayerSpawn.rotation);
        Player.gameObject.SetActive(true);
        FPSCamera.gameObject.SetActive(true);
        Brain.enabled = true;
        Hud.SetActive(true);
        PromptText.text = "";
        SetFlashAlpha(0.0f);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        PlayMusic();
        RefreshHud();
    }

    IEnumerator RideElevator(Vector3 vFrom, Vector3 vTo, float fDuration, bool bFadeIn)
    {
        Vector3 vPlayerOffset = Player.transform.position - ArenaElevator.position;

        for(float fElapsed = 0.0f; fElapsed < fDuration; fElapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0.0f, 1.0f, fElapsed / fDuration);
            ArenaElevator.position = Vector3.Lerp(vFrom, vTo, t);
            Player.Teleport(ArenaElevator.position + vPlayerOffset);
            Fader.alpha = bFadeIn
                ? 1.0f - Mathf.InverseLerp(0.0f, fFadeDuration, fElapsed)
                : Mathf.InverseLerp(fDuration - fFadeDuration, fDuration, fElapsed);
            yield return null;
        }

        ArenaElevator.position = vTo;
        Player.Teleport(vTo + vPlayerOffset);
        Fader.alpha = bFadeIn ? 0.0f : 1.0f;
    }

    IEnumerator ExitRoutine(bool bExtracted)
    {
        bTransitioning = true;
        spawner.StopSpawning();
        Player.SetCanMove(false);
        PromptText.text = "";

        if(Music != null)
        {
            m_MusicFade = StartCoroutine(FadeOutMusic());
        }

        string sMessage;
        if(bExtracted)
        {
            StartElevatorSound(ArenaElevator);
            yield return RideElevator(m_vElevatorRest, m_vElevatorRest + Vector3.up * fRideHeight, fRideUpDuration, false);
            StopElevatorSound();

            sMessage = BankHaul();
            if(!string.IsNullOrEmpty(sMessage))
            {
                Sfx.Play(Sfx.Sounds.HarvestBanked);
            }
        }
        else
        {
            sMessage = "YOU DIED";
            if(bLoseHaulOnDeath)
            {
                if(m_Haul.Count > 0)
                {
                    sMessage += "\nHARVEST LOST";
                }
                m_Haul.Clear();
            }
            else
            {
                sMessage += "\n" + BankHaul();
            }

            FaderText.text = sMessage;
            yield return Fade(0.0f, 1.0f);
        }

        FaderText.text = sMessage;
        if(!string.IsNullOrEmpty(sMessage))
        {
            yield return new WaitForSeconds(fMessageDuration);
        }

        spawner.Clear();
        ArenaElevator.position = m_vElevatorRest;
        Player.gameObject.SetActive(false);
        FPSCamera.gameObject.SetActive(false);
        Brain.enabled = false;
        Hud.SetActive(false);
        FaderText.text = "";

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        m_Camera.fieldOfView = m_fBarFieldOfView;
        m_Camera.nearClipPlane = m_fBarNearClip;

        Transform cameraTransform = m_Camera.transform;
        cameraTransform.rotation = m_qBarCameraRotation;
        Vector3 vBottom = m_vBarCameraPosition + Vector3.down * fDescentDistance;
        StartElevatorSound(BarElevator);
        for(float fElapsed = 0.0f; fElapsed < fAscentDuration; fElapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0.0f, 1.0f, fElapsed / fAscentDuration);
            cameraTransform.position = Vector3.Lerp(vBottom, m_vBarCameraPosition, t);
            Fader.alpha = 1.0f - Mathf.InverseLerp(0.0f, fFadeDuration, fElapsed);
            yield return null;
        }
        cameraTransform.position = m_vBarCameraPosition;
        Fader.alpha = 0.0f;
        StopElevatorSound();

        SetBarActive(true);
        bTransitioning = false;
    }

    public void Update()
    {
        if(!bInEncounter)
        {
            return;
        }

        if(DamageFlash.color.a > 0.0f)
        {
            SetFlashAlpha(Mathf.MoveTowards(DamageFlash.color.a, 0.0f, Time.deltaTime * 1.5f));
        }

        m_fCrosshairFlash = Mathf.MoveTowards(m_fCrosshairFlash, 0.0f, Time.deltaTime * 6.0f);
        Crosshair.color = Color.Lerp(Color.white, bHeadshotFlash ? HeadshotColor : Color.white, m_fCrosshairFlash);
        Crosshair.rectTransform.localScale = Vector3.one * (1.0f + m_fCrosshairFlash * (bHeadshotFlash ? 1.5f : 0.6f));

        PromptText.text = Player.LookedAtRope != null || Player.HeldRope != null ? "HOLD [E] TO GO UP" : "";
    }

    void StartElevatorSound(Transform elevator)
    {
        SetElevatorSoundSource(elevator);
        Sfx.StopLoop(m_ElevatorLoop);
        Sfx.Play(Sfx.Sounds.ElevatorStart, m_ElevatorSound);
        m_ElevatorLoop = Sfx.PlayLoop(Sfx.Sounds.ElevatorMotor, m_ElevatorSound);
    }

    void StopElevatorSound()
    {
        Sfx.StopLoop(m_ElevatorLoop);
        m_ElevatorLoop = null;
        Sfx.Play(Sfx.Sounds.ElevatorArrive, m_ElevatorSound);
    }

    // Without a bar elevator the sounds sit on the camera, which is the same as playing them 2D.
    void SetElevatorSoundSource(Transform elevator)
    {
        m_ElevatorSoundSource = elevator != null ? elevator : m_Camera.transform;
        m_ElevatorSound.position = m_ElevatorSoundSource.position;
    }

    void LateUpdate()
    {
        if(m_ElevatorSoundSource != null)
        {
            m_ElevatorSound.position = m_ElevatorSoundSource.position;
        }
    }

    void PlayMusic()
    {
        if(Music == null)
        {
            return;
        }

        if(m_MusicFade != null)
        {
            StopCoroutine(m_MusicFade);
            m_MusicFade = null;
        }

        Music.volume = m_fMusicVolume;
        if(!Music.isPlaying)
        {
            Music.Play();
        }
    }

    IEnumerator FadeOutMusic()
    {
        float fFrom = Music.volume;
        for(float t = 0.0f; t < 1.0f; t += Time.deltaTime / fMusicFadeDuration)
        {
            Music.volume = Mathf.Lerp(fFrom, 0.0f, t);
            yield return null;
        }

        Music.Stop();
        m_MusicFade = null;
    }

    IEnumerator Fade(float fFrom, float fTo)
    {
        for(float t = 0.0f; t < 1.0f; t += Time.deltaTime / fFadeDuration)
        {
            Fader.alpha = Mathf.Lerp(fFrom, fTo, t);
            yield return null;
        }
        Fader.alpha = fTo;
    }

    string BankHaul()
    {
        if(m_Haul.Count == 0)
        {
            return "";
        }

        var builder = new StringBuilder("HARVESTED");
        foreach(KeyValuePair<IngredientType, int> pair in m_Haul)
        {
            MaterialInventory.Add(pair.Key, pair.Value);
            builder.Append($"\n{pair.Key.ToString().ToUpper()} x{pair.Value}");
        }

        m_Haul.Clear();
        return builder.ToString();
    }

    void HandleMaterialCollected(IngredientType type, int amount)
    {
        if(!bInEncounter)
        {
            return;
        }

        m_Haul[type] = (m_Haul.TryGetValue(type, out int nCount) ? nCount : 0) + amount;
        Sfx.Play(Sfx.Sounds.PickupCollect);
        RefreshHud();
    }

    void HandleHitEnemy(bool bHeadshot)
    {
        m_fCrosshairFlash = 1.0f;
        bHeadshotFlash = bHeadshot;
    }

    void HandlePlayerHurt()
    {
        SetFlashAlpha(fDamageFlashAlpha);
        RefreshHud();
    }

    void HandlePlayerDied()
    {
        if(!bInEncounter)
        {
            return;
        }

        bInEncounter = false;
        StartCoroutine(ExitRoutine(false));
    }

    void RefreshHud()
    {
        var builder = new StringBuilder($"HP {Player.Current}/{Player.Max}");
        foreach(KeyValuePair<IngredientType, int> pair in m_Haul)
        {
            builder.Append($"\n{pair.Key.ToString().ToUpper()} x{pair.Value}");
        }

        HudText.text = builder.ToString();
    }

    void SetFlashAlpha(float fAlpha)
    {
        Color color = DamageFlash.color;
        color.a = fAlpha;
        DamageFlash.color = color;
    }

    void SetBarActive(bool bActive)
    {
        foreach(Behaviour behaviour in aDisableDuringFPS)
        {
            if(behaviour != null)
            {
                behaviour.enabled = bActive;
            }
        }

        foreach(GameObject go in aHideDuringFPS)
        {
            if(go != null)
            {
                go.SetActive(bActive);
            }
        }
    }
}
