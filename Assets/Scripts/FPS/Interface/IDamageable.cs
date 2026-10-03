using UnityEngine;

public class IDamageable : MonoBehaviour
{
    [SerializeField] protected int nMax = 3;
    protected int nCurrent;

    public int Current => nCurrent;
    public int Max => nMax;

    protected virtual void Awake()
    {
        nCurrent = nMax;
    }

    public bool IsAlive()
    {
        return nCurrent > 0;
    }

    public void ResetHealth()
    {
        nCurrent = nMax;
    }

    public void ResetHealth(int max)
    {
        nMax = max;
        nCurrent = nMax;
    }

    public void TakeDamage(int amount)
    {
        if(!IsAlive())
        {
            return;
        }

        nCurrent = Mathf.Clamp(nCurrent - amount, 0, nMax);
        OnDamaged();

        if(!IsAlive())
        {
            OnDeath();
        }
    }

    protected virtual void OnDamaged()
    {
    }

    protected virtual void OnDeath()
    {
    }
};
