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
        if (tutorialUI != null && tutorialUI.CheckFirstLaunch("Tutorial_Game_Start"))
        {
            // 1. 显示游戏的初始强制教程 (假设它的索引是 0)
            tutorialUI.ShowTutorial(0); 
            
            // 2. 更改游戏状态，阻止其他按键交互
            Interactor.Instance.ChangeState(Interactor.GameState.Tutorial);
            
            // 3. 锁定玩家的移动和视角
            if (cameraMove != null)
            {
                cameraMove.canMove = false;
                
                // 强制解锁鼠标，以便玩家可以点击UI按钮
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    // 这个方法可以绑定到你教程UI的“完成”或“下一步”按钮上
    public void OnInitialTutorialComplete()
    {
        // 恢复正常游戏状态
        Interactor.Instance.ChangeState(Interactor.GameState.Roaming); // 假设有Idle或Normal状态
        
        // 恢复玩家的移动和视角控制
        if (cameraMove != null)
        {
            cameraMove.canMove = true;
            
            // 重新隐藏并锁定鼠标
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}