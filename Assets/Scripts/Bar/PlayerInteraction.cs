using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    private const string IngredientTag = "Ingredient";
    private const string FlushButtonTag = "FlushButton";
    private const string ServeButtonTag = "ServeButton";
    private const string TutorialTag = "Tutorial";

    [SerializeField] private DrinkGlass kitchenGlass;
    [SerializeField] private float interactionDistance = 100f;
    [SerializeField] private float inspectionRotationSpeed = 0.3f;

    private static readonly KeyCode[] InputKeys = (KeyCode[])System.Enum.GetValues(typeof(KeyCode));
    private static readonly List<RaycastResult> uiHits = new();
    private Player player;
    private ShopItem hoveredItem;
    private ShopItem inspectedItem;
    private Vector3 previousMousePosition;

	public TMP_Text itemDescriptionForShop;

    void Awake()
    {
        player = GetComponent<Player>();

		itemDescriptionForShop.gameObject.SetActive(false);
            }

    void Update()
    {
        if (inspectedItem != null)
        {
            UpdateInspection();
            return;
        }

        // The bench panels own interactions while they are open.
        if ((player != null && player.Engaged) || IsCursorOverUI())
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

        if (Input.GetMouseButtonDown(1) && hoveredItem != null
            && player != null && player.CurrentRoom == Rooms.Shop)
        {
            BeginInspection();
        }
        else if (Input.GetMouseButtonDown(0))
        {
            HandleClick(hit.collider.gameObject);
        }
    }

    private void UpdateInspection()
    {
        if (HasInspectionDismissalInput()
            || player == null || player.CurrentRoom != Rooms.Shop
            || !inspectedItem.isActiveAndEnabled || !inspectedItem.inInspectingView)
        {
            EndInspection();
            return;
        }

        Vector3 mousePosition = Input.mousePosition;
        if (Input.GetMouseButton(0) && !Input.GetMouseButtonDown(0))
        {
            Vector3 delta = mousePosition - previousMousePosition;
            Transform view = player.mainCamera;
            inspectedItem.transform.Rotate(view.up,
                -delta.x * inspectionRotationSpeed, Space.World);
            inspectedItem.transform.Rotate(view.right,
                delta.y * inspectionRotationSpeed, Space.World);
        }
        previousMousePosition = mousePosition;
    }

    private bool HasInspectionDismissalInput()
    {
        if (Input.mouseScrollDelta.sqrMagnitude > 0f) return true;
        if (!Input.anyKeyDown) return false;

        foreach (KeyCode key in InputKeys)
        {
            if (key != KeyCode.Mouse0 && Input.GetKeyDown(key)) return true;
        }
        return false;
    }

    private void BeginInspection()
    {
        if (player.inpectItemTransform == null)
        {
            return;
        }

        hoveredItem.SetInspect(true, player.inpectItemTransform);
		itemDescriptionForShop.gameObject.SetActive(true);
		itemDescriptionForShop.text = hoveredItem.itemInfo.description;
	
        if (!hoveredItem.inInspectingView) return;

        inspectedItem = hoveredItem;
        previousMousePosition = Input.mousePosition;
        UpdateHoveredItem(null);
    }

    private void EndInspection()
    {
        if (inspectedItem != null)
        {
            inspectedItem.SetInspect(false, null);
        }
		itemDescriptionForShop.gameObject.SetActive(false);
        inspectedItem = null;
        UpdateHoveredItem(null);
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
            target.GetComponentInParent<IClickable>()?.Click(player);
        }
    }

    private void UpdateHoveredItem(ShopItem nextItem)
    {
        if (hoveredItem != null && hoveredItem != nextItem)
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
        EndInspection();
    }

    private static bool IsCursorOverUI()
    {
        EventSystem events = EventSystem.current;
        if (events == null) return false;

        events.RaycastAll(new PointerEventData(events) { position = Input.mousePosition }, uiHits);
        foreach (RaycastResult hit in uiHits)
        {
            Canvas canvas = hit.module.GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace) return true;
        }
        return false;
    }

    // TODO : proper layer mask for this
    private bool TryRaycastFromCursor(out RaycastHit hit)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out hit, interactionDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }
}
