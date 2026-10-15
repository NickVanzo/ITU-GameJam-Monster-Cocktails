using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class FPSLevel : MonoBehaviour, ILevel
{
    public Spawner spawner;
    public FPSController Player;
    public Transform ArenaLighting;
    public Transform ArenaElevator;
    public Transform PlayerSpawn;
    public CinemachineCamera FPSCamera;
    [Tooltip("Has the CinemachineBrain and audio listener, the bar turns its own camera off before this level plays.")]
    public Camera ArenaCamera;

    [Header("UI")]
    public GameObject Hud;
    public TMP_Text HudText;
    public TMP_Text PromptText;
    public Image Crosshair;
    public Image DamageFlash;

    [Header("Elevator")]
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
    bool bExtracted = false;
    LevelLoader m_Elevator;
    Vector3 m_vElevatorRest;
    float m_fCrosshairFlash;
    bool bHeadshotFlash;
    float m_fMusicVolume;
    Coroutine m_MusicFade;

    void Awake()
    {
        m_vElevatorRest = ArenaElevator.position;

        ArenaCamera.gameObject.SetActive(false);
        FPSCamera.gameObject.SetActive(false);
        Player.gameObject.SetActive(false);
        ArenaLighting.gameObject.SetActive(false);
        Hud.SetActive(false);
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
    }

    public IEnumerator Play(LevelLoader elevator)
    {
        m_Elevator = elevator;

        Vector3 vTop = m_vElevatorRest + Vector3.up * fRideHeight;
        ArenaElevator.position = vTop;
        StartEncounter();

        // Takes over the motor sound of the bar's elevator, so the whole way down sounds like one ride.
        elevator.StartRideSound(ArenaElevator);
        yield return RideElevator(vTop, m_vElevatorRest, fRideDownDuration, true);
        elevator.StopRideSound();

        Player.SetCanMove(true);
        spawner.StartSpawning();
        bInEncounter = true;

        yield return new WaitWhile(() => bInEncounter);
        yield return ExitRoutine();
    }

    public void Leave()
    {
        if(!bInEncounter)
        {
            return;
        }

        bExtracted = true;
        bInEncounter = false;
    }

    public void StartEncounter()
    {
        m_Haul.Clear();
        Player.Spawn(PlayerSpawn.position, PlayerSpawn.rotation);
        ArenaLighting.gameObject.SetActive(true);
        Player.gameObject.SetActive(true);
        ArenaCamera.gameObject.SetActive(true);
        FPSCamera.gameObject.SetActive(true);
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
        CanvasGroup fader = m_Elevator.Fader;
        Vector3 vPlayerOffset = Player.transform.position - ArenaElevator.position;

        for(float fElapsed = 0.0f; fElapsed < fDuration; fElapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0.0f, 1.0f, fElapsed / fDuration);
            ArenaElevator.position = Vector3.Lerp(vFrom, vTo, t);
            Player.Teleport(ArenaElevator.position + vPlayerOffset);
            fader.alpha = bFadeIn
                ? 1.0f - Mathf.InverseLerp(0.0f, fFadeDuration, fElapsed)
                : Mathf.InverseLerp(fDuration - fFadeDuration, fDuration, fElapsed);
            yield return null;
        }

        ArenaElevator.position = vTo;
        Player.Teleport(vTo + vPlayerOffset);
        fader.alpha = bFadeIn ? 0.0f : 1.0f;
    }

    IEnumerator ExitRoutine()
    {
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
            m_Elevator.StartRideSound(ArenaElevator);
            yield return RideElevator(m_vElevatorRest, m_vElevatorRest + Vector3.up * fRideHeight, fRideUpDuration, false);
            m_Elevator.StopRideSound();

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

            m_Elevator.FaderText.text = sMessage;
            yield return Fade(0.0f, 1.0f);
        }

        m_Elevator.FaderText.text = sMessage;
        if(!string.IsNullOrEmpty(sMessage))
        {
            yield return new WaitForSeconds(fMessageDuration);
        }
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
        CanvasGroup fader = m_Elevator.Fader;
        for(float t = 0.0f; t < 1.0f; t += Time.deltaTime / fFadeDuration)
        {
            fader.alpha = Mathf.Lerp(fFrom, fTo, t);
            yield return null;
        }
        fader.alpha = fTo;
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

        bExtracted = false;
        bInEncounter = false;
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
}
