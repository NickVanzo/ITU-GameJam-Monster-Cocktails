using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum Rooms {
    MainRoom,
    Kitchen,
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
    public Transform mainCamera;
    public ViewSpot startSpot;
	public Transform inpectItemTransform;
    public float movementSpeed = 10.0f;
    public AnimationCurve Curve;

    [SerializeField] private Button goLeftUIButton;
    [SerializeField] private Button goRightUIButton;
    [SerializeField] private Button goUpUIButton;
    [SerializeField] private Button goDownUIButton;
    private ViewSpot currentSpot;
    private bool engaged;
    private int engagedFrame;
    private Coroutine transitionCoroutine;
    private BarCamera barCamera;

    public ViewSpot CurrentSpot => currentSpot;
    public Rooms CurrentRoom => currentSpot.Room;
    public bool Engaged => engaged;

    void Awake()
    {
#if UNITY_EDITOR
        foreach (IngredientType type in System.Enum.GetValues(typeof(IngredientType)))
        {
            MaterialInventory.Add(type, 999);
        }
#endif

        barCamera = mainCamera.GetComponent<BarCamera>();

        goLeftUIButton.onClick.AddListener(() => Move(BarMovementDirections.Left));
        goRightUIButton.onClick.AddListener(() => Move(BarMovementDirections.Right));
        goUpUIButton.onClick.AddListener(() => Move(BarMovementDirections.Up));
        goDownUIButton.onClick.AddListener(() => Move(BarMovementDirections.Down));

        SnapTo(startSpot);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {            
            Move(BarMovementDirections.Down);
        }

        if (engaged)
            return;

        //todo: Change with proper unity input
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Move(BarMovementDirections.Left);            
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            Move(BarMovementDirections.Right);
        }
        else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))   
        {
            Move(BarMovementDirections.Up);
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {            
            Move(BarMovementDirections.Down);
        }
    }

    public void Move(BarMovementDirections direction)
    {
        if (engaged)
        {
            if (direction == BarMovementDirections.Down)
            {
                SetEngaged(false);                
            }
            return;
        }

        if (direction == BarMovementDirections.Up && currentSpot.bEngageable)
        {
            SetEngaged(true);
            return;
        }

        ViewSpot next = currentSpot.Neighbour(direction);
        if (next != null)
            GoTo(next);
    }

    public void Engage(ViewSpot spot)
    {
        GoTo(spot);
        SetEngaged(true);
    }

    public bool IsEngagedAt(ViewSpot spot)
    {
        return engaged && currentSpot == spot && Time.frameCount > engagedFrame;
    }

    public void GoTo(ViewSpot spot)
    {
        if (spot == currentSpot)
            return;

        if (spot.Room != currentSpot.Room)
            Sfx.Play(Sfx.Sounds.RoomSwitch);

        Focus(spot);

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(MoveCameraTo(spot.Anchor));
    }

    public void SnapTo(ViewSpot spot)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        Focus(spot);
        SetCameraPose(spot.Anchor.position, spot.Anchor.rotation);
    }

    private void Focus(ViewSpot spot)
    {
        if (currentSpot != null)
            currentSpot.OnUnfocus.Invoke();

        currentSpot = spot;
        engaged = false;
        currentSpot.OnFocus.Invoke();
        RefreshButtons();
    }

    private void SetEngaged(bool value)
    {
        engaged = value;
        engagedFrame = Time.frameCount;
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        goLeftUIButton.gameObject.SetActive(CanMove(BarMovementDirections.Left));
        goRightUIButton.gameObject.SetActive(CanMove(BarMovementDirections.Right));
        goUpUIButton.gameObject.SetActive(CanMove(BarMovementDirections.Up));
        goDownUIButton.gameObject.SetActive(CanMove(BarMovementDirections.Down));
    }

    private bool CanMove(BarMovementDirections direction)
    {
        return !engaged && (currentSpot.Neighbour(direction) != null || direction == BarMovementDirections.Up && currentSpot.bEngageable);
    }

    // todo : fucking coroutines man lmao
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
                Quaternion.Slerp(startRotation, target.rotation, blend)
            );

            yield return null;
        }

        SetCameraPose(target.position, target.rotation);
    }

    private void SetCameraPose(Vector3 position, Quaternion rotation)
    {
        mainCamera.SetPositionAndRotation(position, rotation);
        if (barCamera != null)
        {
            barCamera.SetBaseRotation(rotation);
        }
    }

	public bool LockCameraZoom() {
		return currentSpot.Parent != null;
	}
}
