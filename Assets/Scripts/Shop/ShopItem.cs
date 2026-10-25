using UnityEngine;

public class ShopItem : MonoBehaviour
{
    public ShopItemSO itemInfo;

    public bool isMouseOverItem;

    [SerializeField] private Transform initialPosition;
    [SerializeField] private Transform liftedPosition;
    [SerializeField] private Sound hoverSound;
    bool hoverSoundPlayed = false;
    public float movementSpeed = 1.5f;

    private void Update()
    {
        if (isMouseOverItem)
        {
            if(!hoverSoundPlayed) Sfx.Play(hoverSound);
            
            hoverSoundPlayed = true;
            if (!isMouseOverItem) return;
            transform.position = Vector3.MoveTowards(transform.position, liftedPosition.position, movementSpeed * Time.deltaTime);
        }
        else
        {
            hoverSoundPlayed = false;
            if (Vector3.Distance(transform.position, initialPosition.position) < Mathf.Epsilon) return;
            transform.position = Vector3.MoveTowards(transform.position, initialPosition.position, movementSpeed * Time.deltaTime);
        }
    }

    public void SetHovered(bool hovered)
    {
        if (isMouseOverItem == hovered) return;

        isMouseOverItem = hovered;
    }
}
