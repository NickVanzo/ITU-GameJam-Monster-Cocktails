using System.Collections.Generic;
using UnityEngine;

// How likely someone is to turn up on each day of the week, relative to everyone else due that evening.
[System.Serializable]
public class DayWeights
{
    public float Monday = 1.0f, Tuesday = 1.0f, Wednesday = 1.0f, Thursday = 1.0f, Friday = 1.0f, Saturday = 1.0f, Sunday = 1.0f;

    public float this[Day eDay] => eDay switch
    {
        Day.Monday => Monday,
        Day.Tuesday => Tuesday,
        Day.Wednesday => Wednesday,
        Day.Thursday => Thursday,
        Day.Friday => Friday,
        Day.Saturday => Saturday,
        _ => Sunday
    };
}

// Everything that makes one customer who they are. The prefab is only the body that walks in and talks.
[CreateAssetMenu(menuName = "Bar/Customer")]
public class CustomerObject : ScriptableObject
{
    [Tooltip("Body that walks in. Several customers can share one.")]
    public Customer Prefab;
    [Tooltip("0 keeps them away that day. Someone at 0 every day never comes, and the game can't end without them.")]
    public DayWeights PreferredDays = new();

    [Header("Order")]
    [Tooltip("Pour order doesn't matter, how many of each does.")]
    public IngredientType[] aRecipe = new IngredientType[0];

    [Header("Lines")]
    [TextArea] public string sRequest;
    [TextArea] public string sWrongDrink;
    [TextArea] public string sSatisfied;
    [Tooltip("Said when the bar closes before they got their drink.")]
    [TextArea] public string sClosing = "Closing already ??";

    [Header("Conversation")]
    [Tooltip("First line after the right drink. Left empty, they just leave happy.")]
    public Dialogue Conversation;

    public bool Accepts(IReadOnlyList<IngredientType> aServed)
    {
        if(aServed.Count != aRecipe.Length)
        {
            return false;
        }

        List<IngredientType> aLeft = new(aRecipe);
        foreach(IngredientType served in aServed)
        {
            if(!aLeft.Remove(served))
            {
                return false;
            }
        }

        return true;
    }
}
