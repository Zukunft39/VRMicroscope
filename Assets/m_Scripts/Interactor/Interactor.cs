using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class Interactor : MonoBehaviour
{
    public enum GameState
    {
        Roaming,    // 漫游：移动、交互检测
        Observing,  // 观察：显微镜操作
        Tutorial    // 教程：UI 操作 (暂停其他输入)
    }
    [Header("Input Configuration")]
    public InputActionAsset inputActionAsset;
    public LocomotionSystem xrLocomotionSys;
    
    public GameState CurrentState { get; private set; }
    public Microscope AssistantMicroscope => _currentMicroscope;
    private Microscope _currentMicroscope;
    private InteractableSamples _sample;
    private InputActionMap _roamingMap;
    private InputActionMap _observingMap;
    private InputActionMap _tutorialMap;
    private InputActionMap _globalMap;
    
    private InteractWithSamples _interactWithSamples;
    public TutorialButtonInput tutorialButtonInput;
    public Tutorial currentTutorial;
    private GameState lastState;
    private bool _forceTutorialGameplayInputBlocked;
    private Coroutine _pendingStateApplyCoroutine;
    public static Interactor Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Interactor shares Camera Offset with sample and camera components.
            // Destroying the whole object also destroys the active sample state.
            enabled = false;
            Destroy(this);
            return;
        }
        Instance = this;
        
        
        _roamingMap = inputActionAsset.FindActionMap("Roaming"); 
        _observingMap = inputActionAsset.FindActionMap("Observing");
        _tutorialMap = inputActionAsset.FindActionMap("Tutorial");
        _globalMap = inputActionAsset.FindActionMap("Global");
        _interactWithSamples = GetComponent<InteractWithSamples>();
        
    }

    private void Start()
    {
        BindObserving();
        BindRoaming();
        BindTutorial();
        BindGlobal();
        _roamingMap.Disable();
        _observingMap.Disable();
        _tutorialMap.Disable();
        ChangeState(GameState.Roaming);
    }

    private void Update()
    {
    }

    private void OnDestroy()
    {
        if (_pendingStateApplyCoroutine != null)
        {
            StopCoroutine(_pendingStateApplyCoroutine);
            _pendingStateApplyCoroutine = null;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void OnMicroscopeIn(Microscope microscope)
    {
        _currentMicroscope = microscope;
    }
    public void OnMicroscopeOut(Microscope microscope)
    {
        if(_currentMicroscope == microscope)
            _currentMicroscope = null;
    }
    void BindRoaming()
    {
        _roamingMap.FindAction("LightSwitch").started += (context) =>
        {
            TriggerLightSwitch();
        };
        _roamingMap.FindAction("PutAndObserve").started += (context) =>
        {
            TriggerPutAndObserve();
        };
        _roamingMap.FindAction("TakeObj").started += (context) =>
        {
            TriggerTakeObjectOrSample();
        };
        _roamingMap.FindAction("ChangeGlass").started += (context) =>
        {
            TriggerChangeGlass();
        };
        _roamingMap.FindAction("ChangeFocusOrChangeLIght").performed += (context) =>
        {
            ApplyRoamingFocusLightInput(context.ReadValue<Vector2>());
        };
    }

    private Coroutine _observingCoroutine;
    void BindObserving()
    {
        _observingMap.FindAction("ChangeMode").started += (context) =>
        {
            TriggerChangeFocusMode();
        };
        _observingMap.FindAction("QuitObserve").started += (context) =>
        {
            TriggerQuitObserve();
        };
        _observingMap.FindAction("ChangeFocusOrChangeLIght").performed += (context) =>
        {
            if (_observingCoroutine != null) return;
            _observingCoroutine = StartCoroutine(AdjustMicroscopeRoutine(context.action));
        };
        _observingMap.FindAction("ChangeFocusOrChangeLIght").canceled += (context) =>
        {
            if (_observingCoroutine != null)
            {
                StopCoroutine(_observingCoroutine);
                _observingCoroutine = null;
            }
        };
        _observingMap.FindAction("ChangeGlass").started += (context) =>
        {
            TriggerChangeGlass();
        };
    }
    void BindTutorial()
    {
        _tutorialMap.FindAction("MoveFocus").performed += (context) =>
        {
            Vector2 temp = context.ReadValue<Vector2>();
            tutorialButtonInput?.HandleNavigate(temp);
        };
        _tutorialMap.FindAction("ClickFocusedUI").started += (context) =>
        {
            tutorialButtonInput?.HandleConfirm();
        };
    }

    void BindGlobal()
    {
        _globalMap.FindAction("OpenTutorial").started += (context) =>
        {
            TriggerOpenTutorial();
        };
    }

    public void TriggerOpenTutorial()
    {
        if (VRMicroscope.Assistant.AssistantChatPanel.BlocksGameplay) return;
        if (CurrentState == GameState.Roaming)
        {
            currentTutorial.ReturnToFirstLevel();
        }
        else if (CurrentState == GameState.Observing)
        {
            currentTutorial.ShowTutorial(2);
        }

        ChangeState(GameState.Tutorial);
    }

    public void TriggerLightSwitch()
    {
        if (VRMicroscope.Assistant.AssistantChatPanel.BlocksGameplay) return;
        _currentMicroscope?.LightSwitch();
    }

    public void TriggerPutAndObserve()
    {
        if (VRMicroscope.Assistant.AssistantChatPanel.BlocksGameplay) return;
        _currentMicroscope?.PutAndObserve();
    }

    public void TriggerTakeObjectOrSample()
    {
        TriggerTakeObjectOrSample(() =>
            SNOMDemonstrationController.TryHandleRightTrigger() ||
            MicroscopeExploderModeController.TryHandleRightTrigger());
    }

    public void TriggerTakeObjectOrSample(Func<bool> assemblyHandler)
    {
        if (VRMicroscope.Assistant.AssistantChatPanel.BlocksGameplay) return;
        bool handledByExploderMode = assemblyHandler != null && assemblyHandler.Invoke();
        Debug.Log(
            $"[Interactor] TakeObj triggered. handledByExploderMode={handledByExploderMode}, currentMicroscope='{_currentMicroscope?.name ?? "null"}', hasPlacedSample={(_currentMicroscope != null && _currentMicroscope.HasPlacedSample())}");

        if (handledByExploderMode)
        {
            return;
        }

        // 优先处理“从显微镜上取下当前样本”，避免同一输入同时触发“再实例化一个新样本”，
        // 导致显微镜观察对象被替换或状态混乱。
        if (_currentMicroscope != null && _currentMicroscope.HasPlacedSample())
        {
            _currentMicroscope.TakeOutobj();
        }
        else
        {
            _interactWithSamples?.PickSample();
        }
    }

    public void TriggerChangeGlass()
    {
        if (VRMicroscope.Assistant.AssistantChatPanel.BlocksGameplay) return;
        _currentMicroscope?.RotateGlass();
        _currentMicroscope?.RotateGlassOnObserving();
    }

    public void TriggerChangeFocusMode()
    {
        if (VRMicroscope.Assistant.AssistantChatPanel.BlocksGameplay) return;
        _currentMicroscope?.SwitchModeOfChange();
    }

    public void TriggerQuitObserve()
    {
        if (VRMicroscope.Assistant.AssistantChatPanel.BlocksGameplay) return;
        _currentMicroscope?.QuitObserve();
        ChangeState(GameState.Roaming);
    }

    public void ApplyRoamingFocusLightInput(Vector2 input)
    {
        if (VRMicroscope.Assistant.AssistantChatPanel.BlocksGameplay) return;
        if (_currentMicroscope == null)
        {
            return;
        }

        _currentMicroscope.ChangeFocal(Math.Abs(input.x) > 0.7f ? input.x : 0);
        _currentMicroscope.AdjustLight(Math.Abs(input.y) > 0.7f ? input.y : 0);
    }

    public void ApplyObservingFocusLightInput(Vector2 input, float deltaTime)
    {
        if (VRMicroscope.Assistant.AssistantChatPanel.BlocksGameplay) return;
        if (_currentMicroscope == null)
        {
            return;
        }

        float xVal = Math.Abs(input.x) > 0.7f ? input.x : 0;
        float yVal = Math.Abs(input.y) > 0.7f ? input.y : 0;
        if (xVal != 0)
        {
            _currentMicroscope.ChangeFocal(xVal * deltaTime);
        }

        if (yVal != 0)
        {
            _currentMicroscope.AdjustLight(yVal * deltaTime);
        }
    }

    public void ChangeState(GameState? newState)
    {
        bool flag = newState == CurrentState;
        GameState temp = CurrentState;
        CurrentState = newState??lastState;
        lastState=flag?lastState:temp;
       
        print($"State Transition: -> {CurrentState}");
        MandatoryTutorialTrigger.NotifyGlobalStartConditionsMayHaveChanged();
        
        // 开启协程，将输入映射和组件的切换延迟到当前帧末尾
        QueueStateMapRefresh();
    }

    public void SetForceTutorialGameplayInputBlocked(bool isBlocked)
    {
        if (this == null)
        {
            return;
        }

        if (_forceTutorialGameplayInputBlocked == isBlocked)
        {
            return;
        }

        _forceTutorialGameplayInputBlocked = isBlocked;
        QueueStateMapRefresh();
    }

    private void QueueStateMapRefresh()
    {
        if (this == null || !isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            return;
        }

        if (_pendingStateApplyCoroutine != null)
        {
            StopCoroutine(_pendingStateApplyCoroutine);
        }

        _pendingStateApplyCoroutine = StartCoroutine(ApplyStateChangeNextFrame(CurrentState));
    }

    private IEnumerator ApplyStateChangeNextFrame(GameState state)
    {
        // 等待当前帧所有的物理、Update 和 Input System 内部遍历全部安全结束
        yield return new WaitForEndOfFrame();

        _roamingMap.Disable();
        _observingMap.Disable();
        _tutorialMap.Disable();

        switch (state)
        {
            case GameState.Roaming:
                if (!_forceTutorialGameplayInputBlocked)
                {
                    _roamingMap.Enable();
                }
                if (xrLocomotionSys != null) xrLocomotionSys.gameObject.SetActive(true);
                break;
            case GameState.Observing:
                if (!_forceTutorialGameplayInputBlocked)
                {
                    _observingMap.Enable();
                }
                if (xrLocomotionSys != null) xrLocomotionSys.gameObject.SetActive(false);
                break;
            case GameState.Tutorial:
                _tutorialMap.Enable();
                if (xrLocomotionSys != null) xrLocomotionSys.gameObject.SetActive(false);
                break;
        }

        _pendingStateApplyCoroutine = null;
    }
    private IEnumerator AdjustMicroscopeRoutine(InputAction action)
    {
        while (true) // 死循环，直到外部调用 StopCoroutine 把它杀掉
        {
            // 实时读取摇杆数值
            Vector2 input = action.ReadValue<Vector2>();
            float xVal = Mathf.Abs(input.x) > 0.7f ? input.x : 0;
            float yVal = Mathf.Abs(input.y) > 0.7f ? input.y : 0;

            ApplyObservingFocusLightInput(new Vector2(xVal, yVal), Time.deltaTime);

            // 暂停一帧，等待下一次循环（相当于 Update 的效果）
            yield return null; 
        }
    }
}
