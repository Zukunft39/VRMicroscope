using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using System.Reflection;

/// <summary>
/// Repairs corrupted Action reference serialization at runtime.
/// Some scene instances may keep an empty inline InputAction while still storing a valid InputActionReference.
/// Rebinding through InputActionProperty(reference) restores the expected behavior without editing scenes manually.
/// </summary>
public static class XRActionReferenceGuard
{
    private static readonly FieldInfo SerializedReferenceField =
        typeof(InputActionProperty).GetField("m_Reference", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo UseReferenceField =
        typeof(InputActionProperty).GetField("m_UseReference", BindingFlags.Instance | BindingFlags.NonPublic);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RepairLoadedSceneOnStartup()
    {
        RepairAllTurnProviders();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RepairAllTurnProviders();
    }

    public static void RepairAllTurnProviders()
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

        if (TryGetReboundProperty(provider.leftHandTurnAction, out InputActionProperty repairedLeftAction))
        {
            provider.leftHandTurnAction = repairedLeftAction;
            changed = true;
        }

        if (TryGetReboundProperty(provider.rightHandTurnAction, out InputActionProperty repairedRightAction))
        {
            provider.rightHandTurnAction = repairedRightAction;
            changed = true;
        }

        if (changed)
        {
            Debug.LogWarning($"[XRActionReferenceGuard] Repaired turn actions on '{provider.name}'.");
        }
    }

    private static bool TryGetReboundProperty(InputActionProperty property, out InputActionProperty repairedProperty)
    {
        repairedProperty = property;

        InputActionReference reference = GetSerializedReference(property);
        if (reference == null)
        {
            return false;
        }

        InputAction action = property.action;
        if (action == null)
        {
            return true;
        }

        bool missingBindings = action.bindings.Count == 0;
        bool missingExpectedControlType = string.IsNullOrEmpty(action.expectedControlType);
        bool hiddenReferenceNotActivated = !IsUsingReference(property);

        if (!missingBindings && !missingExpectedControlType && !hiddenReferenceNotActivated)
        {
            return false;
        }

        repairedProperty = new InputActionProperty(reference);
        return true;
    }

    private static InputActionReference GetSerializedReference(InputActionProperty property)
    {
        return property.reference ?? SerializedReferenceField?.GetValue(property) as InputActionReference;
    }

    private static bool IsUsingReference(InputActionProperty property)
    {
        if (UseReferenceField == null)
        {
            return property.reference != null;
        }

        object useReferenceValue = UseReferenceField.GetValue(property);
        return useReferenceValue is bool boolValue && boolValue;
    }
}
