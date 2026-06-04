using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using VRMicroscope.Tutorial;

[DefaultExecutionOrder(-500)]
public class DesktopDebugInputBridge : MonoBehaviour
{
    [Header("Enable")]
    [SerializeField] private bool enableDesktopDebugInput = true;
    [SerializeField] private bool installRuntimeBindings = true;

    [Header("Input Assets")]
    [SerializeField] private InputActionAsset mainInputActions;
    [SerializeField] private InputActionAsset forceTutorialInputActions;

    [Header("Scene References")]
    [SerializeField] private Interactor interactor;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private XROrigin xrOrigin;

    [Header("Desktop Pointer")]
    [SerializeField] private bool mouseLeftClickAsRightTrigger = true;
    [SerializeField] private KeyCode keyboardRightTriggerKey = KeyCode.F;
    [SerializeField] private float desktopRayDistance = 100f;

    [Header("Mouse Look")]
    [SerializeField] private bool enableMouseLook = true;
    [SerializeField] private KeyCode mouseLookButton = KeyCode.Mouse1;
    [SerializeField] private float mouseLookSensitivity = 0.18f;
    [SerializeField] private float minPitch = -70f;
    [SerializeField] private float maxPitch = 70f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private readonly List<string> installedBindingKeys = new List<string>();
    private bool bindingsInstalled;
    private float cameraPitch;

    private void Awake()
    {
        ResolveReferences();

        if (installRuntimeBindings)
        {
            InstallBindings();
        }
    }

    private void OnEnable()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (!enableDesktopDebugInput)
        {
            return;
        }

