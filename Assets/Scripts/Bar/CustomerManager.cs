using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CustomerManager : MonoBehaviour
{
    public static CustomerManager Instance { get; private set; }

    // The customer in the bar, from walking in until they're out the door again.
    public Customer ActiveCustomer { get; private set; }
    public bool HasCustomerInside => ActiveCustomer != null;

    [Tooltip("0: spawn point, 1: counter/waiting spot, 2: exit transition point, 3: despawn point")]
    [SerializeField] private Transform[] waypoints;

    [Tooltip("Everyone who can come in. BarGameState keeps them by index, so add to the end rather than reordering.")]
    [SerializeField] private List<CustomerObject> aCustomers;
    [SerializeField] private float delayBetweenCustomers = 3f;
    [SerializeField] private int customersPerEvening = 3;

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
            // Only without a TimeManager, where the bar is always open and never got an evening to plan.
            if (BarGameState.Bar.aQueue.Count == 0)
            {
                ScheduleCustomers();
            }
            SpawnNextCustomer();
        }
    }

    // Picks up to customersPerEvening of those not done yet, the ones who like today best being the likeliest.
    private void ScheduleCustomers()
    {
        BarState bar = BarGameState.Bar;
        Day today = TimeManager.Today;

        // Someone the bar unloaded on mid-visit comes back first to finish it.
        int heldOver = bar.State != CustomerState.Arriving && bar.aQueue.Count > 0 ? bar.aQueue[0] : -1;

        // Weighted pick without replacement: random^(1/weight) sorts heavier weights to the front more often.
        IEnumerable<int> picks = Enumerable.Range(0, aCustomers.Count)
            .Where(i => i != heldOver && !bar.aDone.Contains(i) && aCustomers[i].PreferredDays[today] > 0f)
            .OrderByDescending(i => Mathf.Pow(Random.value, 1f / aCustomers[i].PreferredDays[today]));

        if (heldOver >= 0)
        {
            picks = picks.Prepend(heldOver);
        }

        bar.aQueue = picks.Take(customersPerEvening).ToList();
    }

    private void HandleBarOpened()
    {
        ScheduleCustomers();
        StartCoroutine(SpawnNextCustomerAfterDelay());
    }

    private void HandlePhaseChanged(DayPhase phase)
    {
        // Bar closing
        if (phase == DayPhase.Night && ActiveCustomer != null)
        {
            ActiveCustomer.LeaveBar();
        }
    }

    public void ReturnToMainMenu()
    {
        Sfx.Play(Sfx.Sounds.UIClick);
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    // Moves the queue on to the next customer. The finished one can still walk out, but won't come back after a trip.
    public void CompleteCurrentCustomer()
    {
        BarState bar = BarGameState.Bar;
        if (bar.aQueue.Count > 0)
        {
            bar.aDone.Add(bar.aQueue[0]);
            bar.aQueue.RemoveAt(0);
        }
        bar.State = CustomerState.Arriving;
        bar.aChoices.Clear();
    }

    public void NotifyCustomerDespawned()
    {
        ActiveCustomer = null;
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
        if (bar.aDone.Count >= aCustomers.Count)
        {
            gameOverCanvas.SetActive(true);
            playerInteraction.enabled = false;
            return;
        }

        // Everyone due tonight has been, the bar can close.
        if (bar.aQueue.Count == 0)
        {
            return;
        }

        CustomerObject data = aCustomers[bar.aQueue[0]];
        ActiveCustomer = Instantiate(data.Prefab, waypoints[0].position, waypoints[0].rotation);
        ActiveCustomer.Init(data, waypoints);

        if (bar.State == CustomerState.Arriving)
        {
            Sfx.Play(Sfx.Sounds.CustomerArrive);
        }
    }
}
