using System.Reflection;
using TMPro;
using UnityEngine;
using System.Linq;
using HarmonyLib;

namespace TouMegaChujoweExtension.Modules;

public static class VentUtilities
{
    private static FieldInfo? _timerTextField;
    private static FieldInfo? _visualField;
    private static bool _fieldsFound;

    private static readonly string[] TimerTextFieldNames = { "cooldownTimerText", "CooldownTimerText", "cooldownText", "TimerText" };
    private static readonly string[] VisualFieldNames = { "cooldownVisual", "CooldownVisual", "ProgressImage", "Circle" };

    private static void FindFields()
    {
        if (_fieldsFound) return;
        var type = typeof(ActionButton);
        
        foreach (var name in TimerTextFieldNames)
        {
            _timerTextField = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (_timerTextField != null) break;
        }

        foreach (var name in VisualFieldNames)
        {
            _visualField = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (_visualField != null) break;
        }

        _fieldsFound = true;
    }

    /// <summary>
    /// Ensures that a vanilla ActionButton (like ImpostorVentButton) has its internal fields
    /// initialized so that SetCoolDown works for non-impostor/non-engineer roles.
    /// </summary>
    public static void InitializeVentButton(ActionButton? button)
    {
        if (button == null) return;
        
        try 
        {
            FindFields();

            // Check if already initialized or needs initialization
            if (_timerTextField != null && _timerTextField.GetValue(button) == null)
            {
                var text = button.transform.Find("CooldownTimerText")?.GetComponent<TextMeshPro>() 
                           ?? button.GetComponentInChildren<TextMeshPro>(true);
                
                if (text != null)
                {
                    _timerTextField.SetValue(button, text);
                }
            }

            if (_visualField != null && _visualField.GetValue(button) == null)
            {
                var visual = button.transform.Find("CooldownVisual")?.GetComponent<PassiveButton>()
                             ?? button.GetComponentInChildren<PassiveButton>(true);
                
                if (visual != null)
                {
                    _visualField.SetValue(button, visual);
                }
            }
        }
        catch (System.Exception)
        {
            // Silent catch to prevent any potential game crashes during initialization attempt
        }
    }

    /// <summary>
    /// Checks if the button is safe to call SetCoolDown on.
    /// </summary>
    public static bool IsSafeToSetCooldown(ActionButton? button)
    {
        if (button == null) return false;
        FindFields();
        
        // If we can't find the timer text field at all, we can't safely call SetCoolDown 
        // as it will likely throw NRE inside.
        if (_timerTextField == null) return false;

        try
        {
            return _timerTextField.GetValue(button) != null;
        }
        catch
        {
            return false;
        }
    }
}

[HarmonyPatch(typeof(ActionButton), nameof(ActionButton.SetCoolDown))]
public static class ActionButtonSafetyPatch
{
    [HarmonyPrefix]
    public static bool Prefix(ActionButton __instance)
    {
        if (__instance == null) return false;

        // Try to initialize fields if they are missing
        VentUtilities.InitializeVentButton(__instance);

        // If it's still not safe (fields are null), skip the native call to prevent NRE
        if (!VentUtilities.IsSafeToSetCooldown(__instance))
        {
            return false;
        }

        return true;
    }
}
