using UnityEngine;

public enum IngredientType
{
    ZombieBrain,
    HolyBlood,
    Gin,
    Vodka,
    Crux
}

public class Ingredient : MonoBehaviour
{
    public IngredientType type;
}
