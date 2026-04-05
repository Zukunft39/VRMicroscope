using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class MandatoryTutorialTrigger : MonoBehaviour
{
    [Header("教程配置")]
    [Tooltip("该强制教程的唯一标识符，必须全局唯一（例如：Tutorial_001）")]
    public string tutorialKey;

    [Tooltip("是否在游戏一开始就自动触发此教学？（勾选后无需玩家走入触发器即可触发）")]
    public bool triggerOnStart = false;

    [Header("前置条件")]
    [Tooltip("是否启用进入该强制教程的前置条件判断。启用后，只有当前置条件满足时才允许开始教程。")]
    public bool requirePrerequisite = false;

    [Tooltip("当前前置条件是否已满足。可由外部事件调用下方方法动态修改。")]
    public bool prerequisiteSatisfied = false;

    [Header("全局状态条件")]
    [Tooltip("是否要求 Interactor 处于指定全局状态后，才允许开始该强制教程。适合 InsideTutorial 这类依赖观察模式的流程。")]
    public bool requireInteractorState = false;

    [Tooltip("当启用全局状态条件时，只有 Interactor 进入该状态后才允许开始教程。")]
    public Interactor.GameState requiredInteractorState = Interactor.GameState.Observing;

    [Tooltip("当全局状态条件满足时，是否允许该教程在队列中自动开始。")]
    public bool autoStartWhenInteractorStateMatches = true;

    [Header("流程事件控制 (限制/恢复操作)")]
    [Tooltip("进入触发器时调用（在这里挂载禁用玩家移动、禁用其余交互的逻辑）")]
    public UnityEvent onTutorialStart;

    [Tooltip("教程任务完成时调用（在这里挂载恢复玩家移动、关闭UI面板的逻辑）")]
    public UnityEvent onTutorialComplete;

    [Tooltip("当玩家进入触发范围、或系统尝试自动启动，但前置条件尚未满足时调用。可用于提示 UI 或播放提醒。")]
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
