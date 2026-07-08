using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraTryMove : MonoBehaviour
{
    [Header("PC Movement")]
    public bool canMove = true;
    public float moveSpeed = 5f;
    public float fastMoveMultiplier = 2f;
    public float rotationSpeed = 2f;
    public float keyboardTurnSpeed = 75f;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode moveUpKey = KeyCode.E;
    public KeyCode moveDownKey = KeyCode.Q;

    [Header("Mouse Look")]
    public int lookMouseButton = 1;
    public bool lockCursorWhileLooking = true;
    public bool hideCursorWhileLooking = true;

    [Header("Desktop Ray")]
    public int primaryClickMouseButton = 0;
    public float desktopRayMaxDistance = 100f;
    public LayerMask desktopRaycastMask = ~0;
    public LayerMask teleportSurfaceMask = ~0;
    public float maxTeleportSurfaceAngle = 60f;

    [Header("VR Button Mapping")]
    public KeyCode leftPrimaryButtonKey = KeyCode.X;
    public KeyCode leftSecondaryButtonKey = KeyCode.Y;
    public KeyCode leftTriggerButtonKey = KeyCode.Z;
    public KeyCode leftGripButtonKey = KeyCode.C;
    public KeyCode rightPrimaryButtonKey = KeyCode.R;
    public KeyCode rightSecondaryButtonKey = KeyCode.B;
    public KeyCode rightGripButtonKey = KeyCode.G;
    public KeyCode leftStickPressKey = KeyCode.V;
    public KeyCode rightStickPressKey = KeyCode.Tab;

    [Header("Stick Mapping")]
    public KeyCode leftStickUpKey = KeyCode.W;
    public KeyCode leftStickDownKey = KeyCode.S;
    public KeyCode leftStickLeftKey = KeyCode.A;
    public KeyCode leftStickRightKey = KeyCode.D;
    public KeyCode rightStickUpKey = KeyCode.UpArrow;
    public KeyCode rightStickDownKey = KeyCode.DownArrow;
    public KeyCode rightStickLeftKey = KeyCode.LeftArrow;
    public KeyCode rightStickRightKey = KeyCode.RightArrow;

    [Header("PC Debug Actions")]
    public KeyCode openTutorialKey = KeyCode.Y;
    public KeyCode confirmTutorialKey = KeyCode.Space;
    public KeyCode alternateConfirmTutorialKey = KeyCode.Return;
    public KeyCode quitObserveKey = KeyCode.Escape;

    private const float TutorialLookupInterval = 0.25f;

    private static CameraTryMove activeDesktopController;

    private float pitch;
    private float yaw;
    private float nextTutorialLookupTime;
    private StandaloneTutorialUI cachedVisibleForceTutorial;
    private Interactor cachedInteractor;
    private XROrigin cachedXrOrigin;
    private Camera cachedCamera;

    public static bool TryGetDesktopPointerRay(out Ray ray, out float maxDistance)
    {
        ray = default;
        maxDistance = 0f;

        if (activeDesktopController == null || !activeDesktopController.isActiveAndEnabled)
        {
            return false;
        }

        return activeDesktopController.TryBuildPointerRay(out ray, out maxDistance);
    }

    public static bool IsDesktopPointerOverUi()
    {
        return activeDesktopController != null && activeDesktopController.IsPointerOverUi();
    }

    private void OnEnable()
    {
        activeDesktopController = this;
        CacheReferences();
        SyncEulerAnglesFromTransform();
    }

    private void OnDisable()
    {
        if (activeDesktopController == this)
        {
            activeDesktopController = null;
        }

        SetCursorLookMode(false);
    }

    private void Start()
    {
        CacheReferences();
        SyncEulerAnglesFromTransform();
        SetCursorLookMode(false);
    }

    private void Update()
    {
        CacheReferences();

        StandaloneTutorialUI forceTutorial = GetVisibleForceTutorial();
        bool suppressGameplayThisFrame = false;

        HandleForceTutorialDiscreteInput(forceTutorial, ref suppressGameplayThisFrame);
        HandleGlobalTutorialInput(forceTutorial, suppressGameplayThisFrame);

        if (HandleTutorialMenuInput())
        {
            SetCursorLookMode(false);
            return;
        }

        HandleMappedGameplayInput(forceTutorial, suppressGameplayThisFrame);
        HandleContinuousFocusLightInput(forceTutorial);

        bool canUseFreeMovement = CanUseFreeMovement(forceTutorial);
        bool canUseMouseLook = canUseFreeMovement && IsMouseButtonHeld(lookMouseButton);
        SetCursorLookMode(canUseMouseLook);

        if (canUseFreeMovement)
        {
            MoveCamera();
        }

        if (canUseMouseLook)
        {
            RotateCameraWithMouse();
        }

        ApplyKeyboardTurnInput(forceTutorial);
    }

    private void CacheReferences()
    {
        if (cachedInteractor == null)
        {
            cachedInteractor = Interactor.Instance != null ? Interactor.Instance : FindObjectOfType<Interactor>();
        }

        if (cachedXrOrigin == null)
        {
            cachedXrOrigin = GetComponent<XROrigin>();
            if (cachedXrOrigin == null)
            {
                cachedXrOrigin = FindObjectOfType<XROrigin>();
            }
        }

        if (cachedCamera == null || !cachedCamera.gameObject.activeInHierarchy)
        {
            cachedCamera = ResolveActiveCamera();
        }
    }

    private Camera ResolveActiveCamera()
    {
        if (ProgressControl.Instance != null &&
            ProgressControl.Instance.cinemachineBrain != null &&
            ProgressControl.Instance.cinemachineBrain.OutputCamera != null)
        {
            return ProgressControl.Instance.cinemachineBrain.OutputCamera;
        }

        if (cachedXrOrigin != null && cachedXrOrigin.Camera != null)
        {
            return cachedXrOrigin.Camera;
        }

        return Camera.main;
    }

    private void SyncEulerAnglesFromTransform()
    {
        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = NormalizeAngle(euler.x);
    }

    private float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }

    private void SetCursorLookMode(bool isLooking)
    {
        if (isLooking && lockCursorWhileLooking)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = !hideCursorWhileLooking;
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private bool CanUseFreeMovement(StandaloneTutorialUI forceTutorial)
    {
        if (!canMove)
        {
            return false;
        }

        if (cachedInteractor != null && cachedInteractor.CurrentState != Interactor.GameState.Roaming)
        {
            return false;
        }

        if (MicroscopeExploderModeController.Instance != null &&
            MicroscopeExploderModeController.Instance.CurrentMode != MicroscopeExploderModeController.AssemblyMode.Normal)
        {
            return false;
        }

        if (forceTutorial == null)
        {
            return true;
        }

        Vector2 leftStick = GetLeftStickVectorHeld();
        if (leftStick == Vector2.zero)
        {
            return false;
        }

        return ForceTutorialAcceptsLeftStickInput(forceTutorial, leftStick);
    }

    private bool HandleTutorialMenuInput()
    {
        if (cachedInteractor == null || cachedInteractor.CurrentState != Interactor.GameState.Tutorial)
        {
            return false;
        }

        TutorialButtonInput tutorialInput = cachedInteractor.tutorialButtonInput;
        if (tutorialInput == null)
        {
            return true;
        }

        Vector2 navigation = Vector2.zero;
        if (IsKeyHeld(leftStickUpKey) || IsKeyHeld(rightStickUpKey)) navigation.y += 1f;
        if (IsKeyHeld(leftStickDownKey) || IsKeyHeld(rightStickDownKey)) navigation.y -= 1f;
        if (IsKeyHeld(leftStickLeftKey) || IsKeyHeld(rightStickLeftKey)) navigation.x -= 1f;
        if (IsKeyHeld(leftStickRightKey) || IsKeyHeld(rightStickRightKey)) navigation.x += 1f;

        if (navigation != Vector2.zero)
        {
            tutorialInput.HandleNavigate(navigation.normalized);
        }

        if (IsKeyDown(confirmTutorialKey) ||
            IsKeyDown(alternateConfirmTutorialKey) ||
            IsMouseButtonDown(primaryClickMouseButton))
        {
            tutorialInput.HandleConfirm();
        }

        return true;
    }

    private void HandleGlobalTutorialInput(StandaloneTutorialUI forceTutorial, bool suppressGameplayThisFrame)
    {
        if (!IsKeyDown(openTutorialKey) || suppressGameplayThisFrame || forceTutorial != null)
        {
            return;
        }

        cachedInteractor?.TriggerOpenTutorial();
    }

    private void HandleMappedGameplayInput(StandaloneTutorialUI forceTutorial, bool suppressGameplayFromEarlierInput)
    {
        bool suppressGameplay = suppressGameplayFromEarlierInput;

        if (IsMouseButtonDown(primaryClickMouseButton))
        {
            if (!suppressGameplay)
            {
                HandlePrimaryClick();
            }
        }

        if (IsKeyDown(leftTriggerButtonKey))
        {
            if (!suppressGameplay)
            {
                if (cachedInteractor != null && cachedInteractor.CurrentState == Interactor.GameState.Observing)
                {
                    cachedInteractor.TriggerQuitObserve();
                }
                else
                {
                    cachedInteractor?.TriggerPutAndObserve();
                }
            }
        }

        if (IsKeyDown(rightPrimaryButtonKey))
        {
            if (!suppressGameplay)
            {
                cachedInteractor?.TriggerChangeGlass();
            }
        }

        if (IsKeyDown(rightSecondaryButtonKey))
        {
            if (!suppressGameplay)
            {
                cachedInteractor?.TriggerLightSwitch();
            }
        }

        if (IsKeyDown(rightGripButtonKey))
        {
            if (!suppressGameplay)
            {
                TeleportToPointer();
            }
        }

        if (IsKeyDown(rightStickPressKey))
        {
            if (!suppressGameplay)
            {
                cachedInteractor?.TriggerChangeFocusMode();
            }
        }

        if (IsKeyDown(quitObserveKey) && cachedInteractor != null &&
            cachedInteractor.CurrentState == Interactor.GameState.Observing)
        {
            cachedInteractor.TriggerQuitObserve();
        }
    }

    private void HandlePrimaryClick()
    {
        bool isPointerOverUi = IsPointerOverUi();
        Ray ray;
        float maxDistance;
        bool hasRay = TryBuildPointerRay(out ray, out maxDistance);

        if (isPointerOverUi)
        {
            if (hasRay && SuperAssemblyPartSelectionController.HasActiveSelection)
            {
                SuperAssemblyPartSelectionController.TryHandleDesktopPrimaryClick(ray, maxDistance, true);
            }

            return;
        }

        if (cachedInteractor == null || cachedInteractor.CurrentState != Interactor.GameState.Roaming)
        {
            return;
        }

        cachedInteractor.TriggerTakeObjectOrSample(() =>
            hasRay && MicroscopeExploderModeController.TryHandleDesktopPrimaryClick(ray, maxDistance, isPointerOverUi));
    }

    private void HandleContinuousFocusLightInput(StandaloneTutorialUI forceTutorial)
    {
        if (!IsKeyHeld(leftPrimaryButtonKey))
        {
            return;
        }

        Vector2 rightStick = GetRightStickVectorHeld();
        if (rightStick == Vector2.zero)
        {
            return;
        }

        if (forceTutorial != null && !ForceTutorialAcceptsRightStickInput(forceTutorial, rightStick))
        {
            return;
        }

        if (cachedInteractor == null)
        {
            return;
        }

        if (cachedInteractor.CurrentState == Interactor.GameState.Observing)
        {
            cachedInteractor.ApplyObservingFocusLightInput(rightStick, Time.deltaTime);
        }
        else if (cachedInteractor.CurrentState == Interactor.GameState.Roaming)
        {
            cachedInteractor.ApplyRoamingFocusLightInput(rightStick);
        }
    }

    private void MoveCamera()
    {
        Vector2 leftStick = GetLeftStickVectorHeld();
        float vertical = 0f;
        if (IsKeyHeld(moveUpKey)) vertical += 1f;
        if (IsKeyHeld(moveDownKey)) vertical -= 1f;

        if (leftStick == Vector2.zero && Mathf.Approximately(vertical, 0f))
        {
            return;
        }

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 direction = right * leftStick.x + forward * leftStick.y + Vector3.up * vertical;
        if (direction.sqrMagnitude > 1f)
        {
            direction.Normalize();
        }

        float speed = IsKeyHeld(sprintKey) ? moveSpeed * fastMoveMultiplier : moveSpeed;
        transform.position += direction * speed * Time.deltaTime;
    }

    private void RotateCameraWithMouse()
    {
        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");

        if (Mathf.Approximately(mouseX, 0f) && Mathf.Approximately(mouseY, 0f))
        {
            return;
        }

        yaw += mouseX * rotationSpeed;
        pitch -= mouseY * rotationSpeed;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void ApplyKeyboardTurnInput(StandaloneTutorialUI forceTutorial)
    {
        if (!canMove)
        {
            return;
        }

        Vector2 rightStick = GetRightStickVectorHeld();
        if (rightStick == Vector2.zero)
        {
            return;
        }

        if (forceTutorial != null && !ForceTutorialAcceptsRightStickInput(forceTutorial, rightStick))
        {
            return;
        }

        if (cachedInteractor != null && cachedInteractor.CurrentState == Interactor.GameState.Observing)
        {
            return;
        }

        if (MicroscopeExploderModeController.Instance != null &&
            MicroscopeExploderModeController.Instance.CurrentMode != MicroscopeExploderModeController.AssemblyMode.Normal)
        {
            return;
        }

        yaw += rightStick.x * keyboardTurnSpeed * Time.deltaTime;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void TeleportToPointer()
    {
        if (IsPointerOverUi())
        {
            return;
        }

        Ray ray;
        float maxDistance;
        if (!TryBuildPointerRay(out ray, out maxDistance))
        {
            return;
        }

        RaycastHit hitInfo;
        if (!Physics.Raycast(ray, out hitInfo, maxDistance, teleportSurfaceMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        if (Vector3.Angle(hitInfo.normal, Vector3.up) > maxTeleportSurfaceAngle)
        {
            return;
        }

        MoveToWorldFloorPoint(hitInfo.point);
    }

    private void MoveToWorldFloorPoint(Vector3 destinationPosition)
    {
        if (cachedXrOrigin != null)
        {
            Vector3 up = cachedXrOrigin.Origin != null ? cachedXrOrigin.Origin.transform.up : Vector3.up;
            Vector3 cameraDestination = destinationPosition + up * cachedXrOrigin.CameraInOriginSpaceHeight;
            cachedXrOrigin.MoveCameraToWorldLocation(cameraDestination);
            return;
        }

        transform.position = destinationPosition;
    }

    private bool TryBuildPointerRay(out Ray ray, out float maxDistance)
    {
        CacheReferences();

        ray = default;
        maxDistance = desktopRayMaxDistance;

        Camera targetCamera = cachedCamera != null ? cachedCamera : ResolveActiveCamera();
        if (targetCamera == null)
        {
            return false;
        }

        Vector3 pointerPosition = Cursor.lockState == CursorLockMode.Locked
            ? new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f)
            : Input.mousePosition;

        ray = targetCamera.ScreenPointToRay(pointerPosition);
        return true;
    }

    private void HandleForceTutorialDiscreteInput(StandaloneTutorialUI forceTutorial, ref bool suppressGameplay)
    {
        TryHandleKeyInput(forceTutorial, leftPrimaryButtonKey, TutorialStepInputButton.LeftPrimaryButton, "PC:X", ref suppressGameplay);
        TryHandleKeyInput(forceTutorial, leftSecondaryButtonKey, TutorialStepInputButton.LeftSecondaryButton, "PC:Y", ref suppressGameplay);
        TryHandleKeyInput(forceTutorial, leftTriggerButtonKey, TutorialStepInputButton.LeftTriggerButton, "PC:Z", ref suppressGameplay);
        TryHandleKeyInput(forceTutorial, leftGripButtonKey, TutorialStepInputButton.LeftGripButton, "PC:C", ref suppressGameplay);
        TryHandleKeyInput(forceTutorial, rightPrimaryButtonKey, TutorialStepInputButton.RightPrimaryButton, "PC:R", ref suppressGameplay);
        TryHandleKeyInput(forceTutorial, rightSecondaryButtonKey, TutorialStepInputButton.RightSecondaryButton, "PC:B", ref suppressGameplay);
        TryHandleKeyInput(forceTutorial, leftStickPressKey, TutorialStepInputButton.LeftStickPress, "PC:V", ref suppressGameplay);
        TryHandleKeyInput(forceTutorial, rightStickPressKey, TutorialStepInputButton.RightStickPress, "PC:Tab", ref suppressGameplay);

        if (IsKeyDown(rightGripButtonKey))
        {
            TryHandleGripForceTutorialInput(forceTutorial, out bool shouldSuppress);
            suppressGameplay |= shouldSuppress;
        }

        if (IsMouseButtonDown(primaryClickMouseButton))
        {
            TryHandleForceTutorialInput(
                forceTutorial,
                TutorialStepInputButton.RightTriggerButton,
                "PC:Left Mouse",
                out bool shouldSuppress);
            suppressGameplay |= shouldSuppress;
        }

        TryHandleKeyInput(forceTutorial, leftStickUpKey, TutorialStepInputButton.LeftStickUp, "PC:W", ref suppressGameplay);
        TryHandleKeyInput(forceTutorial, leftStickDownKey, TutorialStepInputButton.LeftStickDown, "PC:S", ref suppressGameplay);
        TryHandleKeyInput(forceTutorial, leftStickLeftKey, TutorialStepInputButton.LeftStickLeft, "PC:A", ref suppressGameplay);
        TryHandleKeyInput(forceTutorial, leftStickRightKey, TutorialStepInputButton.LeftStickRight, "PC:D", ref suppressGameplay);
        TryHandleKeyInput(forceTutorial, rightStickUpKey, TutorialStepInputButton.RightStickUp, "PC:Up Arrow", ref suppressGameplay);
        TryHandleKeyInput(forceTutorial, rightStickDownKey, TutorialStepInputButton.RightStickDown, "PC:Down Arrow", ref suppressGameplay);
        TryHandleKeyInput(forceTutorial, rightStickLeftKey, TutorialStepInputButton.RightStickLeft, "PC:Left Arrow", ref suppressGameplay);
        TryHandleKeyInput(forceTutorial, rightStickRightKey, TutorialStepInputButton.RightStickRight, "PC:Right Arrow", ref suppressGameplay);
    }

    private void TryHandleKeyInput(
        StandaloneTutorialUI forceTutorial,
        KeyCode key,
        TutorialStepInputButton input,
        string sourceName,
        ref bool suppressGameplay)
    {
        if (!IsKeyDown(key))
        {
            return;
        }

        if (forceTutorial != null && forceTutorial.ShouldIgnoreCurrentStepModifierPress(input))
        {
            return;
        }

        TryHandleForceTutorialInput(forceTutorial, input, sourceName, out bool shouldSuppress);
        suppressGameplay |= shouldSuppress;
    }

    private bool TryHandleGripForceTutorialInput(StandaloneTutorialUI forceTutorial, out bool suppressGameplay)
    {
        suppressGameplay = false;
        if (forceTutorial == null)
        {
            return false;
        }

        bool isRequiredHoldSatisfied = IsRequiredHoldButtonSatisfied(forceTutorial);
        if (forceTutorial.CurrentStepAcceptsInput(TutorialStepInputButton.RightGripButton, isRequiredHoldSatisfied))
        {
            return TryHandleForceTutorialInput(forceTutorial, TutorialStepInputButton.RightGripButton, "PC:G", out suppressGameplay);
        }

        if (forceTutorial.CurrentStepAcceptsInput(TutorialStepInputButton.LeftGripButton, isRequiredHoldSatisfied))
        {
            return TryHandleForceTutorialInput(forceTutorial, TutorialStepInputButton.LeftGripButton, "PC:G", out suppressGameplay);
        }

        return TryHandleForceTutorialInput(forceTutorial, TutorialStepInputButton.RightGripButton, "PC:G", out suppressGameplay);
    }

    private bool TryHandleForceTutorialInput(
        StandaloneTutorialUI forceTutorial,
        TutorialStepInputButton input,
        string sourceName,
        out bool suppressGameplay)
    {
        suppressGameplay = false;
        if (forceTutorial == null)
        {
            return false;
        }

        bool isRequiredHoldSatisfied = IsRequiredHoldButtonSatisfied(forceTutorial);
        bool isAccepted = forceTutorial.CurrentStepAcceptsInput(input, isRequiredHoldSatisfied);

        if (forceTutorial.IsWaitingForConfiguredInputDelay())
        {
            suppressGameplay = true;
            return true;
        }

        bool handled = forceTutorial.TryHandleConfiguredInput(input, sourceName, isRequiredHoldSatisfied);
        if (handled && !isAccepted)
        {
            suppressGameplay = true;
        }

        return handled;
    }

    private bool IsRequiredHoldButtonSatisfied(StandaloneTutorialUI forceTutorial)
    {
        if (forceTutorial == null ||
            !forceTutorial.CurrentStepRequiresHeldButtonCombo(out TutorialStepInputButton requiredHeldButton))
        {
            return false;
        }

        return IsTutorialButtonHeld(requiredHeldButton);
    }

    private bool IsTutorialButtonHeld(TutorialStepInputButton button)
    {
        switch (button)
        {
            case TutorialStepInputButton.LeftPrimaryButton:
                return IsKeyHeld(leftPrimaryButtonKey);
            case TutorialStepInputButton.LeftSecondaryButton:
                return IsKeyHeld(leftSecondaryButtonKey);
            case TutorialStepInputButton.LeftTriggerButton:
                return IsKeyHeld(leftTriggerButtonKey);
            case TutorialStepInputButton.LeftGripButton:
                return IsKeyHeld(leftGripButtonKey);
            case TutorialStepInputButton.RightPrimaryButton:
                return IsKeyHeld(rightPrimaryButtonKey);
            case TutorialStepInputButton.RightSecondaryButton:
                return IsKeyHeld(rightSecondaryButtonKey);
            case TutorialStepInputButton.RightTriggerButton:
                return IsMouseButtonHeld(primaryClickMouseButton);
            case TutorialStepInputButton.RightGripButton:
                return IsKeyHeld(rightGripButtonKey);
            case TutorialStepInputButton.LeftStickPress:
                return IsKeyHeld(leftStickPressKey);
            case TutorialStepInputButton.RightStickPress:
                return IsKeyHeld(rightStickPressKey);
            default:
                return false;
        }
    }

    private bool ForceTutorialAcceptsLeftStickInput(StandaloneTutorialUI forceTutorial, Vector2 leftStick)
    {
        if (forceTutorial == null)
        {
            return true;
        }

        bool isHoldSatisfied = IsRequiredHoldButtonSatisfied(forceTutorial);
        return
            (leftStick.y > 0f && forceTutorial.CurrentStepAcceptsInput(TutorialStepInputButton.LeftStickUp, isHoldSatisfied)) ||
            (leftStick.y < 0f && forceTutorial.CurrentStepAcceptsInput(TutorialStepInputButton.LeftStickDown, isHoldSatisfied)) ||
            (leftStick.x < 0f && forceTutorial.CurrentStepAcceptsInput(TutorialStepInputButton.LeftStickLeft, isHoldSatisfied)) ||
            (leftStick.x > 0f && forceTutorial.CurrentStepAcceptsInput(TutorialStepInputButton.LeftStickRight, isHoldSatisfied));
    }

    private bool ForceTutorialAcceptsRightStickInput(StandaloneTutorialUI forceTutorial, Vector2 rightStick)
    {
        if (forceTutorial == null)
        {
            return true;
        }

        bool isHoldSatisfied = IsRequiredHoldButtonSatisfied(forceTutorial);
        return
            (rightStick.y > 0f && forceTutorial.CurrentStepAcceptsInput(TutorialStepInputButton.RightStickUp, isHoldSatisfied)) ||
            (rightStick.y < 0f && forceTutorial.CurrentStepAcceptsInput(TutorialStepInputButton.RightStickDown, isHoldSatisfied)) ||
            (rightStick.x < 0f && forceTutorial.CurrentStepAcceptsInput(TutorialStepInputButton.RightStickLeft, isHoldSatisfied)) ||
            (rightStick.x > 0f && forceTutorial.CurrentStepAcceptsInput(TutorialStepInputButton.RightStickRight, isHoldSatisfied));
    }

    private StandaloneTutorialUI GetVisibleForceTutorial()
    {
        if (Time.unscaledTime < nextTutorialLookupTime &&
            cachedVisibleForceTutorial != null &&
            IsForceTutorialVisible(cachedVisibleForceTutorial))
        {
            return cachedVisibleForceTutorial;
        }

        nextTutorialLookupTime = Time.unscaledTime + TutorialLookupInterval;
        cachedVisibleForceTutorial = null;

        StandaloneTutorialUI[] tutorialUis = FindObjectsOfType<StandaloneTutorialUI>(true);
        for (int i = 0; i < tutorialUis.Length; i++)
        {
            if (IsForceTutorialVisible(tutorialUis[i]))
            {
                cachedVisibleForceTutorial = tutorialUis[i];
                break;
            }
        }

        return cachedVisibleForceTutorial;
    }

    private bool IsForceTutorialVisible(StandaloneTutorialUI tutorialUi)
    {
        return tutorialUi != null &&
               tutorialUi.isActiveAndEnabled &&
               tutorialUi.uiRoot != null &&
               tutorialUi.uiRoot.activeInHierarchy;
    }

    private Vector2 GetLeftStickVectorHeld()
    {
        Vector2 input = Vector2.zero;
        if (IsKeyHeld(leftStickUpKey)) input.y += 1f;
        if (IsKeyHeld(leftStickDownKey)) input.y -= 1f;
        if (IsKeyHeld(leftStickLeftKey)) input.x -= 1f;
        if (IsKeyHeld(leftStickRightKey)) input.x += 1f;
        return input.sqrMagnitude > 1f ? input.normalized : input;
    }

    private Vector2 GetRightStickVectorHeld()
    {
        Vector2 input = Vector2.zero;
        if (IsKeyHeld(rightStickUpKey)) input.y += 1f;
        if (IsKeyHeld(rightStickDownKey)) input.y -= 1f;
        if (IsKeyHeld(rightStickLeftKey)) input.x -= 1f;
        if (IsKeyHeld(rightStickRightKey)) input.x += 1f;
        return input.sqrMagnitude > 1f ? input.normalized : input;
    }

    private bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private bool IsKeyDown(KeyCode key)
    {
        return key != KeyCode.None && Input.GetKeyDown(key);
    }

    private bool IsKeyHeld(KeyCode key)
    {
        return key != KeyCode.None && Input.GetKey(key);
    }

    private bool IsMouseButtonDown(int button)
    {
        return button >= 0 && Input.GetMouseButtonDown(button);
    }

    private bool IsMouseButtonHeld(int button)
    {
        return button >= 0 && Input.GetMouseButton(button);
    }
}
