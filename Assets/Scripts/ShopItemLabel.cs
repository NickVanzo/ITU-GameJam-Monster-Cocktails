using TMPro;
using UnityEngine;

public class ShopItemLabel : MonoBehaviour
{
    [SerializeField] private ShopItemSO itemInfo;
    [SerializeField] private Color stockColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(1f, 0.3f, 0.3f);

    private TMP_Text label;

    void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        //MaterialInventory.OnChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        //MaterialInventory.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        if (itemInfo == null)
        {
            label.text = "";
            return;
        }

        label.text = $"{itemInfo.displayName.ToUpper()}\nPrice: {itemInfo.price}$";
        label.color = stockColor;
    }
}