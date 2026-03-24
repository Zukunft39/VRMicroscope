using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

[Serializable]
public enum TutorialStepInputButton
{
    None = 0,
    LeftPrimaryButton = 1,
    LeftSecondaryButton = 2,
    LeftTriggerButton = 3,
    LeftGripButton = 4,
    RightPrimaryButton = 5,
    RightSecondaryButton = 6,
    RightTriggerButton = 7,
    RightGripButton = 8,
    LeftStickUp = 9,
    LeftStickDown = 10,
    LeftStickLeft = 11,
    LeftStickRight = 12,
    RightStickUp = 13,
    RightStickDown = 14,
    RightStickLeft = 15,
    RightStickRight = 16
}

[Serializable]
public class TutorialStep
{
    [TextArea(2, 5)]
    [Tooltip("当前步骤要显示的教程文本")]
    public string stepText;

    [Tooltip("是否为【强制交互】步骤？\n如果勾选：该步骤不会自动推进，必须由正确输入或外部交互后调用 CompleteAction()。\n如果不勾选：该步骤会在等待一段时间后自动进入下一步。")]
    public bool isMandatoryInteraction = false;

    [Tooltip("当步骤为【强制交互】时，可在这里直接配置允许推进教程的输入。\n支持按钮与摇杆方向。\n留空表示该步骤仍由外部交互脚本手动调用 CompleteAction()。")]
    public List<TutorialStepInputButton> acceptedButtons = new List<TutorialStepInputButton>();

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

    [Header("UI 行为")]
    [Tooltip("是否让 UI 始终跟随并朝向主摄像机？")]
    public bool alwaysFaceCamera = true;
    
    [Tooltip("UI 距离玩家摄像机的保持距离（建议设为 1.0 ~ 1.5）")]
    public float followDistance = 1.2f;

    [Tooltip("UI 的高度偏移（建议稍微偏下一点，如 -0.1，避免完全挡住正前方视线）")]
    public float followHeightOffset = -0.1f;
    
    [Tooltip("UI 旋转和位置跟随的平滑速度（值越小跟随越有延迟感，建议设为 5）\n如果设为 0 或者极大，则等同于瞬间硬切。")]
    public float faceCameraLerpSpeed = 5f;

    [Tooltip("非强制交互步骤自动进入下一步的等待时间（秒）")]
    public float autoAdvanceDelay = 5f;

    [Header("教程流程配置")]
    public List<TutorialStep> steps = new List<TutorialStep>();
    

    [Header("全局生命周期事件")]
    [Tooltip("教程开始时触发（在这里挂载：完全禁用玩家所有操作/移动）")]
    public UnityEvent onTutorialStart;

    [Tooltip("教程全部结束时触发（在这里挂载：恢复玩家所有操作、隐藏UI等）")]
    public UnityEvent onTutorialFinish;

    [Tooltip("玩家在强制交互阶段执行了【错误操作】时触发（可配置播放错误提示音等）")]
    public UnityEvent onWrongAction;

    [Header("Debug")]
    [Tooltip("是否输出教程步骤切换与输入匹配日志")]
    public bool enableDebugLogs = true;

    private int currentStepIndex = 0;
    private bool isPlaying = false;
    private Coroutine errorCoroutine;
    private Color originalTextColor;
    private Transform mainCameraTransform;
    private float autoAdvanceTimer = 0f;

    public bool IsPlaying => isPlaying;
    public int CurrentStepIndex => currentStepIndex;

    private void Awake()
    {
        if (tutorialTextDisplay != null)
        {
            originalTextColor = tutorialTextDisplay.color;
        }

        if (uiRoot != null && !isPlaying)
        {
            uiRoot.SetActive(false);
        }
        
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (!isPlaying || currentStepIndex >= steps.Count) return;

        if (mainCameraTransform == null && Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }

        if (alwaysFaceCamera && uiRoot != null && mainCameraTransform != null)
        {
            Vector3 targetPosition = mainCameraTransform.position + mainCameraTransform.forward * followDistance + Vector3.up * followHeightOffset;
            Quaternion targetRotation = mainCameraTransform.rotation;

            if (faceCameraLerpSpeed > 0.01f)
            {
                uiRoot.transform.position = Vector3.Lerp(uiRoot.transform.position, targetPosition, Time.deltaTime * faceCameraLerpSpeed);
                uiRoot.transform.rotation = Quaternion.Slerp(uiRoot.transform.rotation, targetRotation, Time.deltaTime * faceCameraLerpSpeed);
            }
            else
            {
                uiRoot.transform.position = targetPosition;
                uiRoot.transform.rotation = targetRotation;
            }
        }

        if (!steps[currentStepIndex].isMandatoryInteraction)
        {
            autoAdvanceTimer += Time.deltaTime;
            bool pressedSpace = Input.GetKeyDown(KeyCode.Space);
            if (pressedSpace || autoAdvanceTimer >= autoAdvanceDelay)
            {
                string reason = pressedSpace ? "键盘 Space" : $"自动倒计时 {autoAdvanceDelay:0.##} 秒";
                AdvanceStep(reason);
            }
        }
    }

