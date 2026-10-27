using TMPro;
using UnityEngine;

public class TimePanel : MonoBehaviour
{
    public TimeManager TimeManager;
    public TMP_Text DayText;
    public TMP_Text PhaseText;

    [Header("Phase Colors")]
    [SerializeField] Color DawnColor = new(1.0f, 0.7f, 0.6f);
    [SerializeField] Color DayColor = new(1.0f, 0.9f, 0.55f);
    [SerializeField] Color DuskColor = new(1.0f, 0.5f, 0.25f);
    [SerializeField] Color NightColor = new(0.55f, 0.6f, 1.0f);

    void OnEnable()
    {
        TimeManager.OnPhaseChanged += HandlePhaseChanged;
        Refresh();
    }

    void OnDisable()
    {
        TimeManager.OnPhaseChanged -= HandlePhaseChanged;
    }

    void HandlePhaseChanged(DayPhase phase)
    {
        Refresh();
    }

    void Refresh()
    {
        DayText.text = $"WEEK {TimeManager.State.nWeek} - {TimeManager.DayName.ToUpper()}";

        DayPhase phase = TimeManager.Phase;
        PhaseText.text = phase.ToString().ToUpper();
        PhaseText.color = ColorFor(phase);
    }

    Color ColorFor(DayPhase phase)
    {
        switch(phase)
        {
            case DayPhase.Dawn:
                return DawnColor;
            case DayPhase.Dusk:
                return DuskColor;
            case DayPhase.Night:
                return NightColor;
            default:
                return DayColor;
        }
    }
}
