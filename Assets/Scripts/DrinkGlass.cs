using System.Collections.Generic;
using UnityEngine;

public class DrinkGlass : MonoBehaviour
{
    [SerializeField] private Transform ingredientSpawn;
    [SerializeField] private float ingredientStackHeight = 0.15f;

    private readonly List<GameObject> ingredients = new();

    public void AddIngredient(GameObject ingredient)
    {
        var newIngredient = Instantiate(ingredient, ingredientSpawn);
        newIngredient.transform.SetParent(ingredientSpawn);
        newIngredient.transform.localPosition = ingredientSpawn.localPosition;
        newIngredient.transform.localRotation = Quaternion.identity;

        ingredients.Add(newIngredient);
    }

    public void Flush()
    {
        foreach (GameObject ingredient in ingredients)
        {
            Destroy(ingredient);
        }

        ingredients.Clear();
    }

    public void ServeTo(DrinkGlass targetGlass)
    {
        foreach (GameObject ingredient in ingredients)
        {
            targetGlass.AddIngredient(ingredient);
        }

        Flush();
    }

    public List<IngredientType> GetIngredientTypes()
    {
        List<IngredientType> types = new(ingredients.Count);

        foreach (GameObject ingredient in ingredients)
        {
            if (ingredient.TryGetComponent(out Ingredient ingredientComponent))
            {
                types.Add(ingredientComponent.type);
            }
        }

        return types;
    }
}
