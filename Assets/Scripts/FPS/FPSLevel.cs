using System.Collections;
using System.Data;
using UnityEngine;

public class FPSLevel : MonoBehaviour
{
    public Spawner spawner;

    bool bIsReady = false;

    public void StartEncounter()
    {
        spawner.Reset();
    }

    public void EndEncounter()
    {
        
    }

    public void Update()
    {
        
    }
}