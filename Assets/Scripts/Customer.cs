using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class Customer : MonoBehaviour
{
    private enum State
    {
        MovingToCounter,
        WaitingForDrink,
        MovingToExit,
        MovingToDespawn
    }

    [SerializeField] private NavMeshAgent agent;

    [Tooltip("0: spawn point, 1: counter/waiting spot, 2: exit transition point, 3: despawn point")]
    private Transform[] waypoints;

    [SerializeField] private List<IngredientType> requiredIngredients;

    [SerializeField] private string requestLine;
    [SerializeField] private string wrongDrinkLine;
    [SerializeField] private string satisfiedLine;
    [SerializeField] private float retryDelaySeconds = 5f;
    [SerializeField] private float satisfiedDisplaySeconds = 2f;

    [SerializeField] private GameObject dialogueCanvas;
    [SerializeField] private TMP_Text dialogueText;

    public event System.Action<Customer, string> OnSpeak;

    private State state;

    void Start()
    {
        waypoints = CustomerManager.Instance.GetWaypoint();
        transform.position = waypoints[0].position;
        state = State.MovingToCounter;
        agent.SetDestination(waypoints[1].position);
        dialogueCanvas.SetActive(false);
    }

    void Update()
    {
        if (state == State.WaitingForDrink)
        {
            return;
        }

        if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
        {
            return;
        }

        switch (state)
        {
            case State.MovingToCounter:
                state = State.WaitingForDrink;
                Speak(requestLine);
                CustomerManager.Instance.RegisterActiveCustomer(this);
                break;
            case State.MovingToExit:
                state = State.MovingToDespawn;
                agent.SetDestination(waypoints[3].position);
                break;
            case State.MovingToDespawn:
                CustomerManager.Instance.NotifyCustomerDespawned();
                Destroy(gameObject);
                break;
        }
    }

    public void ReceiveDrink(IReadOnlyList<IngredientType> servedIngredients)
    {
        if (state != State.WaitingForDrink)
        {
            return;
        }

        CustomerManager.Instance.UnregisterActiveCustomer(this);

        if (IsCorrectDrink(servedIngredients))
        {
            Speak(satisfiedLine);
            StartCoroutine(LeaveCounterAfterDelay());
        }
        else
        {
            Speak(wrongDrinkLine);
            StartCoroutine(RepeatRequestAfterDelay());
        }
    }

    private bool IsCorrectDrink(IReadOnlyList<IngredientType> servedIngredients)
    {
        if (servedIngredients.Count != requiredIngredients.Count)
        {
            return false;
        }

        foreach (IngredientType required in requiredIngredients)
        {
            if (!servedIngredients.Contains(required))
            {
                return false;
            }
        }

        return true;
    }

    private IEnumerator RepeatRequestAfterDelay()
    {
        yield return new WaitForSeconds(retryDelaySeconds);

        state = State.WaitingForDrink;
        CustomerManager.Instance.RegisterActiveCustomer(this);
        Speak(requestLine);
    }

    private IEnumerator LeaveCounterAfterDelay()
    {
        yield return new WaitForSeconds(satisfiedDisplaySeconds);

        dialogueCanvas.SetActive(false);
        state = State.MovingToExit;
        agent.SetDestination(waypoints[2].position);
    }

    private void Speak(string line)
    {
        dialogueText.text = line;
        dialogueCanvas.SetActive(true);
        OnSpeak?.Invoke(this, line);
    }
}
