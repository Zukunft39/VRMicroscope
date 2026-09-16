using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using VRMicroscope.Tutorial;

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
    RightStickRight = 16,
    LeftStickPress = 17,
    RightStickPress = 18
}

[Serializable]
public class TutorialStep
{
    public enum InputAccessMode
    {
        FullyBlocked = 0,
        LocomotionOnly = 1,
        ButtonOnly = 2,
        FullyUnblocked = 3
    }

    [TextArea(2, 5)]
    [Tooltip("Tutorial text displayed for this step.")]
    public string stepText;

    [Tooltip("Require interaction: advance only on accepted input or an external CompleteAction() call. Otherwise advance after the configured delay.")]
    public bool isMandatoryInteraction = false;

    [Tooltip("Inputs that advance a mandatory step, including buttons and stick directions. Leave empty for external CompleteAction() calls.")]
    public List<TutorialStepInputButton> acceptedButtons = new List<TutorialStepInputButton>();

    [Header("Combined Input")]
    [Tooltip("Require the modifier below to be held while performing an acceptedButtons input.")]
    public bool requireHoldButtonCombo = false;

    [Tooltip("Held modifier for combined input; acceptedButtons contains the direction/button that advances the step.")]
    public TutorialStepInputButton requiredHeldButton = TutorialStepInputButton.None;

    [Tooltip("Allowed player input for this step. ButtonOnly enables buttons/interactions without pose movement. Unmodified legacy settings use FullyBlocked.")]
    public InputAccessMode inputAccessMode = InputAccessMode.FullyBlocked;

    [Header("Event invoked when entering this step.")]
    [Tooltip("Bind step-entry behavior, such as restoring selected grabbing or button interactions for a mandatory step.")]
    public UnityEvent onStepStart;

    [Header("UI Pose Override")]
    [Tooltip("Optional fixed UI position and orientation for this step, replacing camera following.")]
    public Transform customUIAnchor;
}

