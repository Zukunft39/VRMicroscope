using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace VRMicroscope.Tutorial
{
    /// <summary>
    /// 该组件负责阻止和恢复玩家的输入。
    /// 建议将其放置在 XR Origin 或其父对象上。
    /// 它通过禁用 ActionBasedControllerManager 或独立的 XRBaseController 组件来工作。
    /// </summary>
    public class PlayerInputBlocker : MonoBehaviour
    {
        [Header("XR 交互组件")]
        [SerializeField]
        [Tooltip("【可选】指定 ActionBasedControllerManager。如果未设置，将在该游戏对象的子节点中查找。")]
        private ActionBasedControllerManager[] _controllerManagers;

        [SerializeField]
        [Tooltip("【可选】指定 XRBaseController 组件数组。如果未设置，将在子节点中查找所有 XRBaseController。")]
        private XRBaseController[] _controllers;

        private void Awake()
        {
            // 如果没有在 Inspector 中手动指定，则自动查找组件
            if (_controllerManagers == null || _controllerManagers.Length == 0)
            {
                _controllerManagers = GetComponentsInChildren<ActionBasedControllerManager>(true); // true 表示包含非激活的子对象
                if (_controllerManagers.Length == 0)
                {
                    Debug.LogWarning("PlayerInputBlocker: 未能自动找到任何 ActionBasedControllerManager 组件。", this);
                }
            }

            if (_controllers == null || _controllers.Length == 0)
            {
                _controllers = GetComponentsInChildren<XRBaseController>(true); // true 表示包含非激活的子对象
                if (_controllers.Length == 0)
                {
                     Debug.LogWarning("PlayerInputBlocker: 未能自动找到任何 XRBaseController 组件。", this);
                }
            }
        }

        /// <summary>
        /// 禁用玩家的 XR 控制器输入。
        /// </summary>
        public void BlockInput()
        {
            SetControllerState(false);
            Debug.Log("玩家输入已被 PlayerInputBlocker 阻止。");
        }

        /// <summary>
        /// 启用玩家的 XR 控制器输入。
        /// </summary>
        public void UnblockInput()
        {
            SetControllerState(true);
            Debug.Log("玩家输入已被 PlayerInputBlocker 恢复。");
        }

        /// <summary>
        /// 设置所有相关控制器组件的启用状态。
        /// </summary>
        /// <param name="isEnabled">是否启用</param>
        private void SetControllerState(bool isEnabled)
        {
            bool foundComponents = false;

            if (_controllerManagers != null && _controllerManagers.Length > 0)
            {
                foreach (var manager in _controllerManagers)
                {
                    if (manager != null)
                        manager.enabled = isEnabled;
                }
                foundComponents = true;
            }

            if (_controllers != null && _controllers.Length > 0)
            {
                foreach (var controller in _controllers)
                {
                    if (controller != null)
                    {
                        controller.enabled = isEnabled;
                    }
                }
                foundComponents = true;
            }

            if (!foundComponents)
            {
                Debug.LogWarning("PlayerInputBlocker: 未找到任何可控制的 XR 组件（ActionBasedControllerManager 或 XRBaseController）。", this);
            }
        }
    }
}
