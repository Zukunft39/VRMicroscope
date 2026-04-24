using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Repairs corrupted Action reference serialization at runtime.
/// Some scene instances may keep an empty inline InputAction while still storing a valid InputActionReference.
/// Rebinding through InputActionProperty(reference) restores the expected behavior without editing scenes manually.
/// </summary>
public static class XRActionReferenceGuard
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ActionBasedContinuousTurnProvider[] providers = Object.FindObjectsOfType<ActionBasedContinuousTurnProvider>(true);
        for (int i = 0; i < providers.Length; i++)
        {
            RepairTurnProvider(providers[i]);
        }
    }

    private static void RepairTurnProvider(ActionBasedContinuousTurnProvider provider)
    {
        if (provider == null)
        {
            return;
        }

        bool changed = false;

        if (NeedsReferenceRebind(provider.leftHandTurnAction))
        {
            provider.leftHandTurnAction = new InputActionProperty(provider.leftHandTurnAction.reference);
            changed = true;
        }

        if (NeedsReferenceRebind(provider.rightHandTurnAction))
        {
            provider.rightHandTurnAction = new InputActionProperty(provider.rightHandTurnAction.reference);
            changed = true;
        }

        if (changed)
        {
            Debug.LogWarning($"[XRActionReferenceGuard] Repaired turn actions on '{provider.name}'.");
        }
    }

    private static bool NeedsReferenceRebind(InputActionProperty property)
    {
        InputActionReference reference = property.reference;
        if (reference == null)
        {
            return false;
        }

        InputAction action = property.action;
        if (action == null)
        {
            return true;
        }

        // Corrupted inline actions usually have no bindings and empty expected control type.
        return action.bindings.Count == 0 || string.IsNullOrEmpty(action.expectedControlType);
    }
}
