using System;
using UnityEngine;

/// <summary>
/// The broadcaster of main game events
/// Open to extension
/// </summary>
public static class GameEvents
{
    public static event Action<string> OnItemCollected;

    public static void ItemCollected(string itemId)
    {
        OnItemCollected?.Invoke(itemId);
    }
}

public class GameEventManager : MonoBehaviour
{
    public Tutorial tutorialUI; // 拖入场景中的教程UI管理器
    public CameraTryMove cameraMove; // 拖入玩家的移动控制脚本

    void Start()
    {
        // 旧的开场自动教程入口已停用。
        // 当前项目统一改由 ForceTutorial 流程控制强制引导。
    }

    public void OnInitialTutorialComplete()
    {
        // 保留该方法是为了兼容旧场景事件绑定，避免缺失引用报错。
        // 当前已不再由这里驱动自动教程流程。
        if (Interactor.Instance != null)
        {
            Interactor.Instance.ChangeState(Interactor.GameState.Roaming);
        }

        if (cameraMove != null)
        {
            cameraMove.canMove = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
