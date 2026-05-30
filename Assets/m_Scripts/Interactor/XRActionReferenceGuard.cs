using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using System;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Repairs corrupted Action reference serialization at runtime.
/// Some scene instances may keep an empty inline InputAction while still storing a valid InputActionReference.
/// Rebinding through InputActionProperty(reference) restores the expected behavior without editing scenes manually.
/// </summary>
public static class XRActionReferenceGuard
{
    private static readonly HashSet<int> DisabledInvalidProviderIds = new HashSet<int>();

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
        ActionBasedContinuousTurnProvider[] providers =
            UnityEngine.Object.FindObjectsOfType<ActionBasedContinuousTurnProvider>(true);
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

        if (provider.isActiveAndEnabled && !CanReadTurnProviderActions(provider, out string errorMessage))
        {
            provider.enabled = false;
            int providerId = provider.GetInstanceID();
            if (DisabledInvalidProviderIds.Add(providerId))
            {
                Debug.LogWarning(
                    $"[XRActionReferenceGuard] Disabled invalid continuous turn provider '{provider.name}' to stop repeated Input System errors. Reason: {errorMessage}",
                    provider);
            }
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

        InputAction action;
        try
        {
            action = property.action;
        }
        catch (Exception)
        {
            repairedProperty = new InputActionProperty(reference);
            return true;
        }

        if (action == null)
        {
            repairedProperty = new InputActionProperty(reference);
            return true;
        }

        bool missingBindings = action.bindings.Count == 0;
        bool missingExpectedControlType = string.IsNullOrEmpty(action.expectedControlType);
        bool hiddenReferenceNotActivated = !IsUsingReference(property);
        bool cannotReadValue = !CanReadActionAsVector2(action, out _);

        if (!missingBindings && !missingExpectedControlType && !hiddenReferenceNotActivated && !cannotReadValue)
        {
            return false;
        }

        repairedProperty = new InputActionProperty(reference);
        return true;
    }

    private static bool CanReadTurnProviderActions(ActionBasedContinuousTurnProvider provider, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (provider == null)
        {
            return true;
        }

        if (!CanReadPropertyAsVector2(provider.leftHandTurnAction, out errorMessage))
        {
            errorMessage = $"LeftHandTurnAction: {errorMessage}";
            return false;
        }

        if (!CanReadPropertyAsVector2(provider.rightHandTurnAction, out errorMessage))
        {
            errorMessage = $"RightHandTurnAction: {errorMessage}";
            return false;
        }

        return true;
    }

    private static bool CanReadPropertyAsVector2(InputActionProperty property, out string errorMessage)
    {
        errorMessage = string.Empty;
        InputAction action;
        try
        {
            action = property.action;
        }
        catch (Exception exception)
        {
            errorMessage = $"{exception.GetType().Name}: {exception.Message}";
            return false;
        }

        return CanReadActionAsVector2(action, out errorMessage);
    }

    private static bool CanReadActionAsVector2(InputAction action, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (action == null)
        {
            return true;
        }

        try
        {
            action.ReadValue<Vector2>();
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = $"{exception.GetType().Name}: {exception.Message}";
            return false;
        }
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
