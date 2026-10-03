using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue", menuName = "ScriptableObjects/DialogueScriptableObject", order = 2)]
public class Dialogue : ScriptableObject
{
    public string text;
    public string[] options;

    [Tooltip("Dialogue to go to for each option; leave an entry empty to end the conversation on that choice.")]
    public Dialogue[] nextDialogue;
}
