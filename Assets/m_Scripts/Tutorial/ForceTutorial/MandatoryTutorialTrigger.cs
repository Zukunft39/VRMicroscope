using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class MandatoryTutorialTrigger : MonoBehaviour
{
    [Header("教程配置")]
    [Tooltip("该强制教程的唯一标识符，必须全局唯一（例如：Tutorial_PickUpLens）")]
    public string tutorialKey;
    
    [Tooltip("是否在游戏一开始就自动触发此教学？（勾选后无需玩家走入触发器即可触发）")]
    public bool triggerOnStart = false;

    [Header("流程事件控制 (限制/恢复操作)")]
    [Tooltip("进入触发器时调用（在这里挂载禁用玩家移动、禁用其余交互的逻辑）")]
    public UnityEvent onTutorialStart;

    [Tooltip("教程任务完成时调用（在这里挂载恢复玩家移动、关闭UI面板的逻辑）")]
    public UnityEvent onTutorialComplete;

    [Header("Debug")]
    [Tooltip("是否输出强制教程触发与完成日志")]
    public bool enableDebugLogs = true;

    private Tutorial tutorialSystem;
    private bool isActive = false;

    private void Start()
    {
        tutorialSystem = FindObjectOfType<Tutorial>();
        if (tutorialSystem == null)
        {
            Debug.LogError("场景中未找到 Tutorial 脚本！");
        }

        if (triggerOnStart)
        {
            if (tutorialSystem != null && !tutorialSystem.IsTutorialCompleted(tutorialKey))
            {
                LogDebug("检测到自动触发配置，准备启动强制教程。");
                Invoke(nameof(StartMandatoryTutorial), 0.1f);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isActive)
        {
            if (tutorialSystem != null && !tutorialSystem.IsTutorialCompleted(tutorialKey))
            {
                StartMandatoryTutorial();
            }
        }
    }

    private void StartMandatoryTutorial()
    {
        isActive = true;
        LogDebug("强制教程开始。");
        onTutorialStart?.Invoke();
    }

    public void CompleteTutorial()
    {
        if (!isActive)
        {
            LogDebug("收到完成信号，但当前触发器未激活，已忽略。");
            return;
        }

        isActive = false;

        if (tutorialSystem != null)
        {
            tutorialSystem.CompleteTutorialProgress(tutorialKey);
        }

        LogDebug("强制教程完成，已写入进度并执行完成事件。");
        onTutorialComplete?.Invoke();
    }

    private void LogDebug(string message)
    {
        if (!enableDebugLogs) return;
        Debug.Log($"[ForceTutorial/Trigger:{tutorialKey}] {message}");
    }
}
