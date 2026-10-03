using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private const string IngredientTag = "Ingredient";
    private const string FlushButtonTag = "FlushButton";
    private const string ServeButtonTag = "ServeButton";

    [SerializeField] private Player player;
    [SerializeField] private DrinkGlass kitchenGlass;
    [SerializeField] private DrinkGlass customerGlass;
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
            GetActiveGlass().AddIngredient(target);
        }
        else if (target.CompareTag(FlushButtonTag))
        {
            GetActiveGlass().Flush();
        }
        else if (target.CompareTag(ServeButtonTag))
        {
            kitchenGlass.ServeTo(customerGlass);
            CustomerManager.Instance.ActiveCustomer?.ReceiveDrink(customerGlass.GetIngredientTypes());
            customerGlass.Flush();
        }
    }

    private bool TryRaycastFromCursor(out RaycastHit hit)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out hit, interactionDistance);
    }

    private DrinkGlass GetActiveGlass()
    {
        return player.CurrentRoom == Rooms.Kitchen ? kitchenGlass : customerGlass;
    }
}
