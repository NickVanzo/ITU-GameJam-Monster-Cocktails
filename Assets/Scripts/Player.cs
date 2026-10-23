using System.Collections;
using UnityEngine;

public enum Rooms {
    MainRoom,
    Kitchen,
	Shop
}

public class Player : MonoBehaviour
{
    private Rooms currentRoom;
    public Rooms CurrentRoom => currentRoom;
    public Transform mainCamera;
    public Transform kitchenCameraPosition;
    public Transform customersCameraPosition;
	public Transform shopCameraPosition;
    public float movementSpeed = 10.0f;
    public AnimationCurve Curve;
    
    [SerializeField] private GameObject toCustomersButton;
    [SerializeField] private GameObject toKitchenButton;
	[SerializeField] private GameObject toShopButton;

    private Coroutine transitionCoroutine;

    // Awake, not Start: the bar keeps this disabled while the camera rises, and a late Start would undo SnapToRoom.
    void Awake()
    {
#if UNITY_EDITOR
        foreach (IngredientType type in System.Enum.GetValues(typeof(IngredientType)))
        {
            MaterialInventory.Add(type, 999);
        }
#endif

        SnapToRoom(Rooms.MainRoom);
    }

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

        if (!left && !right) return;
        if (left && currentRoom == Rooms.Shop || right && currentRoom == Rooms.Kitchen) return;
        
        Rooms newRoom = currentRoom;
        if (currentRoom == Rooms.MainRoom)
        {
            newRoom = left ? Rooms.Shop : Rooms.Kitchen;
        }

        if (currentRoom == Rooms.Shop)
        {
            newRoom = Rooms.MainRoom;
        }

        if (currentRoom == Rooms.Kitchen)
        {
            newRoom = Rooms.MainRoom;
        }

        Debug.Log(newRoom);
        ChangeRoom(newRoom);
    }

    public void ChangeRoom(Rooms newRoom)
    {
        currentRoom = newRoom;
        Sfx.Play(Sfx.Sounds.RoomSwitch);

        // Stop any ongoing move coroutine before starting a new one
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(MoveCameraTo(CameraPositionFor(newRoom)));
        RefreshRoomButtons();
    }

    private Transform CameraPositionFor(Rooms room)
    {
        if(room == Rooms.Kitchen) return kitchenCameraPosition;
        if(room == Rooms.Shop) return shopCameraPosition;
        return customersCameraPosition;
    }

    private void RefreshRoomButtons()
    {
        toCustomersButton.SetActive(currentRoom != Rooms.MainRoom);
        toKitchenButton.SetActive(currentRoom == Rooms.MainRoom);
        toShopButton.SetActive(currentRoom == Rooms.MainRoom);
    }

    private IEnumerator MoveCameraTo(Transform target)
    {
        Debug.Log("Moving camera to: " + target.position);
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