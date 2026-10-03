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

    private int customersServed = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        gameOverCanvas.SetActive(false);
        SpawnNextCustomer();
    }

    public void ReturnToMainMenu()
    {
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

    private void SpawnNextCustomer()
    {
        if (customerPrefabs.Count == 0 || customersServed >= customerPrefabs.Count)
        {
            Debug.Log("no more customers for the day");
            gameOverCanvas.SetActive(true);
            playerInteraction.enabled = false;
            return;
        }
        
        GameObject prefab = customerPrefabs[customersServed];
        Instantiate(prefab, waypoints[0].position, waypoints[0].rotation);
        customersServed++;
    }
}
