using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomerManager : MonoBehaviour
{
    public static CustomerManager Instance { get; private set; }

    public Customer ActiveCustomer { get; private set; }

    [Tooltip("0: spawn point, 1: counter/waiting spot, 2: exit transition point, 3: despawn point")]
    [SerializeField] private Transform[] waypoints;

    [SerializeField] private List<GameObject> customerPrefabs;
    [SerializeField] private float delayBetweenCustomers = 3f;

    [SerializeField] private GameObject gameOverCanvas;
    [SerializeField] private PlayerInteraction playerInteraction;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        gameOverCanvas.SetActive(false);
        SpawnNextCustomer();
    }

    public void ReturnToMainMenu()
    {
        Sfx.Play(Sfx.Sounds.UIClick);
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    public void RegisterActiveCustomer(Customer customer)
    {
        ActiveCustomer = customer;
    }

    public void UnregisterActiveCustomer(Customer customer)
    {
        if (ActiveCustomer == customer)
        {
            ActiveCustomer = null;
        }
    }

    // Moves the queue on to the next customer. The finished one can still walk out, but won't come back after a trip.
    public void CompleteCurrentCustomer()
    {
        BarState bar = BarGameState.Bar;
        bar.nCustomer++;
        bar.State = CustomerState.Arriving;
        bar.aChoices.Clear();
    }

    public Transform[] GetWaypoint()
    {
        return waypoints;
    }

    public void NotifyCustomerDespawned()
    {
        StartCoroutine(SpawnNextCustomerAfterDelay());
    }

    private IEnumerator SpawnNextCustomerAfterDelay()
    {
        yield return new WaitForSeconds(delayBetweenCustomers);
        SpawnNextCustomer();
    }

    // Spawns the customer BarGameState says is being served, so a customer left mid-order comes back after a trip.
    private void SpawnNextCustomer()
    {
        BarState bar = BarGameState.Bar;
        if (customerPrefabs.Count == 0 || bar.nCustomer >= customerPrefabs.Count)
        {
            Debug.Log("no more customers for the day");
            gameOverCanvas.SetActive(true);
            playerInteraction.enabled = false;
            return;
        }
        
        GameObject prefab = customerPrefabs[bar.nCustomer];
        Instantiate(prefab, waypoints[0].position, waypoints[0].rotation);
        if (bar.State == CustomerState.Arriving)
        {
            Sfx.Play(Sfx.Sounds.CustomerArrive);
        }
    }
}
