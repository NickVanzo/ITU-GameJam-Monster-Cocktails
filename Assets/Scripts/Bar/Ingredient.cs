using UnityEngine;

public enum IngredientType
{
    ZombieBrain,
    HolyBlood,
    Gin,
    Vodka,
    Crux,
    Rat
}

public class Ingredient : MonoBehaviour
{
    public IngredientType type;
}
