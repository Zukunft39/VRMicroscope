using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    // 单例实例
    public static TutorialManager Instance { get; private set; }

    // 定义一个枚举来代表教程的各个步骤
    public enum TutorialStep
    {
        None,               // 无教程
        EnterLaboratory,    // 1. 首次进入实验室
        ApproachObject,     // 2. 靠近交互物体
        InteractObject      // 3. 与物体交互到一定程度
    }

    // 当前教程步骤 (使用PlayerPrefs持久化)
    public TutorialStep CurrentStep
    {
        get
        {
            // 从PlayerPrefs读取，如果没有则默认为EnterLaboratory
            return (TutorialStep)PlayerPrefs.GetInt("CurrentTutorialStep", (int)TutorialStep.EnterLaboratory);
        }
        private set
        {
            // 设置时同时保存到PlayerPrefs
            PlayerPrefs.SetInt("CurrentTutorialStep", (int)value);
            PlayerPrefs.Save();
        }
    }

    private void Awake()
    {
        // 单例模式
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 确保在场景切换时不被销毁（如果需要的话）
    }

    /// <summary>
    /// 检查并执行当前步骤的教程
    /// </summary>
    public void CheckAndExecuteCurrentTutorial()
    {
        if (CurrentStep == TutorialStep.None)
        {
            Debug.Log("教程已全部完成。");
            return;
        }

        Debug.Log($"开始执行教程步骤: {CurrentStep}");

        // 根据当前步骤执行不同的操作
        switch (CurrentStep)
        {
            case TutorialStep.EnterLaboratory:
                ShowTutorialPanel("欢迎来到实验室！在你面前的是一台重要的设备。");
                // 播放完第一步教程后，自动进入第二步的等待状态
                // 我们不在这里直接设置下一步，而是等待玩家靠近物体的触发器来触发
                break;
            case TutorialStep.ApproachObject:
                ShowTutorialPanel("请靠近这台闪烁的设备，它需要你的操作。");
                break;
            case TutorialStep.InteractObject:
                ShowTutorialPanel("做得好！现在，请按住操作键来启动设备。");
                break;
        }
    }

    /// <summary>
    /// 通知管理器玩家已完成当前步骤
    /// </summary>
    public void CompleteCurrentStep()
    {
        // 将步骤向后移动
        CurrentStep = (TutorialStep)((int)CurrentStep + 1);
        
        Debug.Log($"教程步骤 {CurrentStep - 1} 已完成。当前步骤更新为: {CurrentStep}");

        // 如果还有下一步，准备执行
        if (CurrentStep != TutorialStep.None)
        {
            Debug.Log("等待触发下一步教程...");
        }
        else
        {
            Debug.Log("所有教程步骤已完成！");
            // 可以在这里执行一些收尾工作，比如解锁某些功能
        }
    }

    /// <summary>
    /// 显示教程面板
    /// </summary>
    private void ShowTutorialPanel(string message)
    {
        
    }

    /// <summary>
    /// 当教程面板被玩家关闭时调用
    /// </summary>
    private void OnTutorialPanelClosed()
    {
        Debug.Log("教程面板已关闭。");

        // 根据当前步骤执行后续操作
        switch (CurrentStep)
        {
            case TutorialStep.EnterLaboratory:
                // 第一步教程关闭后，开始让交互物体闪光
                FlashInteractiveObject(true);
                break;
            case TutorialStep.ApproachObject:
                // 第二步教程关闭后，可能不需要做什么，等待玩家交互
                break;
            case TutorialStep.InteractObject:
                // 第三步教程关闭后，可能不需要做什么，等待玩家完成交互
                break;
        }
    }

    /// <summary>
    /// 控制交互物体的闪光效果
    /// </summary>
    private void FlashInteractiveObject(bool isFlashing)
    {
        
    }

    /// <summary>
    /// 重置所有教程进度 (用于测试)
    /// </summary>
    public void ResetTutorialProgress()
    {
        CurrentStep = TutorialStep.EnterLaboratory;
        Debug.Log("教程进度已重置。");
    }
}