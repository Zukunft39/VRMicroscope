using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace VRMicroscope.Tutorial
{
    /// <summary>
    /// 该组件负责阻止和恢复玩家的输入。
    /// 建议将其放置在 XR Origin 或其父对象上。
    /// 它通过禁用交互器能力并锚定玩家根节点来工作。
    /// </summary>
    public class PlayerInputBlocker : MonoBehaviour
    {
        [Header("XR 交互组件")]
        [SerializeField]
        [Tooltip("【可选】指定 XRBaseInteractor（包含所有射线和直接抓取）。如果未设置，将自动查找全局。")]
        private XRBaseInteractor[] _interactors;

        [Header("物理位置锚定")]
        [SerializeField]
        [Tooltip("【可选】玩家的根节点(XR Origin)。如果未设置，将自动通过 LocomotionSystem 寻找。")]
        private Transform _playerRoot;

        [Header("Debug")]
        [SerializeField]
        [Tooltip("是否输出输入锁定与恢复日志")]
        private bool _enableDebugLogs = true;

        private readonly Dictionary<XRRayInteractor, bool> _uiInteractionStates = new Dictionary<XRRayInteractor, bool>();

        private Vector3 _lockedPosition;
        private Quaternion _lockedRotation;
        private CharacterController _characterController;
        private bool _isInputBlocked = false;

        private void EnsureComponents()
        {
            if (_interactors == null || _interactors.Length == 0)
            {
                _interactors = FindObjectsOfType<XRBaseInteractor>(true);
            }

            if (_playerRoot == null)
            {
                LocomotionSystem locomotionSystem = FindObjectOfType<LocomotionSystem>();
                if (locomotionSystem != null)
                {
                    _playerRoot = locomotionSystem.transform;
                    CharacterController characterController = _playerRoot.GetComponentInParent<CharacterController>();
                    if (characterController != null)
                    {
                        _playerRoot = characterController.transform;
                    }
                }
            }
        }

        public void BlockInput()
        {
            if (_isInputBlocked) return;
            _isInputBlocked = true;

            EnsureComponents();
            _uiInteractionStates.Clear();
            
            if (_playerRoot != null)
            {
                _lockedPosition = _playerRoot.position;
                _lockedRotation = _playerRoot.rotation;
                
                _characterController = _playerRoot.GetComponent<CharacterController>();
                if (_characterController != null)
                {
                    _characterController.enabled = false;
                }
            }
            else
            {
                Debug.LogWarning("[PlayerInputBlocker] 未能找到玩家根节点(Player Root)，物理锚定可能失败！");
            }

            foreach (XRBaseInteractor interactor in _interactors)
            {
                if (interactor == null) continue;
                interactor.allowHover = false;
                interactor.allowSelect = false;

                XRRayInteractor ray = interactor as XRRayInteractor;
                if (ray != null)
                {
                    _uiInteractionStates[ray] = ray.enableUIInteraction;
                    ray.enableUIInteraction = false;
                }
            }

            LogDebug("玩家输入已锁定。");
        }

        public void UnblockInput()
        {
            if (!_isInputBlocked) return;
            _isInputBlocked = false;

            if (_characterController != null)
            {
                _characterController.enabled = true;
            }

            foreach (XRBaseInteractor interactor in _interactors)
            {
                if (interactor == null) continue;
                interactor.allowHover = true;
                interactor.allowSelect = true;

                XRRayInteractor ray = interactor as XRRayInteractor;
                bool uiState;
                if (ray != null && _uiInteractionStates.TryGetValue(ray, out uiState))
                {
                    ray.enableUIInteraction = uiState;
                }
            }

            LogDebug("玩家输入已恢复。");
        }

        private void LateUpdate()
        {
            if (_isInputBlocked && _playerRoot != null)
            {
                _playerRoot.position = _lockedPosition;
                _playerRoot.rotation = _lockedRotation;
            }
        }

        private void LogDebug(string message)
        {
            if (!_enableDebugLogs) return;
            Debug.Log($"[ForceTutorial/InputBlocker] {message}");
        }
    }
}