    public void PlayTutorial()
    {
        if (steps.Count == 0) return;
        
        isPlaying = true;
        currentStepIndex = 0;
        
        if (uiRoot != null)
        {
            uiRoot.SetActive(true);
        }

        LogDebug("教程开始。");
        onTutorialStart?.Invoke();
        ShowCurrentStep();
    }

    private void ShowCurrentStep()
    {
        autoAdvanceTimer = 0f;
        
        if (currentStepIndex >= steps.Count)
        {
            EndTutorial();
            return;
        }

        TutorialStep currentStep = steps[currentStepIndex];
        if (tutorialTextDisplay != null)
        {
            tutorialTextDisplay.text = currentStep.stepText;
            tutorialTextDisplay.color = originalTextColor;
        }

        LogDebug($"进入步骤 {currentStepIndex + 1}/{steps.Count}: {BuildStepDebugSummary(currentStep)}");
        currentStep.onStepStart?.Invoke();
    }

    public void NextStep()
    {
        AdvanceStep("外部调用 NextStep()");
    }

    public void CompleteAction()
    {
        CompleteAction("外部脚本调用 CompleteAction()");
    }

    public void CompleteAction(string reason)
    {
        if (!isPlaying || currentStepIndex >= steps.Count)
        {
            LogDebug("忽略 CompleteAction()，当前教程未播放或已经结束。");
            return;
        }
        
        if (steps[currentStepIndex].isMandatoryInteraction)
        {
            AdvanceStep(reason);
        }
    }

    public void FailAction(string errorMsg = "操作错误，请按照提示进行操作！")
    {
        if (!isPlaying || currentStepIndex >= steps.Count) return;

        if (steps[currentStepIndex].isMandatoryInteraction)
        {
            LogDebug($"步骤 {currentStepIndex + 1} 操作失败: {errorMsg}");
            onWrongAction?.Invoke();
            
            if (errorCoroutine != null) StopCoroutine(errorCoroutine);
            errorCoroutine = StartCoroutine(ShowErrorRoutine(errorMsg));
        }
    }

    public bool TryHandleConfiguredButtonInput(TutorialStepInputButton button, string sourceName)
    {
        return TryHandleConfiguredInput(button, sourceName);
    }

    public bool TryHandleConfiguredInput(TutorialStepInputButton input, string sourceName)
    {
        if (!isPlaying || currentStepIndex >= steps.Count) return false;

        TutorialStep currentStep = steps[currentStepIndex];
        if (!currentStep.isMandatoryInteraction || !HasConfiguredButtonInput(currentStep))
        {
            return false;
        }

        if (!StepAcceptsButton(currentStep, input))
        {
            string expectedInputs = GetStepInputSummary(currentStep);
            LogDebug($"步骤 {currentStepIndex + 1} 收到未匹配输入: {GetInputDisplayName(input)}，来源: {sourceName}，期望: {expectedInputs}");
            FailAction($"当前步骤需要执行输入：{expectedInputs}");
            return true;
        }

        CompleteAction($"输入 {GetInputDisplayName(input)} ({sourceName})");
        return true;
    }

    public string GetCurrentStepInputSummary()
    {
        if (!isPlaying || currentStepIndex >= steps.Count)
        {
            return string.Empty;
        }

        return GetStepInputSummary(steps[currentStepIndex]);
    }

