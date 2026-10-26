using UnityEngine;

public class ShopItem : MonoBehaviour
{
    public ShopItemSO itemInfo;

    public bool isMouseOverItem;
    public bool inInspectingView;

    [SerializeField] private Transform initialPosition;
    [SerializeField] private Transform liftedPosition;
    [SerializeField] private Transform inspectionPosition;
    [SerializeField] private Sound hoverSound;
    bool hoverSoundPlayed = false;
    public float movementSpeed = 1.5f;
    [SerializeField] private float returnRotationSpeed = 180f;

    private Quaternion shopRotation;
    private bool returningToShop;

    private void Update()
    {
        if (returningToShop)
        {
            ReturnToShop();
            return;
        }

        if (inInspectingView)
        {
            if (inspectionPosition == null)
            {
                SetInspect(false, null);
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position,
                inspectionPosition.position, movementSpeed * Time.deltaTime);
            return;
        }

        UpdateHover();
    }

    private void UpdateHover()
    {
        if (isMouseOverItem)
        {
            if(hoverSound != null && !hoverSoundPlayed) Sfx.Play(hoverSound);

            hoverSoundPlayed = true;
            transform.position = Vector3.MoveTowards(transform.position,
                liftedPosition.position, movementSpeed * Time.deltaTime);
        }
        else
        {
            hoverSoundPlayed = false;
            if (Vector3.Distance(transform.position, initialPosition.position) < Mathf.Epsilon) return;
            transform.position = Vector3.MoveTowards(transform.position,
                initialPosition.position, movementSpeed * Time.deltaTime);
        }
    }

    public void SetHovered(bool hovered)
    {
        if (returningToShop && hovered) return;
        if (isMouseOverItem == hovered) return;
        isMouseOverItem = hovered;
    }

    public void SetInspect(bool inspect, Transform position)
    {
        if (inspect && (!isMouseOverItem || position == null || returningToShop)) return;

        if (inspect && !inInspectingView) shopRotation = transform.rotation;
        if (!inspect && inInspectingView) returningToShop = true;
        inInspectingView = inspect;
        inspectionPosition = inspect ? position : null;
        if (!inspect)
        {
            SetHovered(false);
            hoverSoundPlayed = false;
        }
    }

    private void ReturnToShop()
    {
        transform.position = Vector3.MoveTowards(transform.position,
            initialPosition.position, movementSpeed * Time.deltaTime);
        transform.rotation = Quaternion.RotateTowards(transform.rotation,
            shopRotation, returnRotationSpeed * Time.deltaTime);
        if (transform.position == initialPosition.position && transform.rotation == shopRotation)
        {
            returningToShop = false;
        }
    }

    private void OnDisable()
    {
        SetInspect(false, null);
    }
}
