using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum Rooms {
    MainRoom,
    Kitchen,
    Workbench,
    Shop
}

public enum BarMovementDirections
{
    Left,
    Right,
    Down,
    Up
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
	public Transform inpectItemTransform;

    public Transform shopCameraPosition;
    public float movementSpeed = 10.0f;
    public AnimationCurve Curve;

    [SerializeField] private Button goLeftUIButton;
    [SerializeField] private Button goRightUIButton;
    [SerializeField] private Button goUpUIButton;
    [SerializeField] private Button goDownUIButton;
    private Coroutine transitionCoroutine;
    private BarCamera barCamera;
    public bool zoomedInShop = false;

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
        
        goLeftUIButton.onClick.AddListener(() => ChangeRoom(BarMovementDirections.Left));
        goRightUIButton.onClick.AddListener(() => ChangeRoom(BarMovementDirections.Right));
        goUpUIButton.onClick.AddListener(() => HandleShopZoom());
        goDownUIButton.onClick.AddListener(() => HandleShopZoom());

        RefreshRoomButtons();
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

            if (zoomedInShop)
            {
                if (left)
                {
                    HandleShopMovement(BarMovementDirections.Left);    
                }

                if (right)
                {
                    HandleShopMovement(BarMovementDirections.Right);
                }
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
        if (!zoomedInShop && currentPointOfInterest != null)
        {
            currentPointOfInterest.OnPlayerUnfocus();
        }
        currentPointOfInterest = centralPointOfInterest;
        RefreshRoomButtons();
    }

    public void HandleShopMovement(BarMovementDirections movDir)
    {
        Transform nextCameraPosition = null;
        if (movDir == BarMovementDirections.Left)
        {
            if (currentPointOfInterest.prev != null)
            {
                currentPointOfInterest.OnPlayerUnfocus();    
                currentPointOfInterest = currentPointOfInterest.prev;
                currentPointOfInterest.OnPlayerFocus();

                nextCameraPosition = currentPointOfInterest.playerPointOfView;
            }
            else
            {
                currentPointOfInterest.OnPlayerUnfocus();
                
                currentPointOfInterest = centralPointOfInterest;
                nextCameraPosition = currentPointOfInterest.playerPointOfView;
            }
        }
        else if(movDir == BarMovementDirections.Right)
        {
            if (currentPointOfInterest.next != null)
            {
                currentPointOfInterest.OnPlayerUnfocus();
                currentPointOfInterest = currentPointOfInterest.next;
                currentPointOfInterest.OnPlayerFocus();

                nextCameraPosition = currentPointOfInterest.playerPointOfView;
            }
            else
            {
                currentPointOfInterest.OnPlayerUnfocus();
                currentPointOfInterest = centralPointOfInterest;
                nextCameraPosition = currentPointOfInterest.playerPointOfView;
            }
        }

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);
        
        transitionCoroutine = StartCoroutine(MoveCameraTo(nextCameraPosition ?? currentPointOfInterest.playerPointOfView));
    }

    private void ChangeRoom(BarMovementDirections dir)
    {
        if (dir != BarMovementDirections.Right && dir != BarMovementDirections.Left) return; // rooms are not up/down 
        
        if (zoomedInShop)
        {
            HandleShopMovement(dir);
            return;
        }
        Rooms newRoom = currentRoom;
        if (dir == BarMovementDirections.Left)
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
        if (!zoomedInShop)
        {
            goLeftUIButton.gameObject.SetActive(currentRoom == Rooms.Kitchen || currentRoom == Rooms.MainRoom);
            goRightUIButton.gameObject.SetActive(currentRoom == Rooms.MainRoom || currentRoom == Rooms.Shop);
            goUpUIButton.gameObject.SetActive(currentRoom == Rooms.Shop);
            goDownUIButton.gameObject.SetActive(false);
        }
        else
        {
            goLeftUIButton.gameObject.SetActive(true);
            goRightUIButton.gameObject.SetActive(true);
            goDownUIButton.gameObject.SetActive(true);
            goUpUIButton.gameObject.SetActive(false);
        }
        
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

	public bool LockCameraZoom() {
		return zoomedInShop; 
	}
}
