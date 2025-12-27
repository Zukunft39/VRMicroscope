using UnityEngine;
using System;

/// <summary>
/// WASD控制按钮选择 + 空格触发的脚本
/// W/S：切换按钮组（二级层级）
/// A/D：切换组内按钮（三级层级）
/// 空格：触发当前选中按钮的点击事件
/// 新增：根据UI级别（-1/1/2）控制输入逻辑
/// </summary>
[RequireComponent(typeof(TutorialButton))]
public class TutorialButtonInput : MonoBehaviour
{
    [Header("按键配置")]
    public KeyCode upGroupKey = KeyCode.W;       // 上一组（W）
    public KeyCode downGroupKey = KeyCode.S;     // 下一组（S）
    public KeyCode upItemKey = KeyCode.A;        // 组内上一个（A）
    public KeyCode downItemKey = KeyCode.D;      // 组内下一个（D）
    public KeyCode triggerKey = KeyCode.Space;   // 触发按钮（空格）

    [Header("输入间隔（防止连点，单位：秒）")]
    public float inputInterval = 0.2f;

    // 依赖的核心脚本
    private TutorialButton tutorialButton;
    // 输入冷却计时
    private float lastInputTime;

    private void Awake()
    {
        // 获取按钮控制核心脚本
        tutorialButton = GetComponent<TutorialButton>();
        if (tutorialButton == null)
        {
            Debug.LogError("未找到TutorialButton组件，请确保该脚本与TutorialButton挂载在同一物体上！");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        // 核心判断：如果当前UI级别是-1，不执行任何输入逻辑
        if (tutorialButton.currentUILevel == -1)
        {
            return;
        }

        // 输入冷却：防止按键连点
        if (Time.time - lastInputTime < inputInterval)
        {
            return;
        }

        // 检测按键输入（根据不同UI级别执行不同逻辑）
        CheckGroupSwitchInput();  // W/S 切换按钮组
        CheckItemSwitchInput();   // A/D 切换组内按钮
        CheckTriggerInput();      // 空格 触发按钮
    }

    /// <summary>
    /// 检测W/S切换按钮组的输入（根据UI级别控制是否执行）
    /// </summary>
    private void CheckGroupSwitchInput()
    {
        // 只有一级/二级UI才执行按钮组切换
        if (tutorialButton.currentUILevel != 1 && tutorialButton.currentUILevel != 2)
        {
            return;
        }

        // 上一组（W）：传递-1表示向上切换
        if (Input.GetKeyDown(upGroupKey))
        {
            tutorialButton.loverButtons(-1);
            lastInputTime = Time.time;
            Debug.Log($"[{tutorialButton.currentUILevel}级UI] 切换到上一组按钮");
        }

        // 下一组（S）：传递1表示向下切换
        if (Input.GetKeyDown(downGroupKey))
        {
            tutorialButton.loverButtons(1);
            lastInputTime = Time.time;
            Debug.Log($"[{tutorialButton.currentUILevel}级UI] 切换到下一组按钮");
        }
    }

    /// <summary>
    /// 检测A/D切换组内按钮的输入（根据UI级别控制是否执行）
    /// </summary>
    private void CheckItemSwitchInput()
    {
        // 只有一级/二级UI才执行组内按钮切换
        if (tutorialButton.currentUILevel != 1 && tutorialButton.currentUILevel != 2)
        {
            return;
        }

        // 组内上一个（A）：传递-1表示向上切换
        if (Input.GetKeyDown(upItemKey))
        {
            tutorialButton.nextButtons(-1);
            lastInputTime = Time.time;
            Debug.Log($"[{tutorialButton.currentUILevel}级UI] 切换到组内上一个按钮");
        }

        // 组内下一个（D）：传递1表示向下切换
        if (Input.GetKeyDown(downItemKey))
        {
            tutorialButton.nextButtons(1);
            lastInputTime = Time.time;
            Debug.Log($"[{tutorialButton.currentUILevel}级UI] 切换到组内下一个按钮");
        }
    }

    /// <summary>
    /// 检测空格键触发按钮的输入（根据UI级别控制是否执行）
    /// </summary>
    private void CheckTriggerInput()
    {
        // 只有一级/二级UI才允许触发按钮
        if (tutorialButton.currentUILevel != 1 && tutorialButton.currentUILevel != 2)
        {
            return;
        }

        if (Input.GetKeyDown(triggerKey))
        {
            tutorialButton.clickNowButton();
            lastInputTime = Time.time;
            Debug.Log($"[{tutorialButton.currentUILevel}级UI] 触发当前选中按钮的点击事件");
        }
    }

    /// <summary>
    /// 新增：外部控制是否启用输入（可选）
    /// </summary>
    /// <param name="enable">是否启用</param>
    public void SetInputEnabled(bool enable)
    {
        this.enabled = enable;
    }
}