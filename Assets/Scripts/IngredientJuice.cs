using System;
using UnityEngine;

public class IngredientJuice : MonoBehaviour
{
    private const string WaterTag = "Water";

    [Serializable]
    private struct IngredientColor
    {
        public IngredientType type;
        public Color color;
    }

    [SerializeField]
    private IngredientColor[] colorsByType =
    {
        new() { type = IngredientType.ZombieBrain, color = new Color(0.2f, 0.8f, 0.3f) },
        new() { type = IngredientType.HolyBlood, color = new Color(0.7f, 0f, 0.05f) },
        new() { type = IngredientType.Gin, color = new Color(0.85f, 0.95f, 0.9f) },
        new() { type = IngredientType.Vodka, color = new Color(0.9f, 0.9f, 0.95f) },
        new() { type = IngredientType.Crux, color = new Color(0.55f, 0.1f, 0.65f) },
    };

    private MeshRenderer meshRenderer;
    private DrinkGlass targetGlass;
    private IngredientType type;

    public Color AppliedColor { get; private set; }

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void Initialize(IngredientType ingredientType, DrinkGlass glass)
    {
        type = ingredientType;
        targetGlass = glass;
        AppliedColor = GetColor(ingredientType);
        meshRenderer.material.color = AppliedColor;
    }

    private Color GetColor(IngredientType ingredientType)
    {
        foreach (IngredientColor entry in colorsByType)
        {
            if (entry.type == ingredientType)
            {
                return entry.color;
            }
        }

        return Color.white;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag(WaterTag))
        {
            return;
        }

        targetGlass.NotifyIngredientLanded(type, AppliedColor, gameObject);
    }
}
