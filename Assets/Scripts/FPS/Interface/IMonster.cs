using UnityEngine;

[CreateAssetMenu]
public class IMonster : ScriptableObject
{
    public string sName;
    public float fMoveSpeed = 2.2f;
    public int nHealth = 2;
    public int nDamage = 1;
    public IngredientType Loot;
    public int nLootCount = 1;
    [Range(0.0f, 1.0f)] public float fLootChance = 1.0f;
}
