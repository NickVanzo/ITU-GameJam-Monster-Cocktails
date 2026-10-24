using System.Collections;
using UnityEngine;

public enum Rooms {
    MainRoom,
    Kitchen,
    Workbench,
    Shop
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
    public ShopPointOfInterest centralPointOfInterest;
    public ShopPointOfInterest currentPointOfInterest;

    public Transform shopCameraPosition;
    public float movementSpeed = 10.0f;
    public AnimationCurve Curve;

    [SerializeField] private GameObject goLeftUIButton;
    [SerializeField] private GameObject goRightUIButton;
    private Coroutine transitionCoroutine;
    private BarCamera barCamera;
    private bool zoomedInShop = false;

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
        SnapToRoom(Rooms.MainRoom);
    }

    public void SnapToRoom(Rooms room)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        currentRoom = room;
        zoomedInShop = false;
        Transform target = CameraPositionFor(room);
        SetCameraPose(target.position, target.rotation);
        RefreshRoomButtons();
    }

    void Update()
    {
        HandleRoomKeys();
    }

    // The shop is to the left of the customer area and the kitchen to its right, so A/Left and D/Right slide the camera that way.
    private void HandleRoomKeys()
    {
        // The workbench uses the same keys to browse guns and attachments.
        if (currentRoom == Rooms.Workbench)
            return;

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

        GoToRoom(newRoom);
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

    public void ChangeRoom(bool leftButtonPressed)
    {
        Debug.Log("Change room");
        Rooms newRoom = currentRoom;
        if (leftButtonPressed)
        {
            newRoom = currentRoom == Rooms.MainRoom ? Rooms.Shop : Rooms.MainRoom;
        }
        else
        {
            newRoom = currentRoom == Rooms.Shop ? Rooms.MainRoom : Rooms.Kitchen;
        }
        GoToRoom(newRoom);
    }

    public void GoToRoom(Rooms room)
    {
        Debug.Log(room + " " + currentRoom);
        if (room == currentRoom)
            return;

        currentRoom = room;
        zoomedInShop = false;
        Sfx.Play(Sfx.Sounds.RoomSwitch);

        // Stop any ongoing move coroutine before starting a new one
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(MoveCameraTo(CameraPositionFor(room)));
        RefreshRoomButtons();
    }

    // Each anchor's rotation is the view from that spot, so the camera faces wherever the anchor faces.
    private Transform CameraPositionFor(Rooms room)
    {
        switch (room)
        {
            case Rooms.Kitchen: return kitchenCameraPosition;
            case Rooms.Workbench: return workbenchCameraPosition;
            case Rooms.Shop: return shopCameraPosition;
            default: return customersCameraPosition;
        }
    }

    private void RefreshRoomButtons()
    {
        goLeftUIButton.SetActive(currentRoom == Rooms.Kitchen || currentRoom == Rooms.MainRoom);
        goRightUIButton.SetActive(currentRoom == Rooms.MainRoom || currentRoom == Rooms.Shop);
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

            // Turns along the way, so it faces the new spot as it arrives.
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
