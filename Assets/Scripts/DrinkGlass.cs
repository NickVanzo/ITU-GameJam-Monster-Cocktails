using System.Collections.Generic;
using UnityEngine;

public class DrinkGlass : MonoBehaviour
{
    [SerializeField] private Transform ingredientSpawn;
    [SerializeField] private GameObject juicePrefab;
    [SerializeField] private Renderer waterRenderer;

    private readonly List<GameObject> fallingJuices = new();

    private Color baseWaterColor;

    // Kept in GameState so the drink survives the bar unloading while the player is in another level.
    private static List<IngredientType> ContainedIngredients => GameState.Bar.aGlass;

    void Awake()
    {
        baseWaterColor = waterRenderer.material.color;
        RestoreWaterColor();
    }

    // Replays the pours in order, so the water ends up the same color it was before the bar unloaded.
    private void RestoreWaterColor()
    {
        IngredientJuice juice = juicePrefab.GetComponent<IngredientJuice>();
        Color color = baseWaterColor;
        for (int i = 0; i < ContainedIngredients.Count; i++)
        {
            color = MixWater(color, juice.GetColor(ContainedIngredients[i]), i + 1);
        }
        waterRenderer.material.color = color;
    }

    public void AddIngredient(IngredientType type)
    {
        GameObject juiceInstance = Instantiate(juicePrefab, ingredientSpawn.position, Quaternion.identity);
        juiceInstance.GetComponent<IngredientJuice>().Initialize(type, this);
        Sfx.Play(Sfx.Sounds.IngredientPour, ingredientSpawn.position);

        fallingJuices.Add(juiceInstance);
    }

    public void NotifyIngredientLanded(IngredientType type, Color ingredientColor, GameObject juiceInstance)
    {
        fallingJuices.Remove(juiceInstance);
        ContainedIngredients.Add(type);
        TintWater(ingredientColor);
        Sfx.Play(Sfx.Sounds.IngredientSplash, transform.position);
        Destroy(juiceInstance);
    }

    public void Flush()
    {
        foreach (GameObject juice in fallingJuices)
        {
            Destroy(juice);
        }

        fallingJuices.Clear();
        ContainedIngredients.Clear();
        waterRenderer.material.color = baseWaterColor;
    }

    private void TintWater(Color ingredientColor)
    {
        waterRenderer.material.color = MixWater(waterRenderer.material.color, ingredientColor, ContainedIngredients.Count);
    }

    // Each ingredient weighs as much as all the ones before it combined, so the mix stays an even blend.
    private static Color MixWater(Color currentColor, Color ingredientColor, int ingredientCount)
    {
        return Color.Lerp(currentColor, ingredientColor, 1f / ingredientCount);
    }

    public List<IngredientType> GetIngredientTypes()
    {
        return new List<IngredientType>(ContainedIngredients);
    }
}
