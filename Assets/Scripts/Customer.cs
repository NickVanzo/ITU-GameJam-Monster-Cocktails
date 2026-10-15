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
        InDialogue,
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
    [SerializeField] private float dialogueEndDelaySeconds = 2f;

    [SerializeField] private GameObject dialogueCanvas;
    [SerializeField] private TMP_Text dialogueText;

    [SerializeField] private Dialogue startingDialogue;

    [SerializeField] private float footstepLength = 0.8f;
    
    public event System.Action<Customer, string> OnSpeak;

    private State state;
    private Dialogue currentDialogue;
    private float footstepDistance;

    // Picks up where GameState left off, so a customer served before a trip to another level resumes there.
    void Start()
    {
        waypoints = CustomerManager.Instance.GetWaypoint();
        dialogueCanvas.SetActive(false);

        BarState bar = GameState.Bar;
        switch (bar.State)
        {
            case CustomerState.Arriving:
                transform.position = waypoints[0].position;
                state = State.MovingToCounter;
                agent.SetDestination(waypoints[1].position);
                break;
            case CustomerState.WaitingForDrink:
                PlaceAtCounter();
                ArriveAtCounter();
                break;
            case CustomerState.InDialogue:
                PlaceAtCounter();
                ResumeDialogue(bar.aChoices);
                break;
        }
    }

    // Faces the way it would after walking in from the spawn point.
    private void PlaceAtCounter()
    {
        agent.Warp(waypoints[1].position);
        Vector3 walkDirection = Vector3.ProjectOnPlane(waypoints[1].position - waypoints[0].position, Vector3.up);
        if (walkDirection.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(walkDirection);
        }
    }

    private void ArriveAtCounter()
    {
        state = State.WaitingForDrink;
        GameState.Bar.State = CustomerState.WaitingForDrink;
        Speak(requestLine);
        CustomerManager.Instance.RegisterActiveCustomer(this);
    }

    private void ResumeDialogue(List<int> choices)
    {
        currentDialogue = startingDialogue;
        foreach (int choice in choices)
        {
            Dialogue[] next = currentDialogue.nextDialogue;
            if (next == null || choice >= next.Length || next[choice] == null)
            {
                Debug.LogError($"Customer '{name}': saved dialogue choice {choice} doesn't exist after '{currentDialogue.name}'. Were the dialogue assets changed? Resuming from '{currentDialogue.name}'.", this);
                break;
            }
            currentDialogue = next[choice];
        }

        state = State.InDialogue;
        ShowCurrentDialogue();
    }

    void Update()
    {
        UpdateFootsteps();

        if (state == State.WaitingForDrink || state == State.InDialogue)
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
                ArriveAtCounter();
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
            GameState.Bar.State = CustomerState.InDialogue;
            GameState.Bar.aChoices.Clear();
            Sfx.Play(Sfx.Sounds.CustomerHappy, transform.position);
            Speak(satisfiedLine);
            StartCoroutine(BeginDialogueAfterDelay());
        }
        else
        {
            Sfx.Play(Sfx.Sounds.CustomerAngry, transform.position);
            Speak(wrongDrinkLine);
            StartCoroutine(RepeatRequestAfterDelay());
        }
    }

    private void UpdateFootsteps()
    {
        footstepDistance += agent.velocity.magnitude * Time.deltaTime;
        if (footstepDistance >= footstepLength)
        {
            footstepDistance -= footstepLength;
            Sfx.Play(Sfx.Sounds.CustomerFootstep, transform.position);
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

    private IEnumerator BeginDialogueAfterDelay()
    {
        yield return new WaitForSeconds(satisfiedDisplaySeconds);
        
        currentDialogue = startingDialogue;
        state = State.InDialogue;
        ShowCurrentDialogue();
    }

    private void ShowCurrentDialogue()
    {
        dialogueText.text = currentDialogue.text;
        dialogueCanvas.SetActive(true);

        if (currentDialogue.options == null || currentDialogue.options.Length == 0)
        {
            DialogueChoiceUI.Instance.Hide();
            StartCoroutine(EndDialogueAfterDelay());
            return;
        }

        var choiceCallbacks = new UnityEngine.Events.UnityAction[currentDialogue.options.Length];
        for (int i = 0; i < choiceCallbacks.Length; i++)
        {
            int choiceIndex = i;
            choiceCallbacks[i] = () => SelectDialogueChoice(choiceIndex);
        }

        DialogueChoiceUI.Instance.Show(currentDialogue.options, choiceCallbacks);
    }

    private void SelectDialogueChoice(int choiceIndex)
    {
        if (state != State.InDialogue)
        {
            return;
        }

        Dialogue next = null;
        if (currentDialogue.nextDialogue != null && choiceIndex < currentDialogue.nextDialogue.Length)
        {
            next = currentDialogue.nextDialogue[choiceIndex];
        }


        if (next != null)
        {
            GameState.Bar.aChoices.Add(choiceIndex);
            currentDialogue = next;
            ShowCurrentDialogue();
        }
        else
        {
            DialogueChoiceUI.Instance.Hide();
            if (currentDialogue.isFinal)
            {
                Sfx.Play(Sfx.Sounds.SoulHarvest, transform.position);
            }
            StartCoroutine(EndDialogueAfterDelay());
        }
    }

    private IEnumerator EndDialogueAfterDelay()
    {
        // Done as soon as the conversation ends: leaving the bar now brings the next customer, not this one.
        CustomerManager.Instance.CompleteCurrentCustomer();

        yield return new WaitForSeconds(dialogueEndDelaySeconds);
    
        dialogueCanvas.SetActive(false);
        state = State.MovingToExit;
        agent.SetDestination(waypoints[2].position);
    }

    private void Speak(string line)
    {
        Sfx.Play(Sfx.Sounds.CustomerSpeak, transform.position);
        dialogueText.text = line;
        dialogueCanvas.SetActive(true);
        OnSpeak?.Invoke(this, line);
    }
}
