using System.Collections.Generic;

public enum CustomerState
{
    Arriving,
    WaitingForDrink,
    InDialogue
}

// Plain data on purpose: no scene or asset references, so it can be written to a save file as-is.
[System.Serializable]
public class BarState
{
    // Index into CustomerManager's customer list of the customer currently being served.
    public int nCustomer;
    // Customers done since the bar door opened in the eveming
    public int nServedTonight;
    public CustomerState State;
    // Options picked since the customer's starting dialogue, replayed to get back to the same line.
    public List<int> aChoices = new();
    // Ingredients that have landed in the kitchen glass, in pour order.
    public List<IngredientType> aGlass = new();
}

// Survives the bar unloading while the player is in another level.
public static class BarGameState
{
    public static BarState Bar = new();

    public static void Reset()
    {
        Bar = new BarState();
    }
}
