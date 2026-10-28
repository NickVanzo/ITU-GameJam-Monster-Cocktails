using System;
using System.Collections;
using UnityEngine;


[System.Serializable]
public class TimeState
{
    public int nWeek = 1;
    public Day eDay = 0;
}

[System.Serializable]
public enum Day
{
    Monday,
    Tuesday,
    Wednesday,
    Thursday,
    Friday,
    Saturday,
    Sunday
}

public enum DayPhase
{
    Dawn,
    Day,
    Dusk,
    Night
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
    public static Day Today => Instance != null ? Instance.State.eDay : Day.Monday;
    public event System.Action<DayPhase> OnPhaseChanged;
    public event System.Action OnBarOpened;

    [SerializeField] float fNightDuration = 4.0f;
    [SerializeField] float fDawnDuration = 4.0f;
    [SerializeField] float fDayDuration = 4.0f;
    public TimeState State { get; private set; } = new();
    public DayPhase Phase { get; private set; } = DayPhase.Day;
    public string DayName => aDayNames[(int) State.eDay];

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
        state.eDay = (Day) Math.Clamp((int) state.eDay, 0, Enum.GetNames(typeof(Day)).Length - 1);
        State = state;

        bBarOpen = false;

        StopAllCoroutines();

        SetPhase(DayPhase.Day);
        
        StartCoroutine(StartDay());
    }

    // Should probably move out of these coroutines tbh
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

    [Tweak("Time")]
    public static void SkipDay()
    {
        Instance.AdvanceDay();
        Instance.SetPhase(DayPhase.Dawn);
        Instance.StopAllCoroutines();
        Instance.StartCoroutine(Instance.StartDawn());
    }

    void AdvanceDay()
    {
        State.eDay++;
        if((int) State.eDay >= Enum.GetNames(typeof(Day)).Length)
        {
            State.eDay = 0;
            State.nWeek++;
        }
    }

    void SetPhase(DayPhase phase)
    {
        Phase = phase;
        OnPhaseChanged?.Invoke(phase);
    }
}
