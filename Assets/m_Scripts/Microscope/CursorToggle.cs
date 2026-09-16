using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

/// <summary>
/// 鼠标光标显示与隐藏控制工具，以及 VR 虚拟手柄 / PC 漫游模式平滑切换器。
/// 无需手动挂载，运行场景时会自动生效。
/// 按 F2 可在一键关闭虚拟手柄（切入纯 PC 漫游模式）与开启手柄（VR 模式）之间切换。
/// </summary>
public class CursorToggle : MonoBehaviour
{
    [Header("快捷键设置")]
    [Tooltip("主要快捷键：波浪号/反引号键 (~ / `，位于键盘 ESC 下方、Tab 上方)")]
    public KeyCode toggleKey = KeyCode.BackQuote;

    [Tooltip("备用快捷键 1：F1 键")]
    public KeyCode alternateKey = KeyCode.F1;

    [Tooltip("备用快捷键 2：反斜杠键 (\\，XR 模拟器官方默认键)")]
    public KeyCode simulatorKey = KeyCode.Backslash;

    [Header("虚拟手柄开关")]
    [Tooltip("一键关闭/开启虚拟 VR 手柄及射线的快捷键 (默认 F2)")]
    public KeyCode toggleSimulatorKey = KeyCode.F2;

    [Header("初始配置")]
    [Tooltip("运行场景时是否自动隐藏并锁定鼠标")]
    public bool hideOnStart = false;

    private static CursorToggle s_Instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (s_Instance == null && FindObjectOfType<CursorToggle>() == null)
        {
            var go = new GameObject("[CursorToggleHelper]");
            s_Instance = go.AddComponent<CursorToggle>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        s_Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (hideOnStart)
        {
            SetCursorHidden(true);
        }

        // 确保所有相机的 Rigidbody 设为 Kinematic，Collider 设为 Trigger，防止物理穿模排斥推飞相机
        SanitizeCameraPhysics();
    }

