using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace VRMicroscope.Tutorial
{
    public enum TutorialInputAccessMode
    {
        FullyBlocked,
        LocomotionOnly,
        ButtonOnly,
        FullyUnblocked
    }

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

        private readonly Dictionary<XRRayInteractor, bool> _uiInteractionStates = new Dictionary<XRRayInteractor, bool>();
        private readonly Dictionary<XRBaseInteractor, bool> _hoverStates = new Dictionary<XRBaseInteractor, bool>();
        private readonly Dictionary<XRBaseInteractor, bool> _selectStates = new Dictionary<XRBaseInteractor, bool>();

        private Vector3 _lockedPosition;
        private Quaternion _lockedRotation;
        private CharacterController _characterController;
        private bool _isInteractionBlocked = false;
        private bool _isLocomotionBlocked = false;
        private TutorialInputAccessMode _currentAccessMode = TutorialInputAccessMode.FullyUnblocked;

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
            ApplyAccessMode(TutorialInputAccessMode.FullyBlocked);
        }

        public void UnblockInput()
        {
            ApplyAccessMode(TutorialInputAccessMode.FullyUnblocked);
        }

        public void ApplyAccessMode(TutorialInputAccessMode accessMode)
        {
            EnsureComponents();
            ApplyInteractorGameplayBlock(accessMode);

            switch (accessMode)
            {
                case TutorialInputAccessMode.FullyBlocked:
                    SetInteractionBlocked(true);
                    SetLocomotionBlocked(true);
                    break;
                case TutorialInputAccessMode.LocomotionOnly:
                    SetInteractionBlocked(true);
                    SetLocomotionBlocked(false);
                    break;
                case TutorialInputAccessMode.ButtonOnly:
                    ForceEnableInteractions();
                    SetLocomotionBlocked(true);
                    break;
                case TutorialInputAccessMode.FullyUnblocked:
                    ForceEnableInteractions();
                    SetLocomotionBlocked(false);
                    break;
            }

            if (_currentAccessMode != accessMode)
            {
                _currentAccessMode = accessMode;
            }
        }

        private void ApplyInteractorGameplayBlock(TutorialInputAccessMode accessMode)
        {
            if (Interactor.Instance == null)
            {
                return;
            }

            // 强制教程里真正的显微镜/交互输入来自 Interactor 的状态输入图。
            // 如果这里只锁 XR 交互器和位姿，观察模式下后续步骤重新切回 FullyBlocked 时，
            // 显微镜操作输入仍会继续生效。
            bool shouldBlockGameplayInput = accessMode == TutorialInputAccessMode.FullyBlocked;
            Interactor.Instance.SetForceTutorialGameplayInputBlocked(shouldBlockGameplayInput);
        }

        private void SetInteractionBlocked(bool shouldBlock)
        {
            if (shouldBlock == _isInteractionBlocked)
            {
                return;
            }

            _isInteractionBlocked = shouldBlock;

            if (shouldBlock)
            {
                _uiInteractionStates.Clear();
                _hoverStates.Clear();
                _selectStates.Clear();

                foreach (XRBaseInteractor interactor in _interactors)
                {
                    if (interactor == null) continue;

                    _hoverStates[interactor] = interactor.allowHover;
                    _selectStates[interactor] = interactor.allowSelect;
                    interactor.allowHover = false;
                    interactor.allowSelect = false;

                    XRRayInteractor ray = interactor as XRRayInteractor;
                    if (ray != null)
                    {
                        _uiInteractionStates[ray] = ray.enableUIInteraction;
                        ray.enableUIInteraction = false;
                    }
                }

                return;
            }

            foreach (XRBaseInteractor interactor in _interactors)
            {
                if (interactor == null) continue;

                XRRayInteractor ray = interactor as XRRayInteractor;
                bool hoverState;
                if (_hoverStates.TryGetValue(interactor, out hoverState))
                {
                    interactor.allowHover = hoverState;
                }
                else
                {
                    interactor.allowHover = true;
                }

                bool selectState;
                if (_selectStates.TryGetValue(interactor, out selectState))
                {
                    interactor.allowSelect = selectState;
                }
                else
                {
                    interactor.allowSelect = true;
                }

                bool uiState;
                if (ray != null && _uiInteractionStates.TryGetValue(ray, out uiState))
                {
                    ray.enableUIInteraction = uiState;
                }
            }

        }

        private void ForceEnableInteractions()
        {
            if (!_isInteractionBlocked)
            {
                foreach (XRBaseInteractor interactor in _interactors)
                {
                    if (interactor == null) continue;
                    interactor.allowHover = true;
                    interactor.allowSelect = true;

                    XRRayInteractor ray = interactor as XRRayInteractor;
                    if (ray != null)
                    {
                        ray.enableUIInteraction = true;
                    }
                }

                return;
            }

            _isInteractionBlocked = false;

            foreach (XRBaseInteractor interactor in _interactors)
            {
                if (interactor == null) continue;
                interactor.allowHover = true;
                interactor.allowSelect = true;

                XRRayInteractor ray = interactor as XRRayInteractor;
                if (ray != null)
                {
                    ray.enableUIInteraction = true;
                }
            }

        }

        private void SetLocomotionBlocked(bool shouldBlock)
        {
            if (shouldBlock == _isLocomotionBlocked)
            {
                return;
            }

            _isLocomotionBlocked = shouldBlock;

            if (shouldBlock)
            {
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

                return;
            }

            if (_characterController != null)
            {
                _characterController.enabled = true;
            }

        }

        private void LateUpdate()
        {
            if (_isLocomotionBlocked && _playerRoot != null)
            {
                _playerRoot.position = _lockedPosition;
                _playerRoot.rotation = _lockedRotation;
            }
        }

    }
}