        HandleDesktopPrimaryClick();
        HandleMouseLook();
    }

    [ContextMenu("Install Runtime Desktop Bindings")]
    public void InstallBindings()
    {
        if (bindingsInstalled)
        {
            return;
        }

        ResolveReferences();
        InstallMainInputBindings();
        InstallForceTutorialBindings();
        bindingsInstalled = true;
        DebugLog("Desktop debug bindings installed.");
    }

    private void ResolveReferences()
    {
        if (interactor == null)
        {
            interactor = Interactor.Instance != null ? Interactor.Instance : FindObjectOfType<Interactor>();
        }

        if (mainInputActions == null && interactor != null)
        {
            mainInputActions = interactor.inputActionAsset;
        }

        if (forceTutorialInputActions == null)
        {
            TutorialButtonListener listener = FindObjectOfType<TutorialButtonListener>(true);
            if (listener != null)
            {
                forceTutorialInputActions = listener.tutorialInputActions;
            }
        }

        if (targetCamera == null)
        {
            targetCamera = ResolveTargetCamera();
        }

        if (xrOrigin == null)
        {
            xrOrigin = FindObjectOfType<XROrigin>();
        }
    }

    private Camera ResolveTargetCamera()
    {
        if (ProgressControl.Instance != null &&
            ProgressControl.Instance.cinemachineBrain != null &&
            ProgressControl.Instance.cinemachineBrain.OutputCamera != null)
        {
            return ProgressControl.Instance.cinemachineBrain.OutputCamera;
        }

        return Camera.main;
    }

    private void InstallMainInputBindings()
    {
        if (mainInputActions == null)
        {
            DebugLogWarning("Main input action asset is missing. Desktop gameplay bindings were not installed.");
            return;
        }

        AddButtonBinding(mainInputActions, "Roaming", "LightSwitch", "<Keyboard>/l");
        AddButtonBinding(mainInputActions, "Roaming", "PutAndObserve", "<Keyboard>/e");
        AddButtonBinding(mainInputActions, "Roaming", "ChangeGlass", "<Keyboard>/t");
        AddButtonBinding(mainInputActions, "Roaming", "AutoMove", "<Keyboard>/g");
        Add2DVectorBinding(mainInputActions, "Roaming", "ChangeFocusOrChangeLIght",
            "<Keyboard>/upArrow", "<Keyboard>/downArrow", "<Keyboard>/leftArrow", "<Keyboard>/rightArrow");

        AddButtonBinding(mainInputActions, "Observing", "ChangeMode", "<Keyboard>/m");
        AddButtonBinding(mainInputActions, "Observing", "QuitObserve", "<Keyboard>/escape");
        AddButtonBinding(mainInputActions, "Observing", "ChangeGlass", "<Keyboard>/t");
        Add2DVectorBinding(mainInputActions, "Observing", "ChangeFocusOrChangeLIght",
            "<Keyboard>/upArrow", "<Keyboard>/downArrow", "<Keyboard>/leftArrow", "<Keyboard>/rightArrow");

        Add2DVectorBinding(mainInputActions, "Tutorial", "MoveFocus",
            "<Keyboard>/w", "<Keyboard>/s", "<Keyboard>/a", "<Keyboard>/d");
        Add2DVectorBinding(mainInputActions, "Tutorial", "MoveFocus",
            "<Keyboard>/upArrow", "<Keyboard>/downArrow", "<Keyboard>/leftArrow", "<Keyboard>/rightArrow");
        AddButtonBinding(mainInputActions, "Tutorial", "ClickFocusedUI", "<Keyboard>/space");
        AddButtonBinding(mainInputActions, "Tutorial", "ClickFocusedUI", "<Keyboard>/enter");

        AddButtonBinding(mainInputActions, "Global", "OpenTutorial", "<Keyboard>/h");
        AddButtonBinding(mainInputActions, "Global", "OpenTutorial", "<Keyboard>/y");

        Add2DVectorBinding(mainInputActions, "XRI LeftHand Locomotion", "Move",
            "<Keyboard>/w", "<Keyboard>/s", "<Keyboard>/a", "<Keyboard>/d");
        Add2DVectorBinding(mainInputActions, "XRI RightHand Locomotion", "Turn",
            null, null, "<Keyboard>/leftBracket", "<Keyboard>/rightBracket");
        Add2DVectorBinding(mainInputActions, "XRI RightHand Locomotion", "Snap Turn",
            null, null, "<Keyboard>/leftBracket", "<Keyboard>/rightBracket");

        AddButtonBinding(mainInputActions, "XRI RightHand Interaction", "UI Press", "<Mouse>/leftButton");
        AddButtonBinding(mainInputActions, "XRI RightHand Interaction", "Activate", "<Mouse>/leftButton");
        AddButtonBinding(mainInputActions, "XRI RightHand Interaction", "Activate", "<Keyboard>/f");
        AddButtonBinding(mainInputActions, "XRI RightHand Interaction", "Select", "<Keyboard>/g");
    }

    private void InstallForceTutorialBindings()
    {
        if (forceTutorialInputActions == null)
        {
            DebugLogWarning("Force tutorial input action asset is missing. Force tutorial desktop bindings were not installed.");
            return;
        }

        AddButtonBinding(forceTutorialInputActions, "VRJoystick", "ForceKey", "<Keyboard>/y");
        AddButtonBinding(forceTutorialInputActions, "VRJoystick", "LeftPrimaryButton", "<Keyboard>/x");
        AddButtonBinding(forceTutorialInputActions, "VRJoystick", "LeftSecondaryButton", "<Keyboard>/y");
        AddButtonBinding(forceTutorialInputActions, "VRJoystick", "LeftTriggerButton", "<Keyboard>/c");
        AddButtonBinding(forceTutorialInputActions, "VRJoystick", "LeftGripButton", "<Keyboard>/v");
        AddButtonBinding(forceTutorialInputActions, "VRJoystick", "RightPrimaryButton", "<Keyboard>/j");
        AddButtonBinding(forceTutorialInputActions, "VRJoystick", "RightSecondaryButton", "<Keyboard>/k");
        AddButtonBinding(forceTutorialInputActions, "VRJoystick", "RightTriggerButton", "<Keyboard>/f");
        AddButtonBinding(forceTutorialInputActions, "VRJoystick", "RightTriggerButton", "<Mouse>/leftButton");
        AddButtonBinding(forceTutorialInputActions, "VRJoystick", "RightGripButton", "<Keyboard>/g");
        AddButtonBinding(forceTutorialInputActions, "VRJoystick", "LeftStickPress", "<Keyboard>/leftCtrl");
        AddButtonBinding(forceTutorialInputActions, "VRJoystick", "RightStickPress", "<Keyboard>/rightCtrl");
        Add2DVectorBinding(forceTutorialInputActions, "VRJoystick", "LeftStick",
            "<Keyboard>/w", "<Keyboard>/s", "<Keyboard>/a", "<Keyboard>/d");
        Add2DVectorBinding(forceTutorialInputActions, "VRJoystick", "RightStick",
            "<Keyboard>/upArrow", "<Keyboard>/downArrow", "<Keyboard>/leftArrow", "<Keyboard>/rightArrow");
    }

    private void HandleDesktopPrimaryClick()
    {
        if (interactor == null || interactor.CurrentState != Interactor.GameState.Roaming)
        {
            return;
        }

        bool mouseClicked = mouseLeftClickAsRightTrigger && Input.GetMouseButtonDown(0);
        bool keyPressed = Input.GetKeyDown(keyboardRightTriggerKey);
        if (!mouseClicked && !keyPressed)
        {
            return;
        }

        bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (pointerOverUi && mouseClicked)
        {
            return;
        }

        Camera camera = targetCamera != null ? targetCamera : ResolveTargetCamera();
        Ray ray = camera != null
            ? camera.ScreenPointToRay(Input.mousePosition)
            : new Ray(transform.position, transform.forward);

        interactor.TriggerTakeObjectOrSample(() =>
            MicroscopeExploderModeController.TryHandleDesktopPrimaryClick(ray, desktopRayDistance, pointerOverUi));
    }

    private void HandleMouseLook()
    {
        if (!enableMouseLook || !Input.GetKey(mouseLookButton))
        {
            return;
        }

        if (interactor != null && interactor.CurrentState == Interactor.GameState.Tutorial)
        {
            return;
        }

        Camera camera = targetCamera != null ? targetCamera : ResolveTargetCamera();
        if (camera == null)
        {
            return;
        }

        Vector2 delta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        if (delta.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        Transform yawRoot = GetYawRoot(camera);
        if (yawRoot != null)
        {
            yawRoot.Rotate(Vector3.up, delta.x * mouseLookSensitivity, Space.World);
        }

        cameraPitch = Mathf.Clamp(cameraPitch - delta.y * mouseLookSensitivity, minPitch, maxPitch);
        Vector3 localEuler = camera.transform.localEulerAngles;
        localEuler.x = cameraPitch;
        camera.transform.localEulerAngles = localEuler;
    }

    private Transform GetYawRoot(Camera camera)
    {
        if (xrOrigin == null)
        {
            return camera.transform.parent != null ? camera.transform.parent : camera.transform;
        }

        return xrOrigin.Origin != null ? xrOrigin.Origin.transform : xrOrigin.transform;
    }

    private void AddButtonBinding(InputActionAsset asset, string mapName, string actionName, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        InputAction action = FindAction(asset, mapName, actionName);
        if (action == null || HasBinding(action, path))
        {
            return;
        }

        AddBindingSafely(action, path);
    }

    private void Add2DVectorBinding(
        InputActionAsset asset,
        string mapName,
        string actionName,
        string up,
        string down,
        string left,
        string right)
    {
        InputAction action = FindAction(asset, mapName, actionName);
        if (action == null)
        {
            return;
        }

        if (HasBinding(action, up) && HasBinding(action, down) && HasBinding(action, left) && HasBinding(action, right))
        {
            return;
        }

        bool wasEnabled = action.actionMap.enabled;
        if (wasEnabled)
        {
            action.actionMap.Disable();
        }

        InputActionSetupExtensions.CompositeSyntax composite = action.AddCompositeBinding("2DVector");
        if (!string.IsNullOrWhiteSpace(up))
        {
            composite.With("Up", up);
        }

        if (!string.IsNullOrWhiteSpace(down))
        {
            composite.With("Down", down);
        }

        if (!string.IsNullOrWhiteSpace(left))
        {
            composite.With("Left", left);
        }

        if (!string.IsNullOrWhiteSpace(right))
        {
            composite.With("Right", right);
        }

        if (wasEnabled)
        {
            action.actionMap.Enable();
        }
    }

    private InputAction FindAction(InputActionAsset asset, string mapName, string actionName)
    {
        if (asset == null)
        {
            return null;
        }

        InputActionMap map = asset.FindActionMap(mapName, false);
        if (map == null)
        {
            DebugLogWarning($"Action map not found: {mapName}");
            return null;
        }

        InputAction action = map.FindAction(actionName, false);
        if (action == null)
        {
            DebugLogWarning($"Action not found: {mapName}/{actionName}");
        }

        return action;
    }

    private bool HasBinding(InputAction action, string path)
    {
        if (action == null || string.IsNullOrWhiteSpace(path))
        {
            return true;
        }

        string key = $"{action.actionMap.name}/{action.name}/{path}";
        if (installedBindingKeys.Contains(key))
        {
            return true;
        }

        for (int i = 0; i < action.bindings.Count; i++)
        {
            if (action.bindings[i].path == path)
            {
                installedBindingKeys.Add(key);
                return true;
            }
        }

        return false;
    }

    private void AddBindingSafely(InputAction action, string path)
    {
        bool wasEnabled = action.actionMap.enabled;
        if (wasEnabled)
        {
            action.actionMap.Disable();
        }

        action.AddBinding(path);
        installedBindingKeys.Add($"{action.actionMap.name}/{action.name}/{path}");

        if (wasEnabled)
        {
            action.actionMap.Enable();
        }
    }

    private void DebugLog(string message)
    {
        if (!enableDebugLogs)
        {
            return;
        }

        Debug.Log($"[DesktopDebugInputBridge] {message}", this);
    }

    private void DebugLogWarning(string message)
    {
        if (!enableDebugLogs)
        {
            return;
        }

        Debug.LogWarning($"[DesktopDebugInputBridge] {message}", this);
    }
}
