using System.Collections.Generic;

public static class MaterialInventory
{
    public static event System.Action OnChanged;

    static readonly Dictionary<IngredientType, int> s_Stock = new();

    public static int GetCount(IngredientType type)
    {
        return s_Stock.TryGetValue(type, out int nCount) ? nCount : 0;
    }

    public static void Add(IngredientType type, int amount)
    {
        s_Stock[type] = GetCount(type) + amount;
        OnChanged?.Invoke();
    }

    public static bool TryConsume(IngredientType type)
    {
        int nCount = GetCount(type);
        if(nCount <= 0)
        {
            return false;
        }

        s_Stock[type] = nCount - 1;
        OnChanged?.Invoke();
        return true;
    }

    public static void Clear()
    {
        s_Stock.Clear();
        OnChanged?.Invoke();
    }
}
