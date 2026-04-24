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
    [Header("输入配置文件")]
    public InputActionAsset inputActionAsset;
    public LocomotionSystem xrLocomotionSys;
    
    public GameState CurrentState { get; private set; }
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
             Destroy(gameObject); // 如果已经有一个实例，销毁重复的
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
        // 不通过 Input System，直接检测键盘 H 键呼出教程
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (CurrentState == GameState.Roaming)
            {
                currentTutorial.ReturnToFirstLevel();
                ChangeState(GameState.Tutorial);
            }
            else if (CurrentState == GameState.Observing)
            {
                currentTutorial.ShowTutorial(2); 
                ChangeState(GameState.Tutorial);
            }
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
            _currentMicroscope?.LightSwitch();
        };
        _roamingMap.FindAction("PutAndObserve").started += (context) =>
        {
            _currentMicroscope?.PutAndObserve();
        };
        _roamingMap.FindAction("TakeObj").started += (context) =>
        {
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
        };
        _roamingMap.FindAction("ChangeGlass").started += (context) =>
        {
            _currentMicroscope?.RotateGlass();
            _currentMicroscope?.RotateGlassOnObserving();
        };
        _roamingMap.FindAction("ChangeFocusOrChangeLIght").performed += (context) =>
        {
            Vector2 temp = context.ReadValue<Vector2>();
            _currentMicroscope.ChangeFocal(Math.Abs(temp.x) > 0.7f ? temp.x : 0);
            _currentMicroscope.AdjustLight(Math.Abs(temp.y) > 0.7f ? temp.y : 0);
        };
    }

    private Coroutine _observingCoroutine;
    void BindObserving()
    {
        _observingMap.FindAction("ChangeMode").started += (context) =>
        {
            _currentMicroscope?.SwitchModeOfChange();
        };
        _observingMap.FindAction("QuitObserve").started += (context) =>
        {
            _currentMicroscope?.QuitObserve();
            ChangeState(GameState.Roaming);
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
            _currentMicroscope?.RotateGlass();
            _currentMicroscope?.RotateGlassOnObserving();
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
            if(CurrentState==GameState.Roaming)
                currentTutorial.ReturnToFirstLevel();
            else if (CurrentState == GameState.Observing)
                currentTutorial.ShowTutorial(2); 
            ChangeState(GameState.Tutorial);
        };
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
        if (_forceTutorialGameplayInputBlocked == isBlocked)
        {
            return;
        }

        _forceTutorialGameplayInputBlocked = isBlocked;
        QueueStateMapRefresh();
    }

    private void QueueStateMapRefresh()
    {
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

            if (_currentMicroscope != null)
            {
                if (xVal != 0) _currentMicroscope.ChangeFocal(xVal * Time.deltaTime);
                if (yVal != 0) _currentMicroscope.AdjustLight(yVal * Time.deltaTime);
            }

            // 暂停一帧，等待下一次循环（相当于 Update 的效果）
            yield return null; 
        }
    }
}
