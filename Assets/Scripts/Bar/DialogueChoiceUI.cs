using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueChoiceUI : MonoBehaviour
{
    public static DialogueChoiceUI Instance { get; private set; }

    [SerializeField] private GameObject choice1;
    [SerializeField] private GameObject choice2;
    
    [SerializeField] private GameObject tutoral;

    private GameObject[] choices;
    private Button[] choiceButtons;

    void Awake()
    {
        Instance = this;

        choices = new[] { choice1, choice2 };
        choiceButtons = System.Array.ConvertAll(choices, choice => choice.GetComponentInChildren<Button>(true));

        Hide();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // Options past the number of buttons are left out.
    public void Show(string[] optionTexts, System.Action<int> onChoiceSelected)
    {
        for (int i = 0; i < choices.Length; i++)
        {
            bool hasOption = i < optionTexts.Length;
            choices[i].SetActive(hasOption);

            Button button = choiceButtons[i];
            button.onClick.RemoveAllListeners();
            if (!hasOption)
            {
                continue;
            }

            int choiceIndex = i;
            button.GetComponentInChildren<TMP_Text>().text = optionTexts[i];
            button.onClick.AddListener(PlayClickSound);
            button.onClick.AddListener(() => onChoiceSelected(choiceIndex));
        }
    }

    public void Hide()
    {
        foreach (GameObject choice in choices)
        {
            choice.SetActive(false);
        }
    }

    private static void PlayClickSound()
    {
        Sfx.Play(Sfx.Sounds.UIClick);
    }

    public void ShowTutorial()
    {
        DialogueChoiceUI.Instance.tutoral.SetActive(true);        
    }

    public void HideTutorial()
    {   
        DialogueChoiceUI.Instance.tutoral.SetActive(false);        
    }
}
