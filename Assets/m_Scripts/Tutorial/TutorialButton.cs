using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
[System.Serializable]
public struct buttonsLevels
{
    [Header("Button / Video Containers in This Group")]
    public GameObject[] buttons; // 容器：可包含Button、视频等组件
}

[System.Serializable]
public struct UILevel
{
    [Header("Button Groups in This Level")]
    public buttonsLevels[] theButtons;
}

public class TutorialButton : MonoBehaviour
{
    #region 变量
    [Header("Button Styles")]
    public Color highlightColor = new Color(1f, 0.92f, 0.016f); // 高亮黄色（仅背景）
    public Color normalColor = Color.white;                     // 默认背景色

    public float highlightScale = 1.35f;                        // 高亮缩放比例
    public float videoSelectScale = 2.5f;                  // 视频容器选中时的缩放比例
    [Header("Preserve Original Text Colors (Recommended)")]
    public bool keepTextOriginalColor = true; // 新增：控制是否保留文字颜色

    [Header("Level Configuration")]
    public List<UILevel> UILevels = new List<UILevel>();

    [Header("Default Focus")]
    public bool preferNamedDefaultButton = true;
    public List<string> preferredDefaultButtonKeywords = new List<string> { "Back", "Close" };

    [Header("Current Selection")]
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
    
    // 新增：当前UI层级（-1/1/2），用于控制输入逻辑
    public int currentUILevel = 1; // 默认一级UI
    
    // 依赖的核心脚本
    private Tutorial tutorial;
    // 缩放动画的携程
    private Coroutine pulseCoroutine;
    #endregion

    private void Start()
    {
        // 修正：Tutorial脚本不在当前物体上，改为查找或通过Inspector赋值
        if (tutorial == null)
        {
            tutorial = FindObjectOfType<Tutorial>();
            
            ForceResetAllScales(); // 物理重置，防止 Prefab 里的原始缩放干扰
            InitializeButtons();
            SelectDefaultButton(levelIndex);
        }
    }

    /// <summary>
    /// 新增：强制重置所有按钮容器的缩放为1，防止Prefab里原始缩放干扰高亮效果
    /// </summary>
    private void ForceResetAllScales()
    {
        foreach (var level in UILevels)
        {
            foreach (var group in level.theButtons)
            {
                foreach (var obj in group.buttons)
                {
                    if (obj != null) obj.transform.localScale = Vector3.one;
                }
            }
        }
    }
    /// <summary>
    /// 初始化所有按钮容器的事件和默认样式
    /// </summary>
    private void InitializeButtons()
    {
        if (UILevels.Count == 0)
        {
            Debug.LogWarning("UILevels is empty. Configure levels in the Inspector.");
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
                        Debug.LogWarning($"UILevel[{currentLevelIdx}] -> group[{currentBtnGroupIdx}] -> object[{currentObjIdx}] is null.");
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
                        Debug.LogWarning($"Button component not found on GameObject[{obj.name}].");
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
            Debug.LogWarning($"Requested level index [{levelIdx}] is invalid.");
            return;
        }

        UILevel targetLevel = UILevels[levelIdx];
        levelIndex=levelIdx;

        if (TryFindPreferredDefaultButton(levelIdx, out int preferredGroupIndex, out int preferredObjectIndex, out GameObject preferredObject, out Button preferredButton))
        {
            buttonIndex = preferredGroupIndex;
            index = preferredObjectIndex;
            nowSelectObj = preferredObject;
            nowButton = preferredButton;
            hilightButton(preferredButton);
            OnButtonSelected?.Invoke(preferredButton, preferredObject, targetLevel, targetLevel.theButtons[preferredGroupIndex]);
            return;
        }

        // 选中第二组（索引1），如果不足则选第一组
        int defaultBtnGroupIdx = targetLevel.theButtons.Length >= 2 ? 2 : 0;

        if (defaultBtnGroupIdx >= targetLevel.theButtons.Length)
        {
            Debug.LogWarning($"Level [{levelIdx}] has no second button group.");
            return;
        }

        buttonsLevels targetBtnGroup = targetLevel.theButtons[defaultBtnGroupIdx];
        if (targetBtnGroup.buttons.Length == 0)
        {
            Debug.LogWarning($"Level[{levelIdx}] -> group[{defaultBtnGroupIdx}] has no objects.");
            return;
        }

        // 获取默认对象和Button组件
        GameObject defaultObj = targetBtnGroup.buttons[0];
        Button defaultBtn = defaultObj.GetComponent<Button>();

        if (defaultBtn != null)
        {

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
            Debug.LogWarning($"Default object [{defaultObj.name}] has no Button component.");
        }
    }

