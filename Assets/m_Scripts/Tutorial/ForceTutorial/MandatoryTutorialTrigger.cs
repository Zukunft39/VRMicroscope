using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class MandatoryTutorialTrigger : MonoBehaviour
{
    [Header("Tutorial Configuration")]
    [Tooltip("Globally unique mandatory tutorial identifier, for example Tutorial_001.")]
    public string tutorialKey;

    [Tooltip("Start this tutorial automatically at game startup without entering its trigger.")]
    public bool triggerOnStart = false;

    [Header("Prerequisites")]
    [Tooltip("Require prerequisites to be met before starting this mandatory tutorial.")]
    public bool requirePrerequisite = false;

    [Tooltip("Whether prerequisites are met. External events can update this through the methods below.")]
    public bool prerequisiteSatisfied = false;

    [Header("Global State Condition")]
    [Tooltip("Require a specified Interactor state before starting, such as observation for InsideTutorial.")]
    public bool requireInteractorState = false;

    [Tooltip("Required Interactor state when the global state condition is enabled.")]
    public Interactor.GameState requiredInteractorState = Interactor.GameState.Observing;

    [Tooltip("Allow this queued tutorial to start automatically when its state condition is met.")]
    public bool autoStartWhenInteractorStateMatches = true;

    [Header("Workflow Events (Restrict / Restore Input)")]
    [Tooltip("Invoked on trigger entry; bind movement and interaction restrictions here.")]
    public UnityEvent onTutorialStart;

    [Tooltip("Invoked on completion; bind movement restoration and UI closing here.")]
    public UnityEvent onTutorialComplete;

    [Tooltip("Invoked when entry or automatic start is attempted before prerequisites are met; use for UI or reminders.")]
    public UnityEvent onPrerequisiteBlocked;

    private Tutorial tutorialSystem;
    private ForceTutorialSequenceController sequenceController;
    private bool isActive = false;
    private bool isInvokingPrerequisiteBlocked = false;

    public string TutorialKey => tutorialKey;
    public bool TriggerOnStart => triggerOnStart;
    public bool IsActive => isActive;
    public bool IsPrerequisiteSatisfied => !requirePrerequisite || prerequisiteSatisfied;
    public bool IsInteractorStateSatisfied => !requireInteractorState || IsRequiredInteractorStateMatched();

    private void Start()
    {
        EnsureTutorialSystem();
        sequenceController = FindSequenceController();

        if (sequenceController != null)
        {
            sequenceController.InitializeIfNeeded();
            return;
        }

        if (triggerOnStart && CanStartTutorial())
        {
            Invoke(nameof(StartMandatoryTutorial), 0.1f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || isActive)
        {
            return;
        }

        EnsureTutorialSystem();
        sequenceController = FindSequenceController();

        if (sequenceController != null)
        {
            sequenceController.InitializeIfNeeded();
            sequenceController.TryStartTrigger(this);
            return;
        }

        if (CanStartTutorial())
        {
            StartMandatoryTutorial();
            return;
        }

        InvokePrerequisiteBlockedIfNeeded();
    }

    public void CompleteTutorial()
    {
        if (!isActive)
        {
            return;
        }

        isActive = false;

        if (tutorialSystem != null)
        {
            tutorialSystem.CompleteTutorialProgress(tutorialKey);
        }

        onTutorialComplete?.Invoke();

        if (sequenceController != null)
        {
            sequenceController.NotifyTriggerCompleted(this);
        }
    }

    internal void StartFromSequence()
    {
        StartMandatoryTutorial();
    }

    internal bool CanStartFromSequence()
    {
        return CanStartTutorial();
    }

    internal bool ShouldAutoStartFromSequence()
    {
        return ShouldAutoStartNow();
    }

    internal void BindSequenceController(ForceTutorialSequenceController controller)
    {
        sequenceController = controller;
    }

    internal Tutorial GetTutorialSystem()
    {
        EnsureTutorialSystem();
        return tutorialSystem;
    }

    public void SetPrerequisiteSatisfied(bool isSatisfied)
    {
        bool wasSatisfied = prerequisiteSatisfied;
        prerequisiteSatisfied = isSatisfied;

        if (!isSatisfied)
        {
            return;
        }

        if (!wasSatisfied || isInvokingPrerequisiteBlocked)
        {
            TryEvaluatePendingStart();
        }
    }

    public void ReevaluateStartConditions()
    {
        TryEvaluatePendingStart();
    }

    public static void NotifyGlobalStartConditionsMayHaveChanged()
    {
        MandatoryTutorialTrigger[] triggers = FindObjectsOfType<MandatoryTutorialTrigger>(true);
        for (int i = 0; i < triggers.Length; i++)
        {
            MandatoryTutorialTrigger trigger = triggers[i];
            if (trigger == null || trigger.isActive)
            {
                continue;
            }

            trigger.TryEvaluatePendingStart();
        }
    }

    public void AllowPrerequisite()
    {
        SetPrerequisiteSatisfied(true);
    }

    public void BlockPrerequisite()
    {
        SetPrerequisiteSatisfied(false);
    }

    private void StartMandatoryTutorial()
    {
        if (!CanStartTutorial())
        {
            InvokePrerequisiteBlockedIfNeeded();
            return;
        }

        isActive = true;
        onTutorialStart?.Invoke();
    }

    private bool CanStartTutorial()
    {
        return !isActive &&
               IsPrerequisiteSatisfied &&
               IsInteractorStateSatisfied &&
               !IsTutorialAlreadyCompleted();
    }

    private void EnsureTutorialSystem()
    {
        if (tutorialSystem != null)
        {
            return;
        }

        tutorialSystem = FindObjectOfType<Tutorial>();
    }

    private ForceTutorialSequenceController FindSequenceController()
    {
        return GetComponentInParent<ForceTutorialSequenceController>(true);
    }

    private bool IsPlayerAlreadyInsideTrigger()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            return false;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            return false;
        }

        Collider[] playerColliders = playerObject.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider playerCollider = playerColliders[i];
            if (playerCollider != null && triggerCollider.bounds.Intersects(playerCollider.bounds))
            {
                return true;
            }
        }

        return false;
    }

    private void TryEvaluatePendingStart()
    {
        sequenceController = FindSequenceController();

        if (sequenceController != null)
        {
            sequenceController.TryStartTrigger(this);
            return;
        }

        if (CanStartTutorial() && ShouldAutoStartNow())
        {
            StartMandatoryTutorial();
        }
    }

    private void InvokePrerequisiteBlockedIfNeeded()
    {
        if (!HasAnyBlockingCondition() || isInvokingPrerequisiteBlocked)
        {
            return;
        }

        isInvokingPrerequisiteBlocked = true;
        try
        {
            onPrerequisiteBlocked?.Invoke();
        }
        finally
        {
            isInvokingPrerequisiteBlocked = false;
        }
    }

    private bool IsTutorialAlreadyCompleted()
    {
        EnsureTutorialSystem();
        return tutorialSystem != null && tutorialSystem.IsTutorialCompleted(tutorialKey);
    }

    private bool ShouldAutoStartNow()
    {
        return triggerOnStart ||
               IsPlayerAlreadyInsideTrigger() ||
               CanAutoStartFromInteractorState();
    }

    private bool CanAutoStartFromInteractorState()
    {
        return requireInteractorState &&
               autoStartWhenInteractorStateMatches &&
               IsRequiredInteractorStateMatched();
    }

    private bool IsRequiredInteractorStateMatched()
    {
        return Interactor.Instance != null &&
               Interactor.Instance.CurrentState == requiredInteractorState;
    }

    private bool HasAnyBlockingCondition()
    {
        return (requirePrerequisite && !prerequisiteSatisfied) ||
               (requireInteractorState && !IsRequiredInteractorStateMatched());
    }
}
