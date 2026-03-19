using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class MandatoryTutorialTrigger : MonoBehaviour
{
    [Header("教程配置")]
    [Tooltip("该强制教程的唯一标识符，必须全局唯一（例如：Tutorial_PickUpLens）")]
    public string tutorialKey;
    
    [Header("流程事件控制 (限制/恢复操作)")]
    [Tooltip("进入触发器时调用（在这里挂载禁用玩家移动、禁用其余交互的逻辑）")]
    public UnityEvent onTutorialStart;

    [Tooltip("教程任务完成时调用（在这里挂载恢复玩家移动、关闭UI面板的逻辑）")]
    public UnityEvent onTutorialComplete;

    private Tutorial tutorialSystem;
    private bool isActive = false; // 防止玩家在里面反复走动多次触发

    private void Start()
    {
        tutorialSystem = FindObjectOfType<Tutorial>();
        if (tutorialSystem == null)
        {
            Debug.LogError("场景中未找到 Tutorial 脚本！");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 确保只触发一次，且是由玩家触发（如果你的玩家Tag不是Player，请修改此处）
        if (other.CompareTag("Player") && !isActive)
        {
            // 检查此教程是否已经彻底完成过
            if (tutorialSystem != null && !tutorialSystem.IsTutorialCompleted(tutorialKey))
            {
                StartMandatoryTutorial();
            }
        }
    }

    /// <summary>
    /// 玩家进入触发器，开始强制性教程
    /// </summary>
    private void StartMandatoryTutorial()
    {
        isActive = true;
        Debug.Log($"[触发强制教学] 锁定玩家操作，当前任务: {tutorialKey}");

        // 执行开始事件（通过Inspector配置：比如调用 StandaloneTutorialUI.PlayTutorial，以及禁用玩家移动）
        onTutorialStart?.Invoke();
    }

    /// <summary>
    /// [核心] 外部调用：当玩家完成了指定的任务（如拿起了物体、按下了按钮），调用此方法解开限制
    /// </summary>
    public void CompleteTutorial()
    {
        if (!isActive) return;

        isActive = false;
        Debug.Log($"[完成强制教学] 任务完成，恢复操作并永久存档: {tutorialKey}");

        // 1. 永久保存进度，以后再进游戏也不会触发了
        if (tutorialSystem != null) tutorialSystem.CompleteTutorialProgress(tutorialKey);

        // 2. 执行完成事件（通过Inspector配置：恢复移动、隐藏高亮等）
        onTutorialComplete?.Invoke();
    }
}