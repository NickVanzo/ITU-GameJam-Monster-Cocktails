using System.Collections;
using System.Data;
using UnityEngine;

public class IDamageable : MonoBehaviour
{
    int nMax;
    int nCurrent;
    public bool IsAlive()
    {
        return nCurrent > 0;
    }

    public void TakeDamage(int amount)
    {
        nCurrent = Mathf.Clamp(nCurrent - amount, 0, nMax);
    }
};