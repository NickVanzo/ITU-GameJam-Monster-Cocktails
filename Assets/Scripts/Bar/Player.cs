using System.Collections;
using UnityEngine;

public enum Rooms {
    MainRoom,
    Kitchen,
    Workbench
}

public class Player : MonoBehaviour
{
    private Rooms currentRoom;
    public Rooms CurrentRoom => currentRoom;
    public Transform mainCamera;
    public Transform kitchenCameraPosition;
    public Transform customersCameraPosition;
    [Tooltip("Where the camera sits at the workbench. Its rotation is used too: frame the bench in the Scene view, then GameObject > Align With View.")]
    public Transform workbenchCameraPosition;
    public float movementSpeed = 10.0f;
    public AnimationCurve Curve;

    [SerializeField] private GameObject toCustomersButton;
    [SerializeField] private GameObject toKitchenButton;

    private Coroutine transitionCoroutine;
    private BarCamera barCamera;
    private Quaternion defaultCameraRotation;

    // Awake, not Start: the bar keeps this disabled while the camera rises, and a late Start would undo SnapToRoom.
    void Awake()
    {
#if UNITY_EDITOR
        foreach (IngredientType type in System.Enum.GetValues(typeof(IngredientType)))
        {
            MaterialInventory.Add(type, 999);
        }
#endif

        barCamera = mainCamera.GetComponent<BarCamera>();
        defaultCameraRotation = mainCamera.localRotation;
        SnapToRoom(Rooms.MainRoom);
    }

    public void SnapToRoom(Rooms room)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        currentRoom = room;
        mainCamera.position = CameraPositionFor(room).position;
        SetCameraRotation(ViewRotationFor(room));
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
        GoToRoom((currentRoom == Rooms.MainRoom) ? Rooms.Kitchen : Rooms.MainRoom);
    }

    public void GoToRoom(Rooms room)
    {
        if (room == currentRoom)
            return;

        currentRoom = room;
        Sfx.Play(Sfx.Sounds.RoomSwitch);

        // Stop any ongoing move coroutine before starting a new one
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(MoveCameraTo(room));
        RefreshRoomButtons();
    }

    private Transform CameraPositionFor(Rooms room)
    {
        switch (room)
        {
            case Rooms.Kitchen: return kitchenCameraPosition;
            case Rooms.Workbench: return workbenchCameraPosition;
            default: return customersCameraPosition;
        }
    }

    // The workbench anchor aims the camera at the bench, the rooms keep the camera's rotation from the scene.
    private Quaternion ViewRotationFor(Rooms room)
    {
        if (room != Rooms.Workbench)
            return defaultCameraRotation;

        Quaternion parentRotation = mainCamera.parent != null ? mainCamera.parent.rotation : Quaternion.identity;
        return Quaternion.Inverse(parentRotation) * workbenchCameraPosition.rotation;
    }

    private void SetCameraRotation(Quaternion rotation)
    {
        if (barCamera != null)
            barCamera.BaseRotation = rotation;
    }

    private void RefreshRoomButtons()
    {
        toCustomersButton.SetActive(currentRoom == Rooms.Kitchen);
        toKitchenButton.SetActive(currentRoom == Rooms.MainRoom);
    }

    private IEnumerator MoveCameraTo(Rooms room)
    {
        Transform target = CameraPositionFor(room);
        Quaternion startRotation = barCamera != null ? barCamera.BaseRotation : defaultCameraRotation;
        Quaternion targetRotation = ViewRotationFor(room);
        float distance = Vector3.Distance(mainCamera.position, target.position);

        float delta = 0;
        while (Vector3.Distance(mainCamera.position, target.position) > 0.01f)
        {
            delta += movementSpeed * Time.deltaTime;
            mainCamera.position = Vector3.MoveTowards(
                mainCamera.position,
                target.position,
                Curve.Evaluate(delta)
            );

            // Turns along the way, so it faces the new spot as it arrives.
            float progress = 1.0f - Vector3.Distance(mainCamera.position, target.position) / distance;
            SetCameraRotation(Quaternion.Slerp(startRotation, targetRotation, progress));

            yield return null;
        }

        mainCamera.position = target.position;
        SetCameraRotation(targetRotation);
    }
}
