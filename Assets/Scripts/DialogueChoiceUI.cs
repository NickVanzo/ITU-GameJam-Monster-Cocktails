using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueChoiceUI : MonoBehaviour
{
    public static DialogueChoiceUI Instance { get; private set; }

    [SerializeField] private GameObject choice1;
    [SerializeField] private GameObject choice2;

    private Button choice1Button;
    private Button choice2Button;

    void Awake()
    {
        Instance = this;

        choice1Button = choice1.GetComponentInChildren<Button>(true);
        choice2Button = choice2.GetComponentInChildren<Button>(true);

        Hide();
    }

    public void Show(string[] optionTexts, UnityEngine.Events.UnityAction[] onChoiceSelected)
    {
        choice1Button.onClick.RemoveAllListeners();
        choice2Button.onClick.RemoveAllListeners();
        choice1Button.onClick.AddListener(PlayClickSound);
        choice2Button.onClick.AddListener(PlayClickSound);

        bool hasFirst = optionTexts.Length > 0;
        bool hasSecond = optionTexts.Length > 1;

        choice1.SetActive(hasFirst);
        choice2.SetActive(hasSecond);

        if (hasFirst)
        {
            choice1Button.GetComponentInChildren<TMP_Text>().text = optionTexts[0];
            choice1Button.onClick.AddListener(onChoiceSelected[0]);
        }

        if (hasSecond)
        {
            choice2Button.GetComponentInChildren<TMP_Text>().text = optionTexts[1];
            choice2Button.onClick.AddListener(onChoiceSelected[1]);
        }
    }

    public void Hide()
    {
        choice1.SetActive(false);
        choice2.SetActive(false);
    }

    private static void PlayClickSound()
    {
        Sfx.Play(Sfx.Sounds.UIClick);
    }
}
