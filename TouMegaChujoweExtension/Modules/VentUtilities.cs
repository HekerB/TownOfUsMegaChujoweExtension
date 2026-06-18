using System.Reflection;
using TMPro;
using UnityEngine;
using System.Linq;
using HarmonyLib;
using AmongUs.GameOptions;

namespace TouMegaChujoweExtension.Modules;

public static class VentUtilities
{
    private static FieldInfo? _timerTextField;
    private static FieldInfo? _visualField;
    private static bool _fieldsFound;

    private static readonly string[] TimerTextFieldNames = ["cooldownTimerText", "CooldownTimerText", "cooldownText", "TimerText"];
    private static readonly string[] VisualFieldNames = ["cooldownVisual", "CooldownVisual", "ProgressImage", "Circle"];

    private static void FindFields()
    {
        if (_fieldsFound) return;

        foreach (var name in TimerTextFieldNames)
        {
            _timerTextField = AccessTools.Field(typeof(ActionButton), name);
            if (_timerTextField != null) break;
        }

        foreach (var name in VisualFieldNames)
        {
            _visualField = AccessTools.Field(typeof(ActionButton), name);
            if (_visualField != null) break;
        }

        if (_timerTextField == null) UnityEngine.Debug.LogWarning("[TOUMCE] VentUtilities: Could not find cooldownTimerText field on ActionButton!");
        if (_visualField == null) UnityEngine.Debug.LogWarning("[TOUMCE] VentUtilities: Could not find cooldownVisual field on ActionButton!");

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
                // Try multiple ways to find the text component
                var text = button.transform.Find("CooldownTimerText")?.GetComponent<TextMeshPro>()
                           ?? button.GetComponentInChildren<TextMeshPro>(true);

                // Special case for Town of Us VentButton which often has buttonLabelText field
                if (text == null && button is VentButton vb)
                {
                    text = vb.buttonLabelText;
                }

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
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"[TOUMCE] VentUtilities Error: {ex}");
        }
    }

    /// <summary>
    /// Checks if the button is safe to call SetCoolDown on.
    /// </summary>
    public static bool IsSafeToSetCooldown(ActionButton? button)
    {
        if (button == null) return false;

        // If we are an Impostor or Engineer, the base game should have handled this.
        // We only really need this safety for custom roles we manually enabled venting for.
        var local = PlayerControl.LocalPlayer;
        if (local != null && local.Data != null && (local.Data.Role.IsImpostor || local.Data.Role.Role == RoleTypes.Engineer))
        {
            return true;
        }

        FindFields();

        if (_timerTextField == null || _visualField == null) return false;

        try
        {
            return _timerTextField.GetValue(button) != null && _visualField.GetValue(button) != null;
        }
        catch
        {
            return false;
        }
    }

    public static bool TrySetCooldown(ActionButton? button, float timer, float maxTimer)
    {
        if (!IsSafeToSetCooldown(button)) return false;

        try
        {
            button!.SetCoolDown(timer, maxTimer);
            return true;
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[TOUMCE] VentUtilities: skipped unsafe SetCoolDown: {ex.Message}");
            return false;
        }
    }
}
