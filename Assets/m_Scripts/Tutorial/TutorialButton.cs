using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct buttonsLevels
{
    [Header("当前按钮组内的按钮/视频容器集合")]
    public GameObject[] buttons; // 容器：可包含Button、视频等组件
}

[System.Serializable]
public struct UILevel
{
    [Header("当前一级层级下的所有按钮组")]
    public buttonsLevels[] theButtons;
}

public class TutorialButton : MonoBehaviour
{
    #region 变量
    [Header("按钮样式配置")]
    public Color highlightColor = new Color(1f, 0.92f, 0.016f); // 高亮黄色（仅背景）
    public Color normalColor = Color.white;                     // 默认背景色

    public float highlightScale = 1.1f;                        // 高亮缩放比例
    [Header("是否保留文字原始颜色（推荐开启）")]
    public bool keepTextOriginalColor = true; // 新增：控制是否保留文字颜色

    [Header("层级数据配置")]
    public List<UILevel> UILevels = new List<UILevel>();

    [Header("当前选中状态")]
    public Button nowButton;          // 当前选中的按钮组件
    public GameObject nowSelectObj;   // 当前选中的GameObject（按钮/视频容器）
    public int levelIndex = 0;        // 一级层级索引（UILevel列表的索引）
    private int buttonIndex = 0;      // 二级按钮组索引（theButtons数组的索引）
    private int index = 0;            // 三级：按钮组内的对象索引

    // 新增：存储每个按钮文字的原始颜色（避免高亮后无法恢复）
    private Dictionary<Button, Color> btnTextOriginalColors = new Dictionary<Button, Color>();

    // 事件回调（便于外部扩展逻辑）
    public Action<Button, GameObject, UILevel, buttonsLevels> OnButtonSelected; // 按钮选中事件（含GameObject）
    public Action<Button> OnButtonClicked;                                      // 按钮点击事件

    #endregion

    private void Start()
    {
        // 初始化按钮事件和默认状态
        InitializeButtons();
        // 默认选中指定层级的第二个按钮组第一个按钮（按你的需求）
        SelectDefaultButton(levelIndex);
    }

