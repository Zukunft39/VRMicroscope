using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

[Serializable]
public class TutorialStep
{
    [TextArea(2, 5)]
    [Tooltip("当前步骤要显示的教程文本")]
    public string stepText;

    [Tooltip("是否为【强制交互】步骤？\n如果勾选：按空格无效，必须由外部正确交互后调用 CompleteAction()。\n如果不勾选：玩家可直接按空格进入下一步。")]
    public bool isMandatoryInteraction = false;

    [Header("进入该步骤时触发的事件")]
    [Tooltip("可用于：进入该步骤时执行特定操作。\n例如：如果这是强制交互步骤，在这里解开玩家的部分操作（如恢复手柄抓取或按键功能）。")]
    public UnityEvent onStepStart;
}

public class StandaloneTutorialUI : MonoBehaviour
{
    [Header("UI 引用")]
    [Tooltip("用于显示教程文字的 TextMeshPro 组件")]
    public TextMeshProUGUI tutorialTextDisplay;
    
    [Tooltip("整个教程界面的根节点（通常就是当前Canvas的第一个子物体，用于自动显示/隐藏）")]
    public GameObject uiRoot;

    [Header("教程流程配置")]
    public List<TutorialStep> steps = new List<TutorialStep>();

    [Header("全局生命周期事件")]
    [Tooltip("教程开始时触发（在这里挂载：完全禁用玩家所有操作/移动）")]
    public UnityEvent onTutorialStart;

    [Tooltip("教程全部结束时触发（在这里挂载：恢复玩家所有操作、隐藏UI等）")]
    public UnityEvent onTutorialFinish;

    [Tooltip("玩家在强制交互阶段执行了【错误操作】时触发（可配置播放错误提示音等）")]
    public UnityEvent onWrongAction;

    private int currentStepIndex = 0;
    private bool isPlaying = false;
    private Coroutine errorCoroutine;
    private Color originalTextColor;

    private void Awake()
    {
        if (tutorialTextDisplay != null)
        {
            originalTextColor = tutorialTextDisplay.color;
        }
        if (uiRoot != null) uiRoot.SetActive(false); // 初始默认隐藏
    }

    private void Update()
    {
        if (!isPlaying || currentStepIndex >= steps.Count) return;

        // 如果当前【不是】强制交互步骤，按下空格键则直接进入下一步
        if (!steps[currentStepIndex].isMandatoryInteraction)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                NextStep();
            }
        }
    }

    /// <summary>
    /// 启动教程（外部通过触发器调用，如 MandatoryTutorialTrigger 的事件中调用此方法）
    /// </summary>
    public void PlayTutorial()
    {
        if (steps.Count == 0) return;
        
        isPlaying = true;
        currentStepIndex = 0;
        
        if (uiRoot != null) uiRoot.SetActive(true);
        
        onTutorialStart?.Invoke();
        ShowCurrentStep();
    }

    private void ShowCurrentStep()
    {
        if (currentStepIndex >= steps.Count)
        {
            EndTutorial();
            return;
        }

        TutorialStep currentStep = steps[currentStepIndex];
        if (tutorialTextDisplay != null)
        {
            tutorialTextDisplay.text = currentStep.stepText;
            tutorialTextDisplay.color = originalTextColor; // 确保颜色正常
        }
        
        // 触发该步骤的专属事件（如激活特定物体的交互权限）
        currentStep.onStepStart?.Invoke();
    }

    public void NextStep()
    {
        currentStepIndex++;
        ShowCurrentStep();
    }

    /// <summary>
    /// 【外部调用】当玩家完成了正确的强制交互（例如拿起了正确的物体）时调用
    /// </summary>
    public void CompleteAction()
    {
        if (!isPlaying || currentStepIndex >= steps.Count) return;
        
        // 只有当前处于强制交互等待期，才允许通过此方法跳到下一步
        if (steps[currentStepIndex].isMandatoryInteraction)
        {
            NextStep();
        }
    }

    /// <summary>
    /// 【外部调用】当玩家交互了错误的物体，或按下了错误的按键时调用
    /// </summary>
    /// <param name="errorMsg">临时显示的警告文本</param>
    public void FailAction(string errorMsg = "操作错误，请按照提示进行操作！")
    {
        if (!isPlaying || currentStepIndex >= steps.Count) return;

        if (steps[currentStepIndex].isMandatoryInteraction)
        {
            onWrongAction?.Invoke();
            
            // 停止之前的报错协程，重新显示错误提示
            if (errorCoroutine != null) StopCoroutine(errorCoroutine);
            errorCoroutine = StartCoroutine(ShowErrorRoutine(errorMsg));
        }
    }

    private IEnumerator ShowErrorRoutine(string errorMsg)
    {
        if (tutorialTextDisplay != null)
        {
            tutorialTextDisplay.color = Color.red; // 变红警示
            tutorialTextDisplay.text = errorMsg;
            
            yield return new WaitForSeconds(1.5f); // 停留 1.5 秒
            
            // 恢复原有文本，继续等待正确交互
            tutorialTextDisplay.color = originalTextColor;
            tutorialTextDisplay.text = steps[currentStepIndex].stepText;
        }
    }

    private void EndTutorial()
    {
        isPlaying = false;
        if (uiRoot != null) uiRoot.SetActive(false);
        onTutorialFinish?.Invoke();
    }
}