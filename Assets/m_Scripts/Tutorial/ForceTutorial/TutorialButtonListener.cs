using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;

namespace VRMicroscope.Tutorial
{
    /// <summary>
    /// 负责监听玩家的 VR 手柄输入，
    /// 并根据当前教程步骤配置决定是否推进教程。
    /// </summary>
    public class TutorialButtonListener : MonoBehaviour
    {
        private const string LeftPrimaryButtonActionName = "LeftPrimaryButton";
        private const string LeftSecondaryButtonActionName = "LeftSecondaryButton";
        private const string LeftTriggerButtonActionName = "LeftTriggerButton";
        private const string LeftGripButtonActionName = "LeftGripButton";
        private const string RightPrimaryButtonActionName = "RightPrimaryButton";
        private const string RightSecondaryButtonActionName = "RightSecondaryButton";
        private const string RightTriggerButtonActionName = "RightTriggerButton";
        private const string RightGripButtonActionName = "RightGripButton";
        private const string LeftStickActionName = "LeftStick";
        private const string RightStickActionName = "RightStick";

        [Header("核心引用")]
        [Tooltip("需要推进的教程 UI 界面实例")]
        public StandaloneTutorialUI tutorialUI;

        [Header("输入资源")]
        [Tooltip("拖入 ForceTutorial.inputactions 资源，脚本会自动读取里面的按钮与摇杆 Action。")]
        public InputActionAsset tutorialInputActions;

        [Header("通用设置")]
        [Tooltip("组件启用后，延迟多久再开始监听输入，避免刚启用时误触发")]
        public float initialInputDelay = 0.5f;

        [Tooltip("每次成功处理一个教程输入后，等待多久再允许处理下一次输入")]
        public float inputCooldown = 0.15f;

        [Tooltip("摇杆方向判定阈值，超过这个值才视为一次有效拨动")]
        [Range(0.1f, 1f)]
        public float stickThreshold = 0.75f;

        [Tooltip("是否输出教程输入监听日志")]
        public bool enableDebugLogs = true;

        [Header("教程反馈增强")]
        [Tooltip("当玩家在强制教程中输入正确的移动/转向操作时，自动追加一小段位移或转向，让反馈更明显。")]
        public bool enableLocomotionFeedbackAssist = true;

        [Tooltip("移动教学步骤中，额外追加的位移距离。")]
        [Min(0f)]
        public float movementAssistDistance = 0.4f;

        [Tooltip("移动教学反馈持续时长。值越小，额外位移越利落。")]
        [Min(0.01f)]
        public float movementAssistDuration = 0.18f;

        [Tooltip("转向教学步骤中，额外追加的转向角度。")]
        [Min(0f)]
        public float turnAssistAngle = 25f;

        [Tooltip("转向教学反馈持续时长。")]
        [Min(0.01f)]
        public float turnAssistDuration = 0.12f;

        private bool isInputActive = false;
        private float nextAllowedInputTime = 0f;
        private Coroutine enableDelayCoroutine;
        private Coroutine locomotionAssistCoroutine;
        private TutorialStepInputButton lastLeftStickDirection = TutorialStepInputButton.None;
        private TutorialStepInputButton lastRightStickDirection = TutorialStepInputButton.None;
        private readonly Dictionary<TutorialStepInputButton, InputAction> buttonActions = new Dictionary<TutorialStepInputButton, InputAction>();
        private InputAction leftStickAction;
        private InputAction rightStickAction;
        private XROrigin xrOrigin;

        private static readonly TutorialStepInputButton[] ButtonInputCheckOrder =
        {
            TutorialStepInputButton.LeftPrimaryButton,
            TutorialStepInputButton.LeftSecondaryButton,
            TutorialStepInputButton.LeftTriggerButton,
            TutorialStepInputButton.LeftGripButton,
            TutorialStepInputButton.RightPrimaryButton,
            TutorialStepInputButton.RightSecondaryButton,
            TutorialStepInputButton.RightTriggerButton,
            TutorialStepInputButton.RightGripButton
        };

        private void OnEnable()
        {
            isInputActive = false;
            lastLeftStickDirection = TutorialStepInputButton.None;
            lastRightStickDirection = TutorialStepInputButton.None;

            CacheXROrigin();
            CacheActionsFromAsset();
            EnableCachedActions();

            if (enableDelayCoroutine != null)
            {
                StopCoroutine(enableDelayCoroutine);
            }

            enableDelayCoroutine = StartCoroutine(EnableInputDelay());
        }

