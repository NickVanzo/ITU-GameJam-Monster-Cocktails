using System.Collections;
using UnityEngine;

public enum DayPhase
{
    Dawn,
    Day,
    Dusk,
    Night
}

[System.Serializable]
public class TimeState
{
    public int nWeek = 1;
    public int nDay = 0;
}

// The clock stops twice a day: at dusk until the bar has been opened by hand and closed again, then at night until
// the player rides the elevator
//      Dusk  -> Night    bar door opens, customers come in, then the door closes and the elevator runs
//      Night -> Dawn     fNightDuration after going down
//      Dawn  -> Day      fDawnDuration
//      Day   -> Dusk     fDayDuration
// Coming back up skips to the next dusk for nw
public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }
    public static readonly string[] aDayNames = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
    public static bool IsBarOpen => Instance == null || Instance.bBarOpen;
    public static bool ElevatorRuns => Instance == null || Instance.Phase == DayPhase.Night;
    public event System.Action<DayPhase> OnPhaseChanged;
    public event System.Action OnBarOpened;

    [SerializeField] float fNightDuration = 4.0f;
    [SerializeField] float fDawnDuration = 4.0f;
    [SerializeField] float fDayDuration = 4.0f;
    public TimeState State { get; private set; } = new();
    public DayPhase Phase { get; private set; } = DayPhase.Day;
    public string DayName => aDayNames[State.nDay];

    bool bBarOpen;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(StartDay());
    }

    void OnDestroy()
    {
        if(Instance == this)
        {
            Instance = null;
        }
    }

    public bool OpenBar()
    {
        if(Phase != DayPhase.Dusk || bBarOpen)
        {
            return false;
        }

        bBarOpen = true;
        OnBarOpened?.Invoke();
        return true;
    }

    public bool CloseBar()
    {
        if(!bBarOpen)
        {
            return false;
        }

        bBarOpen = false;
        SetPhase(DayPhase.Night);
        return true;
    }

    public void GoBelow()
    {
        StopAllCoroutines();

        StartCoroutine(StartNight());
    }

    public void ComeBackUp()
    {
        StopAllCoroutines();

        if(Phase == DayPhase.Night)
        {
            AdvanceDay();
        }

        SetPhase(DayPhase.Dusk);
    }

    // For savestate..
    public void Load(TimeState state)
    {
        state.nWeek = Mathf.Max(state.nWeek, 1);
        state.nDay = Mathf.Clamp(state.nDay, 0, aDayNames.Length - 1);
        State = state;
        bBarOpen = false;

        StopAllCoroutines();

        SetPhase(DayPhase.Day);
        
        StartCoroutine(StartDay());
    }

    IEnumerator StartNight()
    {
        yield return new WaitForSeconds(fNightDuration);

        AdvanceDay();
        
        SetPhase(DayPhase.Dawn);

        StartCoroutine(StartDawn());
    }

    IEnumerator StartDawn()
    {
        yield return new WaitForSeconds(fDawnDuration);

        SetPhase(DayPhase.Day);

        StartCoroutine(StartDay());
    }

    IEnumerator StartDay()
    {
        yield return new WaitForSeconds(fDayDuration);

        SetPhase(DayPhase.Dusk);
    }

    void AdvanceDay()
    {
        State.nDay++;
        if(State.nDay >= aDayNames.Length)
        {
            State.nDay = 0;
            State.nWeek++;
        }
    }

    void SetPhase(DayPhase phase)
    {
        Phase = phase;
        OnPhaseChanged?.Invoke(phase);
    }
}
