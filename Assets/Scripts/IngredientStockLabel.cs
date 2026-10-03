using TMPro;
using UnityEngine;

public class IngredientStockLabel : MonoBehaviour
{
    [SerializeField] private Ingredient source;
    [SerializeField] private Color stockColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(1f, 0.3f, 0.3f);

    private TMP_Text label;

    public Ingredient Source => source;

    void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        MaterialInventory.OnChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        MaterialInventory.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        if (source == null)
        {
            label.text = "";
            return;
        }

        int count = MaterialInventory.GetCount(source.type);
        label.text = $"{source.type.ToString().ToUpper()}\nx{count}";
        label.color = count > 0 ? stockColor : emptyColor;
    }
}