public class StandaloneTutorialUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("TextMeshPro component displaying tutorial text.")]
    public TextMeshProUGUI tutorialTextDisplay;
    
    [Tooltip("Tutorial UI root, usually the Canvas first child, used for automatic visibility.")]
    public GameObject uiRoot;

    [Header("UI Behavior")]
    [Tooltip("Keep the UI following and facing the main camera.")]
    public bool alwaysFaceCamera = true;
    
    [Tooltip("Distance from the player camera; recommended 1.0-1.5.")]
    public float followDistance = 1.2f;

    [Tooltip("UI height offset; a slight downward offset such as -0.1 keeps the forward view clear.")]
    public float followHeightOffset = -0.1f;
    
    [Tooltip("UI pose-follow smoothing speed; lower values lag more (recommended 5). Zero or very large values snap immediately.")]
    public float faceCameraLerpSpeed = 5f;

    [Tooltip("Automatic advance delay for presentation steps, in seconds.")]
    public float autoAdvanceDelay = 5f;

    [Header("Render Order")]
    [Tooltip("Temporarily raise tutorial canvases above ordinary tutorial menus during mandatory steps.")]
    public bool bringTutorialToFront = true;

    [Tooltip("Temporary mandatory tutorial sorting order; larger values render in front.")]
    public int frontSortingOrder = 500;

    [Header("Mandatory Input Timing")]
    [Tooltip("Delay before accepting button/stick input after entering a configured mandatory step.")]
    public float mandatoryConfiguredInputAcceptDelay = 2f;

    [Header("Tutorial Sequence")]
    public List<TutorialStep> steps = new List<TutorialStep>();
    

    [Header("Lifecycle Events")]
    [Tooltip("Invoked on tutorial start; bind player input/movement restrictions here.")]
    public UnityEvent onTutorialStart;

    [Tooltip("Invoked after all steps finish; restore player input and hide UI here.")]
    public UnityEvent onTutorialFinish;

    [Tooltip("Invoked on incorrect input during mandatory interaction; can play an error sound.")]
    public UnityEvent onWrongAction;

    [Header("Error Feedback")]
    [Tooltip("Error-message duration. Correct input interrupts the message and advances immediately.")]
    public float errorDisplayDuration = 1.5f;

    private int currentStepIndex = 0;
    private bool isPlaying = false;
    private Coroutine errorCoroutine;
    private Color originalTextColor;
    private Transform mainCameraTransform;
    private Canvas ownerCanvas;
    private float autoAdvanceTimer = 0f;
    private float currentStepShownUnscaledTime = 0f;
    private PlayerInputBlocker playerInputBlocker;
    private Canvas[] managedCanvases = Array.Empty<Canvas>();
    private CanvasSortingState[] originalCanvasSortingStates = Array.Empty<CanvasSortingState>();
    private bool assemblyModeLockRegistered = false;

    [SerializeField, HideInInspector]
    private int inputAccessConfigVersion = 0;

    private const int CurrentInputAccessConfigVersion = 1;

    private struct CanvasSortingState
    {
        public bool overrideSorting;
        public int sortingOrder;
    }

    public bool IsPlaying => isPlaying;
    public int CurrentStepIndex => currentStepIndex;

    private void Awake()
    {
        UpgradeLegacyInputAccessModes();

        if (tutorialTextDisplay != null)
        {
            originalTextColor = tutorialTextDisplay.color;
        }

        if (uiRoot != null && !isPlaying)
        {
            uiRoot.SetActive(false);
        }

        ownerCanvas = GetComponent<Canvas>();
        if (ownerCanvas == null)
        {
            ownerCanvas = GetComponentInParent<Canvas>();
        }

        mainCameraTransform = ResolveFollowCameraTransform();

        playerInputBlocker = FindObjectOfType<PlayerInputBlocker>();
        CacheManagedCanvases();
    }

    private void OnValidate()
    {
        UpgradeLegacyInputAccessModes();
    }

    private void OnDisable()
    {
        ReleaseAssemblyModeLock();
    }

    private void Update()
    {
        if (!isPlaying || currentStepIndex >= steps.Count) return;

        Transform resolvedFollowCamera = ResolveFollowCameraTransform();
        if (resolvedFollowCamera != null)
        {
            mainCameraTransform = resolvedFollowCamera;
        }

        TutorialStep currentStep = steps[currentStepIndex];

        // 如果当前步骤配置了自定义锚点，则优先使用自定义锚点（例如固定在显微镜旁边的面板）
        if (currentStep.customUIAnchor != null && uiRoot != null)
        {
            uiRoot.transform.SetPositionAndRotation(currentStep.customUIAnchor.position, currentStep.customUIAnchor.rotation);
            return; //  使用 customUIAnchor 后，直接返回，不再执行后续的相机跟随逻辑
        }
        // 否则使用默认的相机跟随逻辑
        else if (alwaysFaceCamera && uiRoot != null && mainCameraTransform != null)
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

        if (!currentStep.isMandatoryInteraction)
        {
            autoAdvanceTimer += Time.deltaTime;
            bool pressedSpace = Input.GetKeyDown(KeyCode.Space);
            if (pressedSpace || autoAdvanceTimer >= autoAdvanceDelay)
            {
                string reason = pressedSpace ? "Keyboard Space" : $"Auto advance in {autoAdvanceDelay:0.##} seconds";
                AdvanceStep(reason);
            }
        }
    }

    private Transform ResolveFollowCameraTransform()
    {
        if (ownerCanvas == null)
        {
            ownerCanvas = GetComponent<Canvas>();
            if (ownerCanvas == null)
            {
                ownerCanvas = GetComponentInParent<Canvas>();
            }
        }

        if (ownerCanvas != null && ownerCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            Camera canvasCamera = ownerCanvas.worldCamera;
            if (canvasCamera != null && canvasCamera.gameObject.activeInHierarchy)
            {
                return canvasCamera.transform;
            }
        }

        if (Camera.main != null && Camera.main.gameObject.activeInHierarchy)
        {
            return Camera.main.transform;
        }

        if (mainCameraTransform != null && mainCameraTransform.gameObject.activeInHierarchy)
        {
            return mainCameraTransform;
        }

        return null;
    }

    public void PlayTutorial()
    {
        if (steps.Count == 0) return;
        
        isPlaying = true;
        currentStepIndex = 0;
        BringManagedCanvasesToFront();
        RegisterAssemblyModeLock();
        
        if (uiRoot != null)
        {
            uiRoot.SetActive(true);
        }

        onTutorialStart?.Invoke();
        ShowCurrentStep();
    }

    private void ShowCurrentStep()
    {
        autoAdvanceTimer = 0f;
        currentStepShownUnscaledTime = Time.unscaledTime;
        
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

        ApplyCurrentStepInputPolicy(currentStep);
        currentStep.onStepStart?.Invoke();
    }

    public void NextStep()
    {
        AdvanceStep("External NextStep() call");
    }

    public void CompleteAction()
    {
        CompleteAction("External CompleteAction() call");
    }

    public void CompleteAction(string reason)
    {
        if (!isPlaying || currentStepIndex >= steps.Count)
        {
            return;
        }
        
        if (steps[currentStepIndex].isMandatoryInteraction)
        {
            CancelErrorDisplay(restoreCurrentStepText: false);
            AdvanceStep(reason);
        }
    }

    public void FailAction(string errorMsg = "Incorrect action. Please follow the instruction.")
    {
        if (!isPlaying || currentStepIndex >= steps.Count) return;

        if (steps[currentStepIndex].isMandatoryInteraction)
        {
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
        return TryHandleConfiguredInput(input, sourceName, false);
    }

    public bool TryHandleConfiguredInput(TutorialStepInputButton input, string sourceName, bool isRequiredHoldButtonHeld)
    {
        if (!isPlaying || currentStepIndex >= steps.Count) return false;

        TutorialStep currentStep = steps[currentStepIndex];
        if (!currentStep.isMandatoryInteraction || !HasConfiguredButtonInput(currentStep))
        {
            return false;
        }

        if (!CanAcceptConfiguredInputNow())
        {
            return false;
        }

        if (StepRequiresHeldButtonCombo(currentStep, out TutorialStepInputButton requiredHeldButton) &&
            !isRequiredHoldButtonHeld)
        {
            FailAction($"Expected input for this step: {GetStepInputSummary(currentStep)}");
            return true;
        }

        if (!StepAcceptsButton(currentStep, input))
        {
            string expectedInputs = GetStepInputSummary(currentStep);
            FailAction($"Expected input for this step: {expectedInputs}");
            return true;
        }

        if (StepRequiresHeldButtonCombo(currentStep, out requiredHeldButton))
        {
            CompleteAction($"Hold {GetInputDisplayName(requiredHeldButton)} and perform {GetInputDisplayName(input)} ({sourceName})");
        }
        else
        {
            CompleteAction($"Input {GetInputDisplayName(input)} ({sourceName})");
        }

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

    public bool CurrentStepAcceptsInput(TutorialStepInputButton input)
    {
        return CurrentStepAcceptsInput(input, false);
    }

    public bool CurrentStepAcceptsInput(TutorialStepInputButton input, bool isRequiredHoldButtonHeld)
    {
        if (!isPlaying || currentStepIndex >= steps.Count)
        {
            return false;
        }

        TutorialStep currentStep = steps[currentStepIndex];
        if (StepRequiresHeldButtonCombo(currentStep, out _) && !isRequiredHoldButtonHeld)
        {
            return false;
        }

        return currentStep.isMandatoryInteraction &&
               CanAcceptConfiguredInputNow() &&
               HasConfiguredButtonInput(currentStep) &&
               StepAcceptsButton(currentStep, input);
    }

    public bool CurrentStepRequiresHeldButtonCombo(out TutorialStepInputButton requiredHeldButton)
    {
        requiredHeldButton = TutorialStepInputButton.None;

        if (!isPlaying || currentStepIndex >= steps.Count)
        {
            return false;
        }

        return StepRequiresHeldButtonCombo(steps[currentStepIndex], out requiredHeldButton);
    }

    public bool ShouldIgnoreCurrentStepModifierPress(TutorialStepInputButton input)
    {
        if (!CurrentStepRequiresHeldButtonCombo(out TutorialStepInputButton requiredHeldButton))
        {
            return false;
        }

        return input == requiredHeldButton && !StepAcceptsButton(steps[currentStepIndex], input);
    }

    public bool CanAcceptConfiguredInputNow()
    {
        if (!isPlaying || currentStepIndex >= steps.Count)
        {
            return false;
        }

        TutorialStep currentStep = steps[currentStepIndex];
        if (!currentStep.isMandatoryInteraction || !HasConfiguredButtonInput(currentStep))
        {
            return true;
        }

        float delay = Mathf.Max(0f, mandatoryConfiguredInputAcceptDelay);
        return Time.unscaledTime >= currentStepShownUnscaledTime + delay;
    }

    public bool IsWaitingForConfiguredInputDelay()
    {
        if (!isPlaying || currentStepIndex >= steps.Count)
        {
            return false;
        }

        TutorialStep currentStep = steps[currentStepIndex];
        return currentStep.isMandatoryInteraction &&
               HasConfiguredButtonInput(currentStep) &&
               !CanAcceptConfiguredInputNow();
    }

    private IEnumerator ShowErrorRoutine(string errorMsg)
    {
        if (tutorialTextDisplay != null)
        {
            tutorialTextDisplay.color = Color.red;
            tutorialTextDisplay.text = errorMsg;
            
            yield return new WaitForSeconds(Mathf.Max(0.01f, errorDisplayDuration));
            
            if (isPlaying && currentStepIndex < steps.Count)
            {
                tutorialTextDisplay.color = originalTextColor;
                tutorialTextDisplay.text = steps[currentStepIndex].stepText;
            }
        }

        errorCoroutine = null;
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
        RestoreManagedCanvasSorting();
        onTutorialFinish?.Invoke();
        ReleaseAssemblyModeLock();
    }

    private void RegisterAssemblyModeLock()
    {
        if (assemblyModeLockRegistered)
        {
            return;
        }

        MicroscopeExploderModeController.PushTutorialModeLock();
        assemblyModeLockRegistered = true;
    }

    private void ReleaseAssemblyModeLock()
    {
        if (!assemblyModeLockRegistered)
        {
            return;
        }

        MicroscopeExploderModeController.PopTutorialModeLock();
        assemblyModeLockRegistered = false;
    }

    private void AdvanceStep(string reason)
    {
        if (!isPlaying || currentStepIndex >= steps.Count) return;

        CancelErrorDisplay(restoreCurrentStepText: false);
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

    private bool StepRequiresHeldButtonCombo(TutorialStep step, out TutorialStepInputButton requiredHeldButton)
    {
        requiredHeldButton = TutorialStepInputButton.None;
        if (step == null || !step.requireHoldButtonCombo)
        {
            return false;
        }

        if (!IsButtonStyleInput(step.requiredHeldButton))
        {
            return false;
        }

        requiredHeldButton = step.requiredHeldButton;
        return true;
    }

    private void ApplyCurrentStepInputPolicy(TutorialStep step)
    {
        if (playerInputBlocker == null)
        {
            playerInputBlocker = FindObjectOfType<PlayerInputBlocker>();
        }

        if (playerInputBlocker == null)
        {
            return;
        }

        TutorialInputAccessMode accessMode = ResolveInputAccessMode(step);
        playerInputBlocker.ApplyAccessMode(accessMode);
    }

    private TutorialInputAccessMode ResolveInputAccessMode(TutorialStep step)
    {
        switch (step.inputAccessMode)
        {
            case TutorialStep.InputAccessMode.FullyBlocked:
                return TutorialInputAccessMode.FullyBlocked;
            case TutorialStep.InputAccessMode.LocomotionOnly:
                return TutorialInputAccessMode.LocomotionOnly;
            case TutorialStep.InputAccessMode.ButtonOnly:
                return TutorialInputAccessMode.ButtonOnly;
            case TutorialStep.InputAccessMode.FullyUnblocked:
                return TutorialInputAccessMode.FullyUnblocked;
            default:
                return TutorialInputAccessMode.FullyBlocked;
        }
    }

    private void UpgradeLegacyInputAccessModes()
    {
        if (inputAccessConfigVersion >= CurrentInputAccessConfigVersion || steps == null)
        {
            return;
        }

        for (int i = 0; i < steps.Count; i++)
        {
            TutorialStep step = steps[i];
            int legacyValue = (int)step.inputAccessMode;

            switch (legacyValue)
            {
                case 0: // Legacy Auto
                case 1: // Legacy FullyBlocked
                    step.inputAccessMode = TutorialStep.InputAccessMode.FullyBlocked;
                    break;
                case 2: // Legacy LocomotionOnly
                    step.inputAccessMode = TutorialStep.InputAccessMode.LocomotionOnly;
                    break;
                case 3: // Legacy ButtonOnly
                    step.inputAccessMode = TutorialStep.InputAccessMode.ButtonOnly;
                    break;
                case 4: // Legacy FullyUnblocked
                    step.inputAccessMode = TutorialStep.InputAccessMode.FullyUnblocked;
                    break;
                default:
                    step.inputAccessMode = TutorialStep.InputAccessMode.FullyBlocked;
                    break;
            }
        }

        inputAccessConfigVersion = CurrentInputAccessConfigVersion;
    }

    private void CacheManagedCanvases()
    {
        managedCanvases = GetComponentsInChildren<Canvas>(true);
        originalCanvasSortingStates = new CanvasSortingState[managedCanvases.Length];
    }

    private void BringManagedCanvasesToFront()
    {
        if (!bringTutorialToFront)
        {
            return;
        }

        if (managedCanvases == null || managedCanvases.Length == 0)
        {
            CacheManagedCanvases();
        }

        for (int i = 0; i < managedCanvases.Length; i++)
        {
            Canvas canvas = managedCanvases[i];
            if (canvas == null)
            {
                continue;
            }

            originalCanvasSortingStates[i] = new CanvasSortingState
            {
                overrideSorting = canvas.overrideSorting,
                sortingOrder = canvas.sortingOrder
            };

            canvas.overrideSorting = true;
            canvas.sortingOrder = frontSortingOrder + i;
        }
    }

    private void RestoreManagedCanvasSorting()
    {
        if (managedCanvases == null || originalCanvasSortingStates == null)
        {
            return;
        }

        int canvasCount = Mathf.Min(managedCanvases.Length, originalCanvasSortingStates.Length);
        for (int i = 0; i < canvasCount; i++)
        {
            Canvas canvas = managedCanvases[i];
            if (canvas == null)
            {
                continue;
            }

            canvas.overrideSorting = originalCanvasSortingStates[i].overrideSorting;
            canvas.sortingOrder = originalCanvasSortingStates[i].sortingOrder;
        }
    }

    private string GetStepInputSummary(TutorialStep step)
    {
        if (step.acceptedButtons == null || step.acceptedButtons.Count == 0)
        {
            return "No tutorial input configured";
        }

        List<string> buttonNames = new List<string>();
        for (int i = 0; i < step.acceptedButtons.Count; i++)
        {
            TutorialStepInputButton button = step.acceptedButtons[i];
            if (button == TutorialStepInputButton.None) continue;
            buttonNames.Add(GetInputDisplayName(button));
        }

        if (buttonNames.Count == 0)
        {
            return "No tutorial input configured";
        }

        string inputSummary = string.Join(" / ", buttonNames);
        if (StepRequiresHeldButtonCombo(step, out TutorialStepInputButton requiredHeldButton))
        {
            return $"Hold {GetInputDisplayName(requiredHeldButton)} + {inputSummary}";
        }

        return inputSummary;
    }

    private void CancelErrorDisplay(bool restoreCurrentStepText)
    {
        if (errorCoroutine == null)
        {
            return;
        }

        StopCoroutine(errorCoroutine);
        errorCoroutine = null;

        if (restoreCurrentStepText && tutorialTextDisplay != null && isPlaying && currentStepIndex < steps.Count)
        {
            tutorialTextDisplay.color = originalTextColor;
            tutorialTextDisplay.text = steps[currentStepIndex].stepText;
        }
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
                return "Left Primary Button";
            case TutorialStepInputButton.LeftSecondaryButton:
                return "Left Secondary Button";
            case TutorialStepInputButton.LeftTriggerButton:
                return "Left Trigger";
            case TutorialStepInputButton.LeftGripButton:
                return "Left Grip";
            case TutorialStepInputButton.RightPrimaryButton:
                return "Right Primary Button";
            case TutorialStepInputButton.RightSecondaryButton:
                return "Right Secondary Button";
            case TutorialStepInputButton.RightTriggerButton:
                return "Right Trigger";
            case TutorialStepInputButton.RightGripButton:
                return "Right Grip";
            case TutorialStepInputButton.LeftStickUp:
                return "Left Thumbstick Up";
            case TutorialStepInputButton.LeftStickDown:
                return "Left Thumbstick Down";
            case TutorialStepInputButton.LeftStickLeft:
                return "Left Thumbstick Left";
            case TutorialStepInputButton.LeftStickRight:
                return "Left Thumbstick Right";
            case TutorialStepInputButton.RightStickUp:
                return "Right Thumbstick Up";
            case TutorialStepInputButton.RightStickDown:
                return "Right Thumbstick Down";
            case TutorialStepInputButton.RightStickLeft:
                return "Right Thumbstick Left";
            case TutorialStepInputButton.RightStickRight:
                return "Right Thumbstick Right";
            case TutorialStepInputButton.LeftStickPress:
                return "Left Thumbstick Press";
            case TutorialStepInputButton.RightStickPress:
                return "Right Thumbstick Press";
            default:
                return "Unspecified Input";
        }
    }

    public static bool IsButtonStyleInput(TutorialStepInputButton button)
    {
        switch (button)
        {
            case TutorialStepInputButton.LeftPrimaryButton:
            case TutorialStepInputButton.LeftSecondaryButton:
            case TutorialStepInputButton.LeftTriggerButton:
            case TutorialStepInputButton.LeftGripButton:
            case TutorialStepInputButton.RightPrimaryButton:
            case TutorialStepInputButton.RightSecondaryButton:
            case TutorialStepInputButton.RightTriggerButton:
            case TutorialStepInputButton.RightGripButton:
            case TutorialStepInputButton.LeftStickPress:
            case TutorialStepInputButton.RightStickPress:
                return true;
            default:
                return false;
        }
    }
}