    /// <summary>
    /// 初始化所有按钮容器的事件和默认样式
    /// </summary>
    private void InitializeButtons()
    {
        if (UILevels.Count == 0)
        {
            Debug.LogWarning("UILevels列表为空，请在Inspector中配置层级数据！");
            return;
        }

        // 遍历所有一级层级
        for (int i = 0; i < UILevels.Count; i++)
        {
            int currentLevelIdx = i;
            UILevel uiLevel = UILevels[i];

            // 遍历当前层级下的所有按钮组
            for (int j = 0; j < uiLevel.theButtons.Length; j++)
            {
                int currentBtnGroupIdx = j;
                buttonsLevels btnGroup = uiLevel.theButtons[j];

                // 遍历按钮组内的所有GameObject容器
                for (int k = 0; k < btnGroup.buttons.Length; k++)
                {
                    int currentObjIdx = k;
                    GameObject obj = btnGroup.buttons[k];

                    if (obj == null)
                    {
                        Debug.LogWarning($"UILevel[{currentLevelIdx}] -> 按钮组[{currentBtnGroupIdx}] -> 对象[{currentObjIdx}] 为空！");
                        continue;
                    }

                    // 获取GameObject上的Button组件（核心适配）
                    Button btn = obj.GetComponent<Button>();
                    if (btn != null)
                    {
                        // 初始化按钮为默认状态
                        returnNormalButton(btn);

                        // 新增：记录按钮文字的原始颜色
                        Text btnText = btn.GetComponentInChildren<Text>();
                        if (btnText != null && keepTextOriginalColor)
                        {
                            btnTextOriginalColors[btn] = btnText.color;
                        }

                        // 绑定按钮点击事件（触发自身事件+更新选中状态）
                        btn.onClick.AddListener(() =>
                        {
                            // 更新选中状态
                            levelIndex = currentLevelIdx;
                            buttonIndex = currentBtnGroupIdx;
                            index = currentObjIdx;
                            nowSelectObj = obj;
                            nowButton = btn;

                            // 高亮当前按钮
                            hilightButton(btn);

                            // 触发回调（外部可监听）
                            OnButtonSelected?.Invoke(btn, obj, UILevels[currentLevelIdx], btnGroup);
                            OnButtonClicked?.Invoke(btn);
                        });
                    }
                    else
                    {
                        Debug.LogWarning($"GameObject[{obj.name}] 上未找到Button组件！");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 选中默认按钮（指定层级的第二组第一个按钮）
    /// </summary>
    /// <param name="levelIdx">要选中的一级层级索引</param>
    private void SelectDefaultButton(int levelIdx)
    {
        // 边界检查
        if (levelIdx < 0 || levelIdx >= UILevels.Count)
        {
            Debug.LogWarning($"指定层级索引[{levelIdx}]无效！");
            return;
        }

        UILevel targetLevel = UILevels[levelIdx];
        // 选中第二组（索引1），如果不足则选第一组
        int defaultBtnGroupIdx = targetLevel.theButtons.Length >= 2 ? 1 : 0;

        if (defaultBtnGroupIdx >= targetLevel.theButtons.Length)
        {
            Debug.LogWarning($"层级[{levelIdx}]下无第二组按钮！");
            return;
        }

        buttonsLevels targetBtnGroup = targetLevel.theButtons[defaultBtnGroupIdx];
        if (targetBtnGroup.buttons.Length == 0)
        {
            Debug.LogWarning($"层级[{levelIdx}] -> 按钮组[{defaultBtnGroupIdx}] 下无对象！");
            return;
        }

        // 获取默认对象和Button组件
        GameObject defaultObj = targetBtnGroup.buttons[0];
        Button defaultBtn = defaultObj.GetComponent<Button>();

        if (defaultBtn != null)
        {
            // 更新选中状态
            levelIndex = levelIdx;
            buttonIndex = defaultBtnGroupIdx;
            index = 0;
            nowSelectObj = defaultObj;
            nowButton = defaultBtn;

            // 高亮默认按钮
            hilightButton(defaultBtn);
            OnButtonSelected?.Invoke(defaultBtn, defaultObj, targetLevel, targetBtnGroup);
        }
        else
        {
            Debug.LogWarning($"默认对象[{defaultObj.name}] 上无Button组件！");
        }
    }

    /// <summary>
    /// 切换当前按钮组内的上/下一个对象（三级切换）
    /// </summary>
    /// <param name="level">level<0：上一个，level>0：下一个</param>
    public void nextButtons(int level)
    {
        // 边界检查：当前一级层级和按钮组是否有效
        if (!IsCurrentLevelValid() || !IsCurrentBtnGroupValid())
        {
            Debug.LogWarning("当前层级/按钮组无效，无法切换按钮！");
            return;
        }

        buttonsLevels currentBtnGroup = UILevels[levelIndex].theButtons[buttonIndex];
        int newIndex = index + (level > 0 ? 1 : -1);

        // 边界检查：组内对象索引
        if (newIndex < 0)
        {
            Debug.Log("已经是当前按钮组内的第一个对象");
            return;
        }
        if (newIndex >= currentBtnGroup.buttons.Length)
        {
            Debug.Log("已经是当前按钮组内的最后一个对象");
            return;
        }

        // 恢复上一个按钮的正常状态
        if (nowButton != null)
        {
            returnNormalButton(nowButton);
        }

        // 更新索引并获取新对象
        index = newIndex;
        GameObject newObj = currentBtnGroup.buttons[index];
        nowSelectObj = newObj;

        // 获取新对象上的Button组件
        Button newBtn = newObj.GetComponent<Button>();
        if (newBtn != null)
        {
            nowButton = newBtn;
            hilightButton(newBtn);
            // 触发选中回调（不触发点击事件，仅选中）
            OnButtonSelected?.Invoke(newBtn, newObj, UILevels[levelIndex], currentBtnGroup);
        }
        else
        {
            nowButton = null;
            Debug.LogWarning($"对象[{newObj.name}] 上无Button组件，无法高亮！");
        }
    }

    /// <summary>
    /// 切换当前一级层级下的上/下一组按钮（二级切换）
    /// </summary>
    /// <param name="level">level<0：上一组，level>0：下一组</param>
    public void loverButtons(int level)
    {
        // 边界检查：当前一级层级是否有效
        if (!IsCurrentLevelValid())
        {
            Debug.LogWarning("当前一级层级无效，无法切换按钮组！");
            return;
        }

        UILevel currentLevel = UILevels[levelIndex];
        int newBtnGroupIndex = buttonIndex + (level > 0 ? 1 : -1);

        // 边界检查：按钮组索引
        if (newBtnGroupIndex < 0)
        {
            Debug.Log("已经是当前层级下的第一组按钮");
            return;
        }
        if (newBtnGroupIndex >= currentLevel.theButtons.Length)
        {
            Debug.Log("已经是当前层级下的最后一组按钮");
            return;
        }

        // 恢复上一个按钮的正常状态
        if (nowButton != null)
        {
            returnNormalButton(nowButton);
        }

        // 更新按钮组索引，重置组内对象为第一个
        buttonIndex = newBtnGroupIndex;
        index = 0;

        buttonsLevels newBtnGroup = currentLevel.theButtons[buttonIndex];
        // 检查新按钮组内是否有对象
        if (newBtnGroup.buttons.Length == 0)
        {
            Debug.LogWarning($"UILevel[{levelIndex}] -> 按钮组[{buttonIndex}] 下无对象！");
            nowButton = null;
            nowSelectObj = null;
            return;
        }

        // 获取新对象和Button组件
        GameObject newObj = newBtnGroup.buttons[index];
        nowSelectObj = newObj;
        Button newBtn = newObj.GetComponent<Button>();

        if (newBtn != null)
        {
            nowButton = newBtn;
            hilightButton(newBtn);
            // 触发选中回调
            OnButtonSelected?.Invoke(newBtn, newObj, currentLevel, newBtnGroup);
        }
        else
        {
            nowButton = null;
            Debug.LogWarning($"对象[{newObj.name}] 上无Button组件！");
        }
    }

    /// <summary>
    /// 切换上/下一个一级层级（扩展方法）
    /// </summary>
    /// <param name="level">level<0：上一层级，level>0：下一层级</param>
    public void switchLevel(int level)
    {
        int newLevelIndex = levelIndex + (level > 0 ? 1 : -1);

        // 边界检查：一级层级索引
        if (newLevelIndex < 0)
        {
            Debug.Log("已经是第一个一级层级");
            return;
        }
        if (newLevelIndex >= UILevels.Count)
        {
            Debug.Log("已经是最后一个一级层级");
            return;
        }

        // 恢复上一个按钮状态
        if (nowButton != null)
        {
            returnNormalButton(nowButton);
        }

        // 更新一级层级索引，重置按钮组和组内对象为第一个
        levelIndex = newLevelIndex;
        buttonIndex = 0;
        index = 0;

        UILevel newLevel = UILevels[levelIndex];
        // 检查新层级是否有按钮组
        if (newLevel.theButtons.Length == 0)
        {
            Debug.LogWarning($"UILevel[{levelIndex}] 下无按钮组！");
            nowButton = null;
            nowSelectObj = null;
            return;
        }

        buttonsLevels newBtnGroup = newLevel.theButtons[buttonIndex];
        // 检查新按钮组是否有对象
        if (newBtnGroup.buttons.Length == 0)
        {
            Debug.LogWarning($"UILevel[{levelIndex}] -> 按钮组[{buttonIndex}] 下无对象！");
            nowButton = null;
            nowSelectObj = null;
            return;
        }

        // 获取新对象和Button组件
        GameObject newObj = newBtnGroup.buttons[index];
        nowSelectObj = newObj;
        Button newBtn = newObj.GetComponent<Button>();

        if (newBtn != null)
        {
            nowButton = newBtn;
            hilightButton(newBtn);
            // 触发选中回调
            OnButtonSelected?.Invoke(newBtn, newObj, newLevel, newBtnGroup);
        }
        else
        {
            nowButton = null;
            Debug.LogWarning($"对象[{newObj.name}] 上无Button组件！");
        }
    }

    /// <summary>
    /// 选中按钮高亮（视觉反馈）- 核心修改：仅改背景，不改文字
    /// </summary>
    public void hilightButton(Button button)
    {
        if (button == null) return;

        // 1. 仅修改按钮背景（Image）颜色
        Image btnImage = button.GetComponent<Image>();
        if (btnImage != null)
        {
            btnImage.color = highlightColor;
        }

        // 2. 保留文字原始颜色（核心修改：注释掉文字颜色修改）
        // Text btnText = button.GetComponentInChildren<Text>();
        // if (btnText != null)
        // {
        //     btnText.color = highlightColor; // 删掉这行，不再修改文字颜色
        // }

        // 3. 缩放高亮（视觉强化）
        button.transform.localScale = Vector3.one * highlightScale;

        // 4. 确保按钮可交互
        button.interactable = true;
    }

    /// <summary>
    /// 按钮恢复正常状态 - 核心修改：仅恢复背景，文字保持原始颜色
    /// </summary>
    public void returnNormalButton(Button button)
    {
        if (button == null) return;

        // 1. 恢复背景颜色
        Image btnImage = button.GetComponent<Image>();
        if (btnImage != null)
        {
            btnImage.color = normalColor;
        }

        // // 2. 恢复文字为原始颜色（核心修改）
        // if (keepTextOriginalColor && btnTextOriginalColors.ContainsKey(button))
        // {
        //     Text btnText = button.GetComponentInChildren<Text>();
        //     if (btnText != null)
        //     {
        //         btnText.color = btnTextOriginalColors[button];
        //     }
        // }

        // 3. 恢复缩放
        button.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// 点击当前选中的按钮（触发按钮自身的onClick事件）
    /// </summary>
    public void clickNowButton()
    {
        if (nowButton != null && nowButton.interactable)
        {
            // 触发按钮自身绑定的所有事件
            nowButton.onClick.Invoke();
            OnButtonClicked?.Invoke(nowButton);
        }
        else
        {
            Debug.LogWarning("当前无选中按钮或按钮不可交互！");
        }
    }

    /// <summary>
    /// 检查当前一级层级是否有效
    /// </summary>
    private bool IsCurrentLevelValid()
    {
        return levelIndex >= 0 && levelIndex < UILevels.Count;
    }

    /// <summary>
    /// 检查当前按钮组是否有效
    /// </summary>
    private bool IsCurrentBtnGroupValid()
    {
        if (!IsCurrentLevelValid()) return false;
        return buttonIndex >= 0 && buttonIndex < UILevels[levelIndex].theButtons.Length;
    }

    /// <summary>
    /// 外部调用：手动选中指定层级的指定对象
    /// </summary>
    /// <param name="levelIdx">一级层级索引</param>
    /// <param name="btnGroupIdx">按钮组索引</param>
    /// <param name="objIdx">组内对象索引</param>
    public void SelectSpecificObj(int levelIdx, int btnGroupIdx, int objIdx)
    {
        if (!IsLevelAndGroupValid(levelIdx, btnGroupIdx)) return;

        buttonsLevels targetGroup = UILevels[levelIdx].theButtons[btnGroupIdx];
        if (objIdx < 0 || objIdx >= targetGroup.buttons.Length)
        {
            Debug.LogWarning($"组内对象索引[{objIdx}]无效！");
            return;
        }

        // 恢复上一个按钮状态
        if (nowButton != null) returnNormalButton(nowButton);

        // 更新索引
        levelIndex = levelIdx;
        buttonIndex = btnGroupIdx;
        index = objIdx;

        // 获取目标对象和Button组件
        GameObject targetObj = targetGroup.buttons[objIdx];
        nowSelectObj = targetObj;
        Button targetBtn = targetObj.GetComponent<Button>();

        if (targetBtn != null)
        {
            nowButton = targetBtn;
            hilightButton(targetBtn);
            OnButtonSelected?.Invoke(targetBtn, targetObj, UILevels[levelIdx], targetGroup);
        }
        else
        {
            nowButton = null;
            Debug.LogWarning($"对象[{targetObj.name}] 上无Button组件！");
        }
    }

    /// <summary>
    /// 检查层级和按钮组是否有效
    /// </summary>
    private bool IsLevelAndGroupValid(int levelIdx, int btnGroupIdx)
    {
        if (levelIdx < 0 || levelIdx >= UILevels.Count)
        {
            Debug.LogWarning($"层级索引[{levelIdx}]无效！");
            return false;
        }

        if (btnGroupIdx < 0 || btnGroupIdx >= UILevels[levelIdx].theButtons.Length)
        {
            Debug.LogWarning($"按钮组索引[{btnGroupIdx}]无效！");
            return false;
        }

        return true;
    }
}