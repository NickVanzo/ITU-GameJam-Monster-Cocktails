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

    [SerializeField] private int customersPerEvening = 3;

    public bool HasCustomerInside => currentCustomer != null;
    private Customer currentCustomer;

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

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            TimeManager.Instance.OnBarOpened -= HandleBarOpened;
        }
    }

    // Nobody comes in until the bar door opens.
    void Start()
    {
        gameOverCanvas.SetActive(false);

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnPhaseChanged += HandlePhaseChanged;
            TimeManager.Instance.OnBarOpened += HandleBarOpened;
        }

        if (TimeManager.IsBarOpen)
        {
            SpawnNextCustomer();
        }
    }

    private void HandleBarOpened()
    {
        BarGameState.Bar.nServedTonight = 0;
        StartCoroutine(SpawnNextCustomerAfterDelay());
    }

    private void HandlePhaseChanged(DayPhase phase)
    {
        // Bar closing
        if (phase == DayPhase.Night && currentCustomer != null)
        {
            currentCustomer.LeaveBar();
        }
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
        bar.nServedTonight++;
        bar.State = CustomerState.Arriving;
        bar.aChoices.Clear();
    }

    public Transform[] GetWaypoint()
    {
        return waypoints;
    }

    public void NotifyCustomerDespawned()
    {
        currentCustomer = null;
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
        if (!TimeManager.IsBarOpen || HasCustomerInside)
        {
            return;
        }

        BarState bar = BarGameState.Bar;
        if (customerPrefabs.Count == 0 || bar.nCustomer >= customerPrefabs.Count)
        {
            Debug.Log("no more customers for the day");
            gameOverCanvas.SetActive(true);
            playerInteraction.enabled = false;
            return;
        }

        if (bar.nServedTonight >= customersPerEvening)
        {
            Debug.Log("last customer of the evening is gone, the bar can close");
            return;
        }

        GameObject prefab = customerPrefabs[bar.nCustomer];
        currentCustomer = Instantiate(prefab, waypoints[0].position, waypoints[0].rotation).GetComponent<Customer>();

        if (bar.State == CustomerState.Arriving)
        {
            Sfx.Play(Sfx.Sounds.CustomerArrive);
        }
    }
}