    /// <summary>
    /// 确保相机上的物理组件不会与场景或角色控制器发生物理碰撞弹射
    /// </summary>
    private void SanitizeCameraPhysics()
    {
        var cameras = FindObjectsOfType<Camera>(true);
        foreach (var c in cameras)
        {
            if (c == null) continue;

            var rb = c.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            var col = c.GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            // 确保在 PC 模式下，非 RenderTexture 相机的 stereoTargetEye 设为 None，targetDisplay 设为 0，防止 Display 1 无画面
            if (c.targetTexture == null)
            {
                c.stereoTargetEye = StereoTargetEyeMask.None;
                c.targetDisplay = 0;
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(alternateKey) || Input.GetKeyDown(simulatorKey))
        {
            ToggleCursor();
        }

        if (Input.GetKeyDown(toggleSimulatorKey))
        {
            ToggleVirtualControllers();
        }
    }

    /// <summary>
    /// 一键开启或关闭虚拟手柄及模拟器，并在切换边界无缝衔接 PC / VR 视角与物理状态
    /// </summary>
    public void ToggleVirtualControllers()
    {
        var sim = FindObjectOfType<XRDeviceSimulator>(true);
        var controllers = FindObjectsOfType<ActionBasedController>(true);
        var locomotion = FindObjectOfType<LocomotionSystem>(true);
        var pcMove = FindObjectOfType<CameraTryMove>(true);
        var xrOrigin = FindObjectOfType<XROrigin>();

        Camera cam = xrOrigin != null && xrOrigin.Camera != null ? xrOrigin.Camera : Camera.main;

        bool isCurrentlyActive = false;
        if (sim != null && sim.gameObject.activeSelf)
        {
            isCurrentlyActive = true;
        }
        else if (controllers != null && controllers.Length > 0 && controllers[0].gameObject.activeSelf)
        {
            isCurrentlyActive = true;
        }

        bool targetActive = !isCurrentlyActive;

        // 1. 切换模拟器本体 (包含左上角控制面板)
        if (sim != null)
        {
            sim.gameObject.SetActive(targetActive);
        }

        // 2. 切换双手柄模型与交互射线
        if (controllers != null)
        {
            foreach (var c in controllers)
            {
                if (c != null)
                {
                    c.gameObject.SetActive(targetActive);
                }
            }
        }

        // 3. 切换 VR 移动系统 (PC 漫游模式下关闭，避免与 CameraTryMove 发生按键与位移冲突)
        if (locomotion != null)
        {
            locomotion.gameObject.SetActive(targetActive);
        }

        // 4. 关键边界：切换 CharacterController 与 Driver 及 GazeAssistance
        // PC 模式下关闭 CharacterController，避免与 CameraTryMove 直接修改 transform.position / 旋转发生地面穿模物理排斥导致乱飞
        if (xrOrigin != null)
        {
            var cc = xrOrigin.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = targetActive;
            }

            var ccd = xrOrigin.GetComponent<CharacterControllerDriver>();
            if (ccd != null)
            {
                ccd.enabled = targetActive;
            }

            var gaze = xrOrigin.GetComponent<XRGazeAssistance>();
            if (gaze != null)
            {
                gaze.enabled = targetActive;
            }
        }

        // 5. 模式切换边界状态处理
        if (targetActive)
        {
            // === 切回 VR 模式 ===
            // 恢复相机上的 VR 跟踪驱动
            if (cam != null)
            {
                var drivers = cam.GetComponents<TrackedPoseDriver>();
                foreach (var d in drivers)
                {
                    if (d != null) d.enabled = true;
                }
            }

            // 禁用 PC 漫游脚本
            if (pcMove != null)
            {
                pcMove.enabled = false;
            }

            Debug.Log("<color=#55ff88>[SimulatorToggle]</color> <b>已开启虚拟 VR 手柄</b> (按 <b>F2</b> 可随时关闭手柄并切回 PC 漫游)。");
        }
        else
        {
            // === 切入 PC 模式 ===
            // 关键 A：消除相机上 Rigidbody 造成的物理穿模排斥乱飞
            SanitizeCameraPhysics();

            if (cam != null)
            {
                // 关键 B：禁用相机的 TrackedPoseDriver，防止设备关闭后抢夺或冻结相机姿态
                var drivers = cam.GetComponents<TrackedPoseDriver>();
                foreach (var d in drivers)
                {
                    if (d != null) d.enabled = false;
                }

                // 关键 C：提取当前相机在世界坐标中的水平朝向（Yaw）
                Vector3 forward = cam.transform.forward;
                forward.y = 0f;
                float currentYaw = forward.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(forward.normalized).eulerAngles.y
                    : (xrOrigin != null ? xrOrigin.transform.eulerAngles.y : 0f);

                // 关键 D：归位子相机的局部坐标与旋转，保证视角平视前方且不叠加错误偏移
                cam.transform.localRotation = Quaternion.identity;
                cam.transform.localPosition = Vector3.zero;

                // 关键 E：将父级 XR Origin 的水平朝向对齐到该世界偏航角，平滑接管
                if (xrOrigin != null)
                {
                    xrOrigin.transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
                }
            }

            // 关键 F：重置鼠标状态，避免切换瞬间光标锁定导致的鼠标 Delta 突变跳变
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 关键 G：激活 PC 漫游脚本，先置 false 再置 true 强制触发 OnEnable 重新根据当前朝向同步 yaw/pitch
            if (pcMove != null)
            {
                pcMove.enabled = false;
                pcMove.enabled = true;
                pcMove.canMove = true;
            }

            Debug.Log("<color=#ffaa55>[SimulatorToggle]</color> <b>已关闭虚拟 VR 手柄</b>，已恢复纯 PC 键鼠漫游 (WASD 移动，按住鼠标右键转视角)。按 <b>F2</b> 可重新开启手柄。");
        }
    }

    /// <summary>
    /// 切换鼠标的显示/隐藏状态
    /// </summary>
    public void ToggleCursor()
    {
        bool shouldHide = Cursor.visible || Cursor.lockState != CursorLockMode.Locked;
        SetCursorHidden(shouldHide);
    }

    /// <summary>
    /// 设置鼠标光标是否隐藏并锁定
    /// </summary>
    /// <param name="hidden">true: 隐藏并锁定到画面中心; false: 显示并解锁自由移动</param>
    public void SetCursorHidden(bool hidden)
    {
        if (hidden)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("<color=#55ff88>[CursorToggle]</color> 鼠标已隐藏并锁定到画面中。再次按 <b>~</b>、<b>F1</b> 或 <b>\\</b> 可呼出鼠标。");
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("<color=#55ff88>[CursorToggle]</color> 鼠标已解锁并显示。按 <b>~</b>、<b>F1</b> 或 <b>\\</b> 可隐藏鼠标。");
        }
    }
}
