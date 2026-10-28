using System.Collections.Generic;
using UnityEngine;

// Where one carried item sits in the bag: the cell its top left corner is on, and how many quarter turns clockwise.
[System.Serializable]
public struct BagSlot
{
    public Vector2Int vCell;
    public int nTurns;
}

// Cells an item takes in the bag, drawn the way the loadout camera sees it lying unturned: x to the right, y down.
[System.Serializable]
public class BagShape
{
    public Vector2Int vSize;
    // One per cell, row by row. Ticked cells are taken.
    public bool[] aCells = new bool[0];

    public bool IsSet => vSize.x > 0 && vSize.y > 0;

    // With nothing ticked the whole box is taken, so a shape that's only been sized is a plain rectangle.
    public List<Vector2Int> TakenCells()
    {
        var aTaken = new List<Vector2Int>();
        for(int y = 0; y < vSize.y; y++)
        {
            for(int x = 0; x < vSize.x; x++)
            {
                int i = y * vSize.x + x;
                if(i < aCells.Length && aCells[i])
                {
                    aTaken.Add(new Vector2Int(x, y));
                }
            }
        }

        if(aTaken.Count == 0)
        {
            for(int y = 0; y < vSize.y; y++)
            {
                for(int x = 0; x < vSize.x; x++)
                {
                    aTaken.Add(new Vector2Int(x, y));
                }
            }
        }

        return aTaken;
    }
}

public static class Loadout
{
    public static GunObject Frame;
    public static readonly List<EquippableObject> Offhand = new();
    // One per carried item, in GetCarried order. The bag at the loadout bench packs anything new.
    public static readonly List<BagSlot> Packing = new();
    // Kept up to date by the bag. The elevator won't go down while something sticks out or overlaps.
    public static bool bFitsBag = true;
    static readonly Dictionary<(GunObject, GunSlot), GunAttachment> s_GunModsDictionary = new();
    public static GunAttachment GetFitted(GunObject frame, GunSlotDefinition slot)
    {
        return s_GunModsDictionary.TryGetValue((frame, slot.eSlot), out GunAttachment attachment) ? attachment : slot.Default;
    }

    public static void Fit(GunObject frame, GunSlot eSlot, GunAttachment attachment)
    {
        s_GunModsDictionary[(frame, eSlot)] = attachment;
    }

    public static Gun Build(GunObject frame)
    {
        List<GunAttachment> aAttachments = new();
        foreach(GunSlotDefinition slot in frame.aSlots)
        {
            GunAttachment attachment = GetFitted(frame, slot);
            if(attachment != null)
            {
                aAttachments.Add(attachment);
            }
        }

        return Gun.Build(frame, aAttachments);
    }

    public static void GetCarried(List<EquippableObject> aInto)
    {
        aInto.Clear();
        if(Frame != null)
        {
            aInto.Add(Frame);
        }

        foreach(EquippableObject item in Offhand)
        {
            if(item != null)
            {
                aInto.Add(item);
            }
        }
    }

    public static void Clear()
    {
        Frame = null;
        Offhand.Clear();
        Packing.Clear();
        bFitsBag = true;
        s_GunModsDictionary.Clear();
    }
}
