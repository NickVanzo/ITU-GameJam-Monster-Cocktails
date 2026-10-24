using UnityEngine;

public class CabinetDoor : MonoBehaviour
{
    public void OpenUpperDoor()
    {
        transform.Rotate(Vector3.up, -90f, Space.Self);
    }

    public void CloseUpperDoor()
    {
        transform.Rotate(Vector3.up, 90f, Space.Self);
    }
}