    private IEnumerator ShowErrorRoutine(string errorMsg)
    {
        if (tutorialTextDisplay != null)
        {
            tutorialTextDisplay.color = Color.red;
            tutorialTextDisplay.text = errorMsg;
            
            yield return new WaitForSeconds(1.5f);
            
            if (isPlaying && currentStepIndex < steps.Count)
            {
                tutorialTextDisplay.color = originalTextColor;
                tutorialTextDisplay.text = steps[currentStepIndex].stepText;
            }
        }
    }

    private void EndTutorial()
    {
        isPlaying = false;

        if (errorCoroutine != null)
        {
            StopCoroutine(errorCoroutine);
            errorCoroutine = null;
        }

        if (uiRoot != null) uiRoot.SetActive(false);
        LogDebug("教程结束。");
        onTutorialFinish?.Invoke();
    }

    private void AdvanceStep(string reason)
    {
        if (!isPlaying || currentStepIndex >= steps.Count) return;

        LogDebug($"完成步骤 {currentStepIndex + 1}/{steps.Count}，原因: {reason}");
        currentStepIndex++;
        ShowCurrentStep();
    }

    private bool HasConfiguredButtonInput(TutorialStep step)
    {
        return step.acceptedButtons != null && step.acceptedButtons.Count > 0;
    }

    private bool StepAcceptsButton(TutorialStep step, TutorialStepInputButton button)
    {
        if (step.acceptedButtons == null) return false;

        for (int i = 0; i < step.acceptedButtons.Count; i++)
        {
            if (step.acceptedButtons[i] == button)
            {
                return true;
            }
        }

        return false;
    }

    private string BuildStepDebugSummary(TutorialStep step)
    {
        if (!step.isMandatoryInteraction)
        {
            return $"普通步骤，可按 Space 跳过，或在 {autoAdvanceDelay:0.##} 秒后自动推进。";
        }

        if (!HasConfiguredButtonInput(step))
        {
            return "强制交互步骤，等待外部脚本调用 CompleteAction()。";
        }

        return $"强制输入步骤，允许输入: {GetStepInputSummary(step)}";
    }

    private string GetStepInputSummary(TutorialStep step)
    {
        if (step.acceptedButtons == null || step.acceptedButtons.Count == 0)
        {
            return "未配置教程输入";
        }

        List<string> buttonNames = new List<string>();
        for (int i = 0; i < step.acceptedButtons.Count; i++)
        {
            TutorialStepInputButton button = step.acceptedButtons[i];
            if (button == TutorialStepInputButton.None) continue;
            buttonNames.Add(GetInputDisplayName(button));
        }

        return buttonNames.Count == 0 ? "未配置教程输入" : string.Join(" / ", buttonNames);
    }

    private void LogDebug(string message)
    {
        if (!enableDebugLogs) return;
        Debug.Log($"[ForceTutorial/UI] {message}");
    }

    public static string GetButtonDisplayName(TutorialStepInputButton button)
    {
        return GetInputDisplayName(button);
    }

    public static string GetInputDisplayName(TutorialStepInputButton button)
    {
        switch (button)
        {
            case TutorialStepInputButton.LeftPrimaryButton:
                return "左手 Primary";
            case TutorialStepInputButton.LeftSecondaryButton:
                return "左手 Secondary";
            case TutorialStepInputButton.LeftTriggerButton:
                return "左手 Trigger";
            case TutorialStepInputButton.LeftGripButton:
                return "左手 Grip";
            case TutorialStepInputButton.RightPrimaryButton:
                return "右手 Primary";
            case TutorialStepInputButton.RightSecondaryButton:
                return "右手 Secondary";
            case TutorialStepInputButton.RightTriggerButton:
                return "右手 Trigger";
            case TutorialStepInputButton.RightGripButton:
                return "右手 Grip";
            case TutorialStepInputButton.LeftStickUp:
                return "左手摇杆向上";
            case TutorialStepInputButton.LeftStickDown:
                return "左手摇杆向下";
            case TutorialStepInputButton.LeftStickLeft:
                return "左手摇杆向左";
            case TutorialStepInputButton.LeftStickRight:
                return "左手摇杆向右";
            case TutorialStepInputButton.RightStickUp:
                return "右手摇杆向上";
            case TutorialStepInputButton.RightStickDown:
                return "右手摇杆向下";
            case TutorialStepInputButton.RightStickLeft:
                return "右手摇杆向左";
            case TutorialStepInputButton.RightStickRight:
                return "右手摇杆向右";
            default:
                return "未指定输入";
        }
    }
}
