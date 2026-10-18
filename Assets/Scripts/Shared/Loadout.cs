using System.Collections.Generic;

public static class Loadout
{
    public static GunObject Frame;
    public static readonly List<EquippableObject> Offhand = new();
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

    public static void Clear()
    {
        Frame = null;
        Offhand.Clear();
        s_GunModsDictionary.Clear();
    }
}
