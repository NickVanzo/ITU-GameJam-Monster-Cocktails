using UnityEngine;

[CreateAssetMenu]
public class IMonster : ScriptableObject
{
    public string sName;
    [Tooltip("Optional. Spawned instead of the Spawner's default prefab. Needs an EnemyBehavior on its root.")]
    public GameObject Prefab;
    public float fMoveSpeed = 2.2f;
    public int nHealth = 2;
    public int nDamage = 1;
    [Tooltip("Each drop picks one of these at random.")]
    public IngredientType[] aLoot = new IngredientType[0];
    public int nLootCount = 1;
    [Range(0.0f, 1.0f)] public float fLootChance = 1.0f;

    public bool HasLoot => aLoot != null && aLoot.Length > 0 && nLootCount > 0;

    public IngredientType PickLoot()
    {
        return aLoot[Random.Range(0, aLoot.Length)];
    }
}
