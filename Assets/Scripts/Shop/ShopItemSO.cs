using UnityEngine;

[CreateAssetMenu(fileName = "ShopItemSO", menuName = "Scriptable Objects/ShopItemSO")]
public class ShopItemSO : ScriptableObject
{
    public string displayName;
    public int price;
}
