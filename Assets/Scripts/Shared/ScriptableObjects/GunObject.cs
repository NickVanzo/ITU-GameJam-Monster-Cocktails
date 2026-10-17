using System;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "FPS/Gun")]
public class GunObject : ScriptableObject
{
    public string sId; // to int32
    public string sDisplayName;
  
    // public GunView ViewModel;
    [Tooltip("Lies on the workbench as a rigidbody. Colliders are optional: a box around its meshes is added if it has none.")]
    public GameObject DisplayModel;

    public float fBaseRecoil = 0.8f; // Todo : Change this !!!
    public List<GunSlotDefinition> aSlots = new();
    [SerializeReference, SubclassPicker] public GunTrigger Trigger;
    [SerializeReference, SubclassPicker] public List<GunConstraint> aConstraints = new();
    [SerializeReference, SubclassPicker] public GunSpawner Spawner;
    public Sound Shot, DryFire, Reload;
}