        private void OnDisable()
        {
            isInputActive = false;
            lastLeftStickDirection = TutorialStepInputButton.None;
            lastRightStickDirection = TutorialStepInputButton.None;

            if (enableDelayCoroutine != null)
            {
                StopCoroutine(enableDelayCoroutine);
                enableDelayCoroutine = null;
            }

            if (locomotionAssistCoroutine != null)
            {
                StopCoroutine(locomotionAssistCoroutine);
                locomotionAssistCoroutine = null;
            }

            DisableCachedActions();
        }

        private IEnumerator EnableInputDelay()
        {
            float safeDelay = Mathf.Max(0f, initialInputDelay);
            if (safeDelay > 0f)
            {
                yield return new WaitForSeconds(safeDelay);
            }

            isInputActive = true;
            nextAllowedInputTime = 0f;
            enableDelayCoroutine = null;
            LogDebug("教程输入监听已启用。");
        }

        private void Update()
        {
            if (tutorialUI == null || !isInputActive || Time.unscaledTime < nextAllowedInputTime) return;

            if (TryGetPressedInput(out TutorialStepInputButton input, out string source))
            {
                bool willAdvanceCurrentStep = tutorialUI.CurrentStepAcceptsInput(input);
                if (tutorialUI.TryHandleConfiguredInput(input, source))
                {
                    if (willAdvanceCurrentStep)
                    {
                        TriggerLocomotionAssist(input);
                    }

                    nextAllowedInputTime = Time.unscaledTime + Mathf.Max(0f, inputCooldown);
                    LogDebug($"已处理教程输入: {StandaloneTutorialUI.GetInputDisplayName(input)}，来源: {source}");
                }
            }
        }

        private bool TryGetPressedInput(out TutorialStepInputButton input, out string source)
        {
            for (int i = 0; i < ButtonInputCheckOrder.Length; i++)
            {
                TutorialStepInputButton buttonType = ButtonInputCheckOrder[i];
                if (WasButtonPressed(buttonType, out input, out source))
                {
                    return true;
                }
            }

            if (WasStickDirectionTriggered(leftStickAction, true, out input, out source)) return true;
            if (WasStickDirectionTriggered(rightStickAction, false, out input, out source)) return true;

            input = TutorialStepInputButton.None;
            source = string.Empty;
            return false;
        }

        private bool WasButtonPressed(
            TutorialStepInputButton button,
            out TutorialStepInputButton pressedButton,
            out string source)
        {
            InputAction action;
            if (buttonActions.TryGetValue(button, out action) && action != null && action.WasPressedThisFrame())
            {
                pressedButton = button;
                source = $"VR:{StandaloneTutorialUI.GetInputDisplayName(button)}";
                return true;
            }

            pressedButton = TutorialStepInputButton.None;
            source = string.Empty;
            return false;
        }

        private bool WasStickDirectionTriggered(
            InputAction action,
            bool isLeftStick,
            out TutorialStepInputButton pressedButton,
            out string source)
        {
            if (action == null)
            {
                pressedButton = TutorialStepInputButton.None;
                source = string.Empty;
                return false;
            }

            Vector2 value = action.ReadValue<Vector2>();
            TutorialStepInputButton currentDirection = GetStickDirection(value, isLeftStick);
            TutorialStepInputButton lastDirection = isLeftStick ? lastLeftStickDirection : lastRightStickDirection;

            if (isLeftStick)
            {
                lastLeftStickDirection = currentDirection;
            }
            else
            {
                lastRightStickDirection = currentDirection;
            }

            if (currentDirection != TutorialStepInputButton.None && currentDirection != lastDirection)
            {
                pressedButton = currentDirection;
                source = $"VR:{StandaloneTutorialUI.GetInputDisplayName(currentDirection)}";
                return true;
            }

            pressedButton = TutorialStepInputButton.None;
            source = string.Empty;
            return false;
        }

