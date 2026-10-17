using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable, VolumeComponentMenu("Custom/Room Lighting")]
public class RoomLighting : VolumeComponent
{
    // What a white surface looks like where no light reaches it. Doubles as the room's ambient fill.
    public ColorParameter ShadowColor = new ColorParameter(new Color(0.29f, 0.27f, 0.35f));
}