    private bool TryFindPreferredDefaultButton(int levelIdx, out int btnGroupIdx, out int objIdx, out GameObject targetObj, out Button targetBtn)
    {
        btnGroupIdx = -1;
        objIdx = -1;
        targetObj = null;
        targetBtn = null;

        if (!preferNamedDefaultButton || preferredDefaultButtonKeywords == null || preferredDefaultButtonKeywords.Count == 0)
        {
            return false;
        }

        UILevel targetLevel = UILevels[levelIdx];
        for (int i = 0; i < targetLevel.theButtons.Length; i++)
        {
            buttonsLevels btnGroup = targetLevel.theButtons[i];
            for (int j = 0; j < btnGroup.buttons.Length; j++)
            {
                GameObject candidateObj = btnGroup.buttons[j];
                if (candidateObj == null)
                {
                    continue;
                }

                Button candidateButton = candidateObj.GetComponent<Button>();
                if (candidateButton == null)
                {
                    continue;
                }

                if (!MatchesPreferredDefaultButton(candidateObj, candidateButton))
                {
                    continue;
                }

                btnGroupIdx = i;
                objIdx = j;
                targetObj = candidateObj;
                targetBtn = candidateButton;
                return true;
            }
        }

        return false;
    }

    private bool MatchesPreferredDefaultButton(GameObject obj, Button button)
    {
        if (obj == null || button == null)
        {
            return false;
        }

        string buttonText = string.Empty;
        Text legacyText = button.GetComponentInChildren<Text>(true);
        if (legacyText != null)
        {
            buttonText = legacyText.text;
        }

        if (string.IsNullOrWhiteSpace(buttonText))
        {
            TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
            if (tmpText != null)
            {
                buttonText = tmpText.text;
            }
        }

        for (int i = 0; i < preferredDefaultButtonKeywords.Count; i++)
        {
            string keyword = preferredDefaultButtonKeywords[i];
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            if (ContainsIgnoreCase(obj.name, keyword) || ContainsIgnoreCase(button.name, keyword) || ContainsIgnoreCase(buttonText, keyword))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsIgnoreCase(string source, string value)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
        {
            return false;
        }

        return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
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
            Debug.LogWarning("Current level/group is invalid; cannot switch buttons.");
            return;
        }

        buttonsLevels currentBtnGroup = UILevels[levelIndex].theButtons[buttonIndex];
        int newIndex = index + (level > 0 ? 1 : -1);

        // 边界检查：组内对象索引
        if (newIndex < 0)
        {
            Debug.Log("Already at the first object in this button group.");
            return;
        }
        if (newIndex >= currentBtnGroup.buttons.Length)
        {
            Debug.Log("Already at the last object in this button group.");
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
            Debug.LogWarning($"Object [{newObj.name}] has no Button component and cannot be highlighted.");
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
            Debug.LogWarning("Current level is invalid; cannot switch button groups.");
            return;
        }

        UILevel currentLevel = UILevels[levelIndex];
        int newBtnGroupIndex = buttonIndex + (level > 0 ? 1 : -1);

        // 边界检查：按钮组索引
        if (newBtnGroupIndex < 0)
        {
            Debug.Log("Already at the first button group in this level.");
            return;
        }
        if (newBtnGroupIndex >= currentLevel.theButtons.Length)
        {
            Debug.Log("Already at the last button group in this level.");
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
            Debug.LogWarning($"UILevel[{levelIndex}] -> group[{buttonIndex}] has no objects.");
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
            Debug.LogWarning($"Object [{newObj.name}] has no Button component.");
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
            Debug.Log("Already at the first level.");
            return;
        }
        if (newLevelIndex >= UILevels.Count)
        {
            Debug.Log("Already at the last level.");
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
            Debug.LogWarning($"UILevel[{levelIndex}] has no button groups.");
            nowButton = null;
            nowSelectObj = null;
            return;
        }

        buttonsLevels newBtnGroup = newLevel.theButtons[buttonIndex];
        // 检查新按钮组是否有对象
        if (newBtnGroup.buttons.Length == 0)
        {
            Debug.LogWarning($"UILevel[{levelIndex}] -> group[{buttonIndex}] has no objects.");
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
            Debug.LogWarning($"Object [{newObj.name}] has no Button component.");
        }
    }

    /// <summary>
    /// 选中按钮高亮（视觉反馈）- 核心修改：仅改背景，不改文字
    /// </summary>
    public void hilightButton(Button button)
    {
        if (button == null) return;

        // 将当前选中的物体在层级面板中移到最后，使其渲染在最顶层
        button.transform.SetAsLastSibling();

        // 无论选中什么，先停止上一个按钮的动画（防止快速切换时动画残留）
        if (pulseCoroutine != null) 
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        // 判断当前选中的是不是视频容器
        if (button.CompareTag("VideoContainer"))
        {
            // 【视频的专属逻辑】
            // 不改颜色、不播动画，直接赋予一个固定的放大倍数
            button.transform.localScale = Vector3.one * videoSelectScale; 
        }
        else
        {
            // 【普通按钮的专属逻辑】
            // 1. 改变背景颜色
            Image btnImage = button.GetComponent<Image>();
            if (btnImage != null) btnImage.color = highlightColor;

            // 2. 开启呼吸循环动画
            pulseCoroutine = StartCoroutine(PulseAnimation(button.transform));
        }

        // 确保按钮可交互
        button.interactable = true;
    }