        private TutorialStepInputButton GetStickDirection(Vector2 value, bool isLeftStick)
        {
            float threshold = Mathf.Clamp(stickThreshold, 0.1f, 1f);

            if (Mathf.Abs(value.x) >= Mathf.Abs(value.y))
            {
                if (value.x >= threshold)
                {
                    return isLeftStick ? TutorialStepInputButton.LeftStickRight : TutorialStepInputButton.RightStickRight;
                }

                if (value.x <= -threshold)
                {
                    return isLeftStick ? TutorialStepInputButton.LeftStickLeft : TutorialStepInputButton.RightStickLeft;
                }
            }
            else
            {
                if (value.y >= threshold)
                {
                    return isLeftStick ? TutorialStepInputButton.LeftStickUp : TutorialStepInputButton.RightStickUp;
                }

                if (value.y <= -threshold)
                {
                    return isLeftStick ? TutorialStepInputButton.LeftStickDown : TutorialStepInputButton.RightStickDown;
                }
            }

            return TutorialStepInputButton.None;
        }

        private void CacheActionsFromAsset()
        {
            buttonActions.Clear();
            leftStickAction = null;
            rightStickAction = null;

            if (tutorialInputActions == null)
            {
                Debug.LogWarning("[ForceTutorial/Input] 未配置 tutorialInputActions，无法监听 VR 输入。");
                return;
            }

            CacheButtonAction(TutorialStepInputButton.LeftPrimaryButton, LeftPrimaryButtonActionName);
            CacheButtonAction(TutorialStepInputButton.LeftSecondaryButton, LeftSecondaryButtonActionName);
            CacheButtonAction(TutorialStepInputButton.LeftTriggerButton, LeftTriggerButtonActionName);
            CacheButtonAction(TutorialStepInputButton.LeftGripButton, LeftGripButtonActionName);
            CacheButtonAction(TutorialStepInputButton.RightPrimaryButton, RightPrimaryButtonActionName);
            CacheButtonAction(TutorialStepInputButton.RightSecondaryButton, RightSecondaryButtonActionName);
            CacheButtonAction(TutorialStepInputButton.RightTriggerButton, RightTriggerButtonActionName);
            CacheButtonAction(TutorialStepInputButton.RightGripButton, RightGripButtonActionName);

            leftStickAction = tutorialInputActions.FindAction(LeftStickActionName, false);
            rightStickAction = tutorialInputActions.FindAction(RightStickActionName, false);

            if (leftStickAction == null)
            {
                Debug.LogWarning($"[ForceTutorial/Input] 在输入资源中未找到 Action: {LeftStickActionName}");
            }

            if (rightStickAction == null)
            {
                Debug.LogWarning($"[ForceTutorial/Input] 在输入资源中未找到 Action: {RightStickActionName}");
            }
        }

        private void CacheXROrigin()
        {
            if (xrOrigin == null)
            {
                xrOrigin = FindObjectOfType<XROrigin>();
            }
        }

        private void CacheButtonAction(TutorialStepInputButton button, string actionName)
        {
            InputAction action = tutorialInputActions.FindAction(actionName, false);
            if (action == null)
            {
                Debug.LogWarning($"[ForceTutorial/Input] 在输入资源中未找到 Action: {actionName}");
                return;
            }

            buttonActions[button] = action;
        }

        private void EnableCachedActions()
        {
            foreach (KeyValuePair<TutorialStepInputButton, InputAction> pair in buttonActions)
            {
                if (pair.Value != null && !pair.Value.enabled)
                {
                    pair.Value.Enable();
                }
            }

            if (leftStickAction != null && !leftStickAction.enabled)
            {
                leftStickAction.Enable();
            }

            if (rightStickAction != null && !rightStickAction.enabled)
            {
                rightStickAction.Enable();
            }
        }

        private void DisableCachedActions()
        {
            foreach (KeyValuePair<TutorialStepInputButton, InputAction> pair in buttonActions)
            {
                if (pair.Value != null && pair.Value.enabled)
                {
                    pair.Value.Disable();
                }
            }

            if (leftStickAction != null && leftStickAction.enabled)
            {
                leftStickAction.Disable();
            }

            if (rightStickAction != null && rightStickAction.enabled)
            {
                rightStickAction.Disable();
            }
        }

        private void TriggerLocomotionAssist(TutorialStepInputButton input)
        {
            if (!enableLocomotionFeedbackAssist)
            {
                return;
            }

            CacheXROrigin();
            if (xrOrigin == null)
            {
                LogDebug("未找到 XROrigin，已跳过教程反馈增强。");
                return;
            }

            if (TryGetMovementAssistDirection(input, out Vector3 worldDirection))
            {
                StartLocomotionAssist(MoveAssistRoutine(worldDirection));
                return;
            }

            if (TryGetTurnAssistAngle(input, out float signedAngle))
            {
                StartLocomotionAssist(TurnAssistRoutine(signedAngle));
            }
        }

