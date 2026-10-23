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
    public ShopPointOfInterest centralPointOfInterest;
    public ShopPointOfInterest currentPointOfInterest;

	public Transform shopCameraPosition;
    public float movementSpeed = 10.0f;
    public AnimationCurve Curve;

    [SerializeField] private GameObject toCustomersButton;
    [SerializeField] private GameObject toKitchenButton;
	[SerializeField] private GameObject toShopButton;

    private Coroutine transitionCoroutine;
    private BarCamera barCamera;
    private bool zoomedInShop = false;

    // Awake, not Start: the bar keeps this disabled while the camera rises, and a late Start would undo SnapToRoom.
    void Awake()
    {
        barCamera = mainCamera.GetComponent<BarCamera>();
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
        Transform target = CameraPositionFor(room);
        SetCameraPose(target.position, target.rotation);
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
        bool up = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        bool down = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);

        if (currentRoom == Rooms.Shop)
        {
            if (up || down)
            {
                HandleShopZoom();
                return;
            }

            if (zoomedInShop && (left || right))
            {
                HandleShopMovement(left);
                return;
            }
        }

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

        ChangeRoom(newRoom);
    }

    public void HandleShopZoom()
    {
        zoomedInShop = !zoomedInShop;
        // Stop any ongoing move coroutine before starting a new one
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(MoveCameraTo(zoomedInShop ? centralPointOfInterest.playerPointOfView : shopCameraPosition));
        currentPointOfInterest = centralPointOfInterest;
    }

    public void HandleShopMovement(bool isMovingLeft)
    {
        Transform nextCameraPosition = null;
        if (isMovingLeft)
        {
            if (currentPointOfInterest.prev != null)
            {
                currentPointOfInterest = currentPointOfInterest.prev;
                nextCameraPosition = currentPointOfInterest.playerPointOfView;
            }
            else
            {
                currentPointOfInterest = centralPointOfInterest;
                nextCameraPosition = currentPointOfInterest.playerPointOfView;
            }
        }
        else
        {
            if (currentPointOfInterest.next != null)
            {
                currentPointOfInterest = currentPointOfInterest.next;
                nextCameraPosition = currentPointOfInterest.playerPointOfView;
            }
            else
            {
                currentPointOfInterest = centralPointOfInterest;
                nextCameraPosition = currentPointOfInterest.playerPointOfView;
            }
        }

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(MoveCameraTo(nextCameraPosition ?? currentPointOfInterest.playerPointOfView));
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
        Vector3 startPosition = mainCamera.position;
        Quaternion startRotation = barCamera != null ? barCamera.BaseRotation : mainCamera.rotation;
        float progress = 0f;
        while (progress < 1f && movementSpeed > 0f)
        {
            progress = Mathf.Clamp01(progress + movementSpeed * Time.deltaTime);
            float blend = Curve.Evaluate(progress);
            SetCameraPose(
                Vector3.Lerp(startPosition, target.position, blend),
                Quaternion.Slerp(startRotation, target.rotation, blend));

            yield return null;
        }

        SetCameraPose(target.position, target.rotation);
    }

    private void SetCameraPose(Vector3 position, Quaternion rotation)
    {
        mainCamera.SetPositionAndRotation(position, rotation);
        if (barCamera != null)
            barCamera.SetBaseRotation(rotation);
    }
}
