using UnityEngine;

[CreateAssetMenu(menuName = "Bar/Dialogue")]
public class Dialogue : ScriptableObject
{
    [TextArea] public string text;
    public string[] options;
    public bool isFinal;

    [Tooltip("Dialogue to go to for each option; leave an entry empty to end the conversation on that choice.")]
    public Dialogue[] nextDialogue;

    public bool HasOptions => options != null && options.Length > 0;

    // Null when that choice ends the conversation.
    public Dialogue Next(int choice)
    {
        return nextDialogue != null && choice >= 0 && choice < nextDialogue.Length ? nextDialogue[choice] : null;
    }
}