    /// <summary>
    /// 按钮恢复正常状态 - 核心修改：仅恢复背景，文字保持原始颜色
    /// </summary>
    public void returnNormalButton(Button button)
    {
        if (button == null) return;

        // 1. 停止可能正在运行的动画
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        // 2. 强制所有对象（无论是视频还是普通按钮）恢复原始大小
        button.transform.localScale = Vector3.one;
        
        // 3. 只有非视频容器才需要恢复背景颜色
        if (!button.CompareTag("VideoContainer"))
        {
            Image btnImage = button.GetComponent<Image>();
            if (btnImage != null) btnImage.color = normalColor;
        }
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
            Debug.LogWarning("No selected or interactable button.");
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
            Debug.LogWarning($"Object index [{objIdx}] is invalid.");
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
            Debug.LogWarning($"Object [{targetObj.name}] has no Button component.");
        }
    }

    /// <summary>
    /// 检查层级和按钮组是否有效
    /// </summary>
    private bool IsLevelAndGroupValid(int levelIdx, int btnGroupIdx)
    {
        if (levelIdx < 0 || levelIdx >= UILevels.Count)
        {
            Debug.LogWarning($"Level index [{levelIdx}] is invalid.");
            return false;
        }

        if (btnGroupIdx < 0 || btnGroupIdx >= UILevels[levelIdx].theButtons.Length)
        {
            Debug.LogWarning($"Button group index [{btnGroupIdx}] is invalid.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 核心完善：切换UI级别（-1/1/2）
    /// </summary>
    /// <param name="newLevel">新的UI级别：-1（无输入）、1（一级UI）、2（二级UI）</param>
    public void ChangeLevel(int newLevel)
    {
        // 更新当前UI层级
        currentUILevel = newLevel;
        
        // 根据不同级别处理逻辑
        switch (newLevel)
        {
            case -1:
                // 隐藏所有按钮交互，恢复所有按钮默认状态
                ResetAllButtonsToNormal();
                nowButton = null;
                nowSelectObj = null;
                Debug.Log("Input disabled; button selection reset.");
                break;
            
            case 1:
                // 切换到一级UI：选中一级UI对应的默认按钮
                ResetAllButtonsToNormal();
                // 一级UI对应UILevels[0]（可根据你的配置调整）
                SelectDefaultButton(0);
                Debug.Log("Switched to UI level 1; default button selected.");
                break;
            
            case 2:
                // 切换到二级UI：选中二级UI对应的默认按钮
                ResetAllButtonsToNormal();
                // 二级UI对应UILevels[1]（可根据你的配置调整）
                SelectDefaultButton(1);
                Debug.Log("Switched to UI level 2; default button selected.");
                break;
            
            default:
                Debug.LogWarning($"Invalid UI level {newLevel}; falling back to level 1.");
                currentUILevel = 1;
                ResetAllButtonsToNormal();
                SelectDefaultButton(0);
                break;
        }
    }

    /// <summary>
    /// 新增：重置所有按钮为默认状态
    /// </summary>
    private void ResetAllButtonsToNormal()
    {
        if (UILevels.Count == 0) return;
        
        foreach (var uiLevel in UILevels)
        {
            foreach (var btnGroup in uiLevel.theButtons)
            {
                foreach (var obj in btnGroup.buttons)
                {
                    if (obj != null)
                    {
                        Button btn = obj.GetComponent<Button>();
                        if (btn != null)
                        {
                            returnNormalButton(btn);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 缩放动画：1.0 -> highlightScale -> 1.0
    /// </summary>
    private System.Collections.IEnumerator PulseAnimation(Transform target)
    {
        float halfDuration = 0.4f; // 从1到highlightScale所需的时间（数值越大呼吸越慢）
        Vector3 initialScale = Vector3.one;
        Vector3 peakScale = Vector3.one * highlightScale;

        while (true) // 无限循环
        {
            // 第一阶段：放大
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                // 使用 SmoothStep 让呼吸感更柔和（平滑起止）
                float t = Mathf.SmoothStep(0, 1, elapsed / halfDuration);
                target.localScale = Vector3.Lerp(initialScale, peakScale, t);
                yield return null;
            }

            // 第二阶段：缩小
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / halfDuration);
                target.localScale = Vector3.Lerp(peakScale, initialScale, t);
                yield return null;
            }
        }
    }
}
