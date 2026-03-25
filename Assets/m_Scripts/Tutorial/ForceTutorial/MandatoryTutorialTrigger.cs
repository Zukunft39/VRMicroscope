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

    [Header("流程事件控制 (限制/恢复操作)")]
    [Tooltip("进入触发器时调用（在这里挂载禁用玩家移动、禁用其余交互的逻辑）")]
    public UnityEvent onTutorialStart;

    [Tooltip("教程任务完成时调用（在这里挂载恢复玩家移动、关闭UI面板的逻辑）")]
    public UnityEvent onTutorialComplete;

    private Tutorial tutorialSystem;
    private ForceTutorialSequenceController sequenceController;
    private bool isActive = false;

    public string TutorialKey => tutorialKey;
    public bool TriggerOnStart => triggerOnStart;
    public bool IsActive => isActive;

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
        }
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
        return triggerOnStart || IsPlayerAlreadyInsideTrigger();
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

    private void StartMandatoryTutorial()
    {
        if (!CanStartTutorial())
        {
            return;
        }

        isActive = true;
        onTutorialStart?.Invoke();
    }

    private bool CanStartTutorial()
    {
        return !isActive &&
               tutorialSystem != null &&
               !tutorialSystem.IsTutorialCompleted(tutorialKey);
    }

    private void EnsureTutorialSystem()
    {
        if (tutorialSystem != null)
        {
            return;
        }

        tutorialSystem = FindObjectOfType<Tutorial>();
        if (tutorialSystem == null)
        {
            Debug.LogError("场景中未找到 Tutorial 脚本！");
        }
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
}
