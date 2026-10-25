using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private const string IngredientTag = "Ingredient";
    private const string FlushButtonTag = "FlushButton";
    private const string ServeButtonTag = "ServeButton";
    private const string TutorialTag = "Tutorial";

    [SerializeField] private DrinkGlass kitchenGlass;
    [SerializeField] private float interactionDistance = 100f;

    private Player player;
    private ShopItem hoveredItem;

    void Awake()
    {
        player = GetComponent<Player>();
    }

    void Update()
    {
        // The workbench panel owns interactions while it is open.
        if (player != null && player.CurrentRoom == Rooms.Workbench)
        {
            UpdateHoveredItem(null);
            return;
        }

        if (!TryRaycastFromCursor(out RaycastHit hit))
        {
            UpdateHoveredItem(null);
            return;
        }

        ShopItem item = hit.collider.GetComponentInParent<ShopItem>();
        UpdateHoveredItem(item);

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        HandleClick(hit.collider.gameObject);
    }

    private void HandleClick(GameObject target)
    {
        if (target.CompareTag(IngredientTag))
        {
            if (target.TryGetComponent(out Ingredient ingredient)
                && MaterialInventory.TryConsume(ingredient.type))
            {
                Debug.Log($"{ingredient.type.ToString().ToUpper()} consumed");
                kitchenGlass.AddIngredient(ingredient.type);
            }
            else
            {
                Sfx.Play(Sfx.Sounds.OutOfStock);
            }
        }
        else if (target.CompareTag(FlushButtonTag))
        {
            Sfx.Play(Sfx.Sounds.GlassFlush, kitchenGlass.transform.position);
            kitchenGlass.Flush();
        }
        else if (target.CompareTag(TutorialTag))
        {
            DialogueChoiceUI.Instance.ShowTutorial();
        }
        else if (target.CompareTag(ServeButtonTag))
        {
            Sfx.Play(Sfx.Sounds.Serve);
            CustomerManager.Instance.ActiveCustomer?.ReceiveDrink(kitchenGlass.GetIngredientTypes());
            kitchenGlass.Flush();
        }
        else
        {
            HandleObjectClick(target);
        }
    }

    private void HandleObjectClick(GameObject target)
    {
        Workbench workbench = target.GetComponentInParent<Workbench>();
        if (workbench != null)
        {
            workbench.Open();
            return;
        }

        ElevatorRope rope = target.GetComponentInParent<ElevatorRope>();
        if (rope != null)
        {
            rope.BeginDrag();
        }
    }

    private void UpdateHoveredItem(ShopItem nextItem)
    {
        if (hoveredItem == nextItem)
        {
            return;
        }

        if (hoveredItem != null)
        {
            hoveredItem.SetHovered(false);
        }

        hoveredItem = nextItem;

        if (hoveredItem != null)
        {
            hoveredItem.SetHovered(true);
        }
    }

    private void OnDisable()
    {
        UpdateHoveredItem(null);
    }

    private bool TryRaycastFromCursor(out RaycastHit hit)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        // Ignore triggers so volume bounds and other trigger zones don't block clicks.
        return Physics.Raycast(ray, out hit, interactionDistance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }
}
