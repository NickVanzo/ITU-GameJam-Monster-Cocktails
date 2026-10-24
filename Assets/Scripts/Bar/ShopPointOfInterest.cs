using UnityEngine;

public class ShopPointOfInterest : MonoBehaviour
{
    public Transform playerPointOfView;
    public ShopPointOfInterest next;
    public ShopPointOfInterest prev;
    public CabinetDoor cabinetDoor;

    public void OnPlayerFocus()
    {
        if (cabinetDoor != null)
            cabinetDoor.OpenUpperDoor();
    }

    public void OnPlayerUnfocus()
    {
        if(cabinetDoor != null)
            cabinetDoor.CloseUpperDoor();
    }
}
