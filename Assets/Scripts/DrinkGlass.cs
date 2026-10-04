using System.Collections.Generic;
using UnityEngine;

public class DrinkGlass : MonoBehaviour
{
    [SerializeField] private Transform ingredientSpawn;
    [SerializeField] private GameObject juicePrefab;
    [SerializeField] private Renderer waterRenderer;

    private readonly List<GameObject> fallingJuices = new();
    private readonly List<IngredientType> containedIngredients = new();

    private Color baseWaterColor;

    void Awake()
    {
        baseWaterColor = waterRenderer.material.color;
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
        containedIngredients.Add(type);
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
        containedIngredients.Clear();
        waterRenderer.material.color = baseWaterColor;
    }

    private void TintWater(Color ingredientColor)
    {
        Color currentColor = waterRenderer.material.color;
        float mixWeight = 1f / containedIngredients.Count;
        waterRenderer.material.color = Color.Lerp(currentColor, ingredientColor, mixWeight);
    }

    public List<IngredientType> GetIngredientTypes()
    {
        return new List<IngredientType>(containedIngredients);
    }
}
