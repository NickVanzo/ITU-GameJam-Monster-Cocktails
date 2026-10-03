using System.Collections;
using System.Data;
using UnityEngine;

[CreateAssetMenu]
public class IMonster : ScriptableObject
{
    string sName;
    float fMoveSpeed = 10.0f; 
    Ingredient Loot;
}
