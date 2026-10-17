using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "FPS/Gun Attachment")]
public class GunAttachment : ScriptableObject
{
    public string sId;
    public string sDisplayName;
    public GunSlot eSlot;
    // public GameObject Model;
    [SerializeReference, SubclassPicker]
    public List<GunPart> aParts = new();
    public List<StatChange> aStatChanges = new();

    public string DisplayName => string.IsNullOrEmpty(sDisplayName) ? name : sDisplayName;
}