using System.Collections.Generic;

// The gun the player takes down the elevator, and what the workbench fitted to each gun.
// Survives the bar unloading while the player is in another level.
public static class Loadout
{
    // Picked in the armory. Null = the FPS player's own GunDefinition.
    public static GunObject Frame;

    static readonly Dictionary<(GunObject, GunSlot), GunAttachment> s_Fitted = new();

    // The workbench's pick for that slot, or the slot's Default until something else is fitted.
    public static GunAttachment GetFitted(GunObject frame, GunSlotDefinition slot)
    {
        return s_Fitted.TryGetValue((frame, slot.eSlot), out GunAttachment attachment) ? attachment : slot.Default;
    }

    public static void Fit(GunObject frame, GunSlot eSlot, GunAttachment attachment)
    {
        s_Fitted[(frame, eSlot)] = attachment;
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

    public static void Clear()
    {
        Frame = null;
        s_Fitted.Clear();
    }
}
