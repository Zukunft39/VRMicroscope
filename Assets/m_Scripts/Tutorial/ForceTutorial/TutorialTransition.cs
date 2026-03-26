using UnityEngine;

/// <summary>
/// 处理教程结束后的过渡与跳转逻辑。
/// 挂载在 StandaloneTutorialUI 所在的 GameObject，并在 OnTutorialFinish 事件中调用。
/// </summary>
public class TutorialTransition : MonoBehaviour
{
    [Header("自动队列跳转")]
    [Tooltip("将包含当前教程的 MandatoryTutorialTrigger 拖入此处。\n完成后会自动通知控制器(SequenceController)启动下一个。")]
    public MandatoryTutorialTrigger currentTriggerToComplete;

    /// <summary>
    /// 将此方法绑定到 StandaloneTutorialUI 的 onTutorialFinish 事件中
    /// </summary>
    public void TriggerNextTutorial()
    {
        if (currentTriggerToComplete != null)
        {
            currentTriggerToComplete.CompleteTutorial();
        }
    }
}
