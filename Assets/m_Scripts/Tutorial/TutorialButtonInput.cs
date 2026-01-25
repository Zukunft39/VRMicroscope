using UnityEngine;
using System;

[RequireComponent(typeof(TutorialButton))]
public class TutorialButtonInput : MonoBehaviour
{
    [Header("依赖配置")]
    public Interactor interactor; 

    [Header("输入间隔（防止连点，单位：秒）")]
    public float inputInterval = 0.2f;

    // 输入阈值，防止摇杆轻微漂移触发
    private float inputThreshold = 0.5f;

    private TutorialButton tutorialButton;
    private float lastInputTime;

    private void Awake()
    {
        tutorialButton = GetComponent<TutorialButton>();
        if (tutorialButton == null)
        {
            Debug.LogError("未找到TutorialButton组件！");
            enabled = false;
        }
    }

    private void Update()
    {
        // 检测WASD按键输入
        HandleWASDInput();
        
        // 检测空格键确认输入
        HandleSpaceConfirmInput();
    }

    /// <summary>
    /// 检测WASD按键输入并转换为Vector2传递给导航逻辑
    /// </summary>
    private void HandleWASDInput()
    {
        // 构建WASD的输入向量（仅在按键按下时触发一次，避免持续输入）
        Vector2 input = Vector2.zero;
        
        // W键（上）- Y轴正方向
        if (Input.GetKeyDown(KeyCode.W))
        {
            input.y = 1f;
        }
        // S键（下）- Y轴负方向
        else if (Input.GetKeyDown(KeyCode.S))
        {
            input.y = -1f;
        }
        // A键（左）- X轴负方向
        else if (Input.GetKeyDown(KeyCode.A))
        {
            input.x = -1f;
        }
        // D键（右）- X轴正方向
        else if (Input.GetKeyDown(KeyCode.D))
        {
            input.x = 1f;
        }

        // 如果有有效输入，调用导航处理方法
        if (input != Vector2.zero)
        {
            HandleNavigate(input);
        }
    }

    /// <summary>
    /// 检测空格键输入并触发确认操作
    /// </summary>
    private void HandleSpaceConfirmInput()
    {
        // 空格键按下时触发确认
        if (Input.GetKeyDown(KeyCode.Space))
        {
            HandleConfirm();
        }
    }

    /// <summary>
    /// 处理导航输入 (WASD / 摇杆)
    /// </summary>
    public void HandleNavigate(Vector2 input)
    {
        // 1. 基础检查：UI级别、冷却时间
        if (!CanInput()) return;

        // 2. 处理 W/S (Y轴) - 切换按钮组
        if (Mathf.Abs(input.y) > inputThreshold)
        {
            // input.y > 0 是 W (上)，对应参数 -1
            // input.y < 0 是 S (下)，对应参数 1
            int direction = input.y > 0 ? -1 : 1;
            
            tutorialButton.loverButtons(direction);
            
            lastInputTime = Time.time;
            Debug.Log($"[{tutorialButton.currentUILevel}级UI] 切换按钮组: {direction}");
            return;
        }

        // 3. 处理 A/D (X轴) - 切换组内按钮
        if (Mathf.Abs(input.x) > inputThreshold)
        {
            // input.x < 0 是 A (左/上)，对应参数 -1
            // input.x > 0 是 D (右/下)，对应参数 1
            int direction = input.x > 0 ? 1 : -1;

            tutorialButton.nextButtons(direction);
            
            lastInputTime = Time.time;
            Debug.Log($"[{tutorialButton.currentUILevel}级UI] 切换组内按钮: {direction}");
        }
    }

    /// <summary>
    /// 处理确认输入 (Space)
    /// </summary>
    public void HandleConfirm()
    {
        if (!CanInput()) return;

        tutorialButton.clickNowButton();
        lastInputTime = Time.time;
        Debug.Log($"[{tutorialButton.currentUILevel}级UI] 触发点击");
    }

    /// <summary>
    /// 统一的条件检查
    /// </summary>
    private bool CanInput()
    {
        // 级别检查 (-1 不执行，必须是 1 或 2)
        if (tutorialButton.currentUILevel == -1 || 
           (tutorialButton.currentUILevel != 1 && tutorialButton.currentUILevel != 2))
        {
            return false;
        }

        // 冷却检查
        if (Time.time - lastInputTime < inputInterval)
        {
            return false;
        }

        return true;
    }
}