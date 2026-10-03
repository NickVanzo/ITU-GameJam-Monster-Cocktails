using System.Collections;
using UnityEngine;

public enum Rooms {
    MainRoom,
    Kitchen
}

public class Player : MonoBehaviour
{
    private Rooms currentRoom;
    public Rooms CurrentRoom => currentRoom;
    public Transform mainCamera; // Assign your main Camera Transform here
    public Transform kitchenCameraPosition;
    public Transform customersCameraPosition;
    public float movementSpeed = 10.0f;
    
    [SerializeField] private GameObject toCustomersButton;
    [SerializeField] private GameObject toKitchenButton;

    private Coroutine transitionCoroutine;

    void Start()
    {
        currentRoom = Rooms.MainRoom;
        
        toCustomersButton.SetActive(false);
        toKitchenButton.SetActive(true);
        
        MoveCameraTo(customersCameraPosition);
    }

    public void ChangeRoom()
    {
        currentRoom = (currentRoom == Rooms.MainRoom) ? Rooms.Kitchen : Rooms.MainRoom;
        Transform target = (currentRoom == Rooms.Kitchen) ? kitchenCameraPosition : customersCameraPosition;

        // Stop any ongoing move coroutine before starting a new one
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(MoveCameraTo(target));
        toCustomersButton.SetActive(currentRoom == Rooms.Kitchen);
        toKitchenButton.SetActive(currentRoom == Rooms.MainRoom);
    }

    private IEnumerator MoveCameraTo(Transform target)
    {
        while (Vector3.Distance(mainCamera.position, target.position) > 0.01f)
        {
            mainCamera.position = Vector3.MoveTowards(
                mainCamera.position, 
                target.position, 
                movementSpeed * Time.deltaTime
            );

            yield return null; 
        }

        mainCamera.position = target.position;
    }
}