        private void StartLocomotionAssist(IEnumerator routine)
        {
            if (locomotionAssistCoroutine != null)
            {
                StopCoroutine(locomotionAssistCoroutine);
            }

            locomotionAssistCoroutine = StartCoroutine(routine);
        }

        private bool TryGetMovementAssistDirection(TutorialStepInputButton input, out Vector3 worldDirection)
        {
            worldDirection = Vector3.zero;

            if (input != TutorialStepInputButton.LeftStickUp &&
                input != TutorialStepInputButton.LeftStickDown &&
                input != TutorialStepInputButton.LeftStickLeft &&
                input != TutorialStepInputButton.LeftStickRight)
            {
                return false;
            }

            Transform referenceTransform = xrOrigin != null && xrOrigin.Camera != null
                ? xrOrigin.Camera.transform
                : transform;

            Vector3 forward = Vector3.ProjectOnPlane(referenceTransform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            Vector3 right = Vector3.ProjectOnPlane(referenceTransform.right, Vector3.up).normalized;
            if (right.sqrMagnitude < 0.0001f)
            {
                right = Vector3.right;
            }

            switch (input)
            {
                case TutorialStepInputButton.LeftStickUp:
                    worldDirection = forward;
                    return true;
                case TutorialStepInputButton.LeftStickDown:
                    worldDirection = -forward;
                    return true;
                case TutorialStepInputButton.LeftStickLeft:
                    worldDirection = -right;
                    return true;
                case TutorialStepInputButton.LeftStickRight:
                    worldDirection = right;
                    return true;
                default:
                    return false;
            }
        }

        private bool TryGetTurnAssistAngle(TutorialStepInputButton input, out float signedAngle)
        {
            switch (input)
            {
                case TutorialStepInputButton.RightStickLeft:
                    signedAngle = -Mathf.Abs(turnAssistAngle);
                    return true;
                case TutorialStepInputButton.RightStickRight:
                    signedAngle = Mathf.Abs(turnAssistAngle);
                    return true;
                default:
                    signedAngle = 0f;
                    return false;
            }
        }

        private IEnumerator MoveAssistRoutine(Vector3 worldDirection)
        {
            if (xrOrigin == null || xrOrigin.Camera == null || movementAssistDistance <= 0f)
            {
                locomotionAssistCoroutine = null;
                yield break;
            }

            Vector3 startCameraPosition = xrOrigin.Camera.transform.position;
            Vector3 targetCameraPosition = startCameraPosition + worldDirection.normalized * movementAssistDistance;
            float duration = Mathf.Max(0.01f, movementAssistDuration);
            float elapsed = 0f;

            LogDebug($"执行移动反馈增强，方向: {worldDirection}, 距离: {movementAssistDistance:0.##}");

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
                Vector3 currentTargetPosition = Vector3.Lerp(startCameraPosition, targetCameraPosition, easedProgress);
                xrOrigin.MoveCameraToWorldLocation(currentTargetPosition);
                yield return null;
            }

            xrOrigin.MoveCameraToWorldLocation(targetCameraPosition);
            locomotionAssistCoroutine = null;
        }

        private IEnumerator TurnAssistRoutine(float signedAngle)
        {
            if (xrOrigin == null || Mathf.Approximately(signedAngle, 0f))
            {
                locomotionAssistCoroutine = null;
                yield break;
            }

            float duration = Mathf.Max(0.01f, turnAssistDuration);
            float elapsed = 0f;
            float appliedAngle = 0f;

            LogDebug($"执行转向反馈增强，角度: {signedAngle:0.##}");

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
                float currentAngle = Mathf.Lerp(0f, signedAngle, easedProgress);
                float deltaAngle = currentAngle - appliedAngle;
                appliedAngle = currentAngle;
                xrOrigin.RotateAroundCameraUsingOriginUp(deltaAngle);
                yield return null;
            }

            float finalDelta = signedAngle - appliedAngle;
            if (!Mathf.Approximately(finalDelta, 0f))
            {
                xrOrigin.RotateAroundCameraUsingOriginUp(finalDelta);
            }

            locomotionAssistCoroutine = null;
        }

        private void LogDebug(string message)
        {
            if (!enableDebugLogs) return;
            Debug.Log($"[ForceTutorial/Input] {message}");
        }
    }
}
