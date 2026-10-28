using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

// The body that walks in, orders and talks. Who it is and what it says comes from the CustomerObject it's given.
public class Customer : MonoBehaviour
{
    private enum State
    {
        MovingToCounter,
        WaitingForDrink,
        // Just got a drink and is saying what they think of it.
        Reacting,
        InDialogue,
        MovingToExit,
        MovingToDespawn
    }

    // Indices into the waypoints CustomerManager hands over.
    private const int SpawnPoint = 0, CounterPoint = 1, ExitPoint = 2, DespawnPoint = 3;

    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private GameObject dialogueCanvas;
    [SerializeField] private TMP_Text dialogueText;

    [SerializeField] private float retryDelaySeconds = 5f;
    [SerializeField] private float satisfiedDisplaySeconds = 2f;
    [SerializeField] private float dialogueEndDelaySeconds = 2f;
    [SerializeField] private float footstepLength = 0.8f;

    public CustomerObject Data { get; private set; }

    private Transform[] waypoints;
    private State state;
    private Dialogue currentDialogue;
    private float footstepDistance;

    // Called right after spawning, before Start.
    public void Init(CustomerObject data, Transform[] waypoints)
    {
        Data = data;
        this.waypoints = waypoints;
    }

    // Picks up where BarGameState left off, so a customer served before a trip to another level resumes there.
    void Start()
    {
        dialogueCanvas.SetActive(false);

        BarState bar = BarGameState.Bar;
        switch (bar.State)
        {
            case CustomerState.Arriving:
                state = State.MovingToCounter;
                agent.SetDestination(waypoints[CounterPoint].position);
                break;
            case CustomerState.WaitingForDrink:
                PlaceAtCounter();
                AskForDrink();
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
        agent.Warp(waypoints[CounterPoint].position);
        Vector3 walkDirection = Vector3.ProjectOnPlane(waypoints[CounterPoint].position - waypoints[SpawnPoint].position, Vector3.up);
        if (walkDirection.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(walkDirection);
        }
    }

    private void AskForDrink()
    {
        state = State.WaitingForDrink;
        BarGameState.Bar.State = CustomerState.WaitingForDrink;
        Speak(Data.sRequest);
    }

    private void ResumeDialogue(List<int> choices)
    {
        Dialogue dialogue = Data.Conversation;
        foreach (int choice in choices)
        {
            Dialogue next = dialogue.Next(choice);
            if (next == null)
            {
                Debug.LogError($"Customer '{Data.name}': saved dialogue choice {choice} doesn't exist after '{dialogue.name}'. Were the dialogue assets changed? Resuming from '{dialogue.name}'.", this);
                break;
            }
            dialogue = next;
        }

        ShowDialogue(dialogue);
    }

    void Update()
    {
        UpdateFootsteps();

        if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
        {
            return;
        }

        // Only the walking states have somewhere to get to.
        switch (state)
        {
            case State.MovingToCounter:
                AskForDrink();
                break;
            case State.MovingToExit:
                state = State.MovingToDespawn;
                agent.SetDestination(waypoints[DespawnPoint].position);
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

        state = State.Reacting;
        if (Data.Accepts(servedIngredients))
        {
            BarGameState.Bar.State = CustomerState.InDialogue;
            BarGameState.Bar.aChoices.Clear();
            Sfx.Play(Sfx.Sounds.CustomerHappy, transform.position);
            Speak(Data.sSatisfied);
            StartCoroutine(BeginDialogueAfterDelay());
        }
        else
        {
            Sfx.Play(Sfx.Sounds.CustomerAngry, transform.position);
            Speak(Data.sWrongDrink);
            StartCoroutine(RepeatRequestAfterDelay());
        }
    }

    // Only someone still waiting on a drink leaves, a customer who got theirs finishes the conversation first.
    public void LeaveBar()
    {
        bool waiting = state == State.MovingToCounter || state == State.WaitingForDrink || state == State.Reacting;
        if (!waiting || BarGameState.Bar.State == CustomerState.InDialogue)
        {
            return;
        }

        // for now just drop everything..
        StopAllCoroutines();

        BarGameState.Bar.State = CustomerState.Arriving;
        Speak(Data.sClosing);
        WalkOut();
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

    private IEnumerator RepeatRequestAfterDelay()
    {
        yield return new WaitForSeconds(retryDelaySeconds);
        AskForDrink();
    }

    private IEnumerator BeginDialogueAfterDelay()
    {
        yield return new WaitForSeconds(satisfiedDisplaySeconds);
        ShowDialogue(Data.Conversation);
    }

    private void ShowDialogue(Dialogue dialogue)
    {
        state = State.InDialogue;
        currentDialogue = dialogue;

        if (dialogue == null)
        {
            StartCoroutine(EndDialogueAfterDelay());
            return;
        }

        ShowText(dialogue.text);

        if (dialogue.HasOptions)
        {
            DialogueChoiceUI.Instance.Show(dialogue.options, SelectDialogueChoice);
        }
        else
        {
            DialogueChoiceUI.Instance.Hide();
            StartCoroutine(EndDialogueAfterDelay());
        }
    }

    private void SelectDialogueChoice(int choiceIndex)
    {
        if (state != State.InDialogue)
        {
            return;
        }

        Dialogue next = currentDialogue.Next(choiceIndex);
        if (next != null)
        {
            BarGameState.Bar.aChoices.Add(choiceIndex);
            ShowDialogue(next);
            return;
        }

        DialogueChoiceUI.Instance.Hide();
        if (currentDialogue.isFinal)
        {
            Sfx.Play(Sfx.Sounds.SoulHarvest, transform.position);
        }
        StartCoroutine(EndDialogueAfterDelay());
    }

    private IEnumerator EndDialogueAfterDelay()
    {
        // Done as soon as the conversation ends: leaving the bar now brings the next customer, not this one.
        CustomerManager.Instance.CompleteCurrentCustomer();

        yield return new WaitForSeconds(dialogueEndDelaySeconds);

        dialogueCanvas.SetActive(false);
        WalkOut();
    }

    private void WalkOut()
    {
        state = State.MovingToExit;
        agent.SetDestination(waypoints[ExitPoint].position);
    }

    private void Speak(string line)
    {
        Sfx.Play(Sfx.Sounds.CustomerSpeak, transform.position);
        ShowText(line);
    }

    private void ShowText(string line)
    {
        dialogueText.text = line;
        dialogueCanvas.SetActive(true);
    }
}
