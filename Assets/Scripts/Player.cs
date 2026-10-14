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
    public AnimationCurve Curve;
    
    [SerializeField] private GameObject toCustomersButton;
    [SerializeField] private GameObject toKitchenButton;

    private Coroutine transitionCoroutine;

    // Awake, not Start: the bar keeps this disabled while the camera rises, and a late Start would undo SnapToRoom.
    void Awake()
    {
        SnapToRoom(Rooms.MainRoom);
    }

    // Puts the camera straight into a room, without the slide or the sound.
    public void SnapToRoom(Rooms room)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        currentRoom = room;
        mainCamera.position = CameraPositionFor(room).position;
        RefreshRoomButtons();
    }

    void Update()
    {
        HandleRoomKeys();
    }

    // The kitchen is to the right of the customer area, so A/Left and D/Right slide the camera that way.
    private void HandleRoomKeys()
    {
        bool left = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
        bool right = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);

        if ((left && currentRoom == Rooms.Kitchen) || (right && currentRoom == Rooms.MainRoom))
        {
            ChangeRoom();
        }
    }

    public void ChangeRoom()
    {
        currentRoom = (currentRoom == Rooms.MainRoom) ? Rooms.Kitchen : Rooms.MainRoom;
        Sfx.Play(Sfx.Sounds.RoomSwitch);

        // Stop any ongoing move coroutine before starting a new one
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(MoveCameraTo(CameraPositionFor(currentRoom)));
        RefreshRoomButtons();
    }

    private Transform CameraPositionFor(Rooms room)
    {
        return (room == Rooms.Kitchen) ? kitchenCameraPosition : customersCameraPosition;
    }

    private void RefreshRoomButtons()
    {
        toCustomersButton.SetActive(currentRoom == Rooms.Kitchen);
        toKitchenButton.SetActive(currentRoom == Rooms.MainRoom);
    }

    private IEnumerator MoveCameraTo(Transform target)
    {
        float delta = 0;
        while (Vector3.Distance(mainCamera.position, target.position) > 0.01f)
        {
            delta += movementSpeed * Time.deltaTime;
            mainCamera.position = Vector3.MoveTowards(
                mainCamera.position, 
                target.position, 
                Curve.Evaluate(delta)
            );

            yield return null; 
        }

        mainCamera.position = target.position;
    }
}