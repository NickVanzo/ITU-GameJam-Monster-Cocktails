using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private const string IngredientTag = "Ingredient";
    private const string FlushButtonTag = "FlushButton";
    private const string ServeButtonTag = "ServeButton";

    [SerializeField] private Player player;
    [SerializeField] private DrinkGlass kitchenGlass;
    [SerializeField] private float interactionDistance = 100f;

    void Update()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (!TryRaycastFromCursor(out RaycastHit hit))
        {
            return;
        }

        GameObject target = hit.collider.gameObject;

        if (target.CompareTag(IngredientTag))
        {
            if (target.TryGetComponent(out Ingredient ingredient) && MaterialInventory.TryConsume(ingredient.type))
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
        else if (target.CompareTag(ServeButtonTag))
        {
            Sfx.Play(Sfx.Sounds.Serve);
            CustomerManager.Instance.ActiveCustomer?.ReceiveDrink(kitchenGlass.GetIngredientTypes());
            kitchenGlass.Flush();
        }
        else
        {
            ElevatorRope rope = target.GetComponentInParent<ElevatorRope>();
            if (rope != null)
            {
                rope.BeginDrag();
            }
        }
    }

    private bool TryRaycastFromCursor(out RaycastHit hit)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        // Ignore triggers so volume bounds and other trigger zones don't block clicks.
        return Physics.Raycast(ray, out hit, interactionDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }
}
