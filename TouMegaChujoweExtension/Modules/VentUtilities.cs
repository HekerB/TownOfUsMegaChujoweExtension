using TMPro;
using UnityEngine;
using AmongUs.GameOptions;

namespace TouMegaChujoweExtension.Modules;

public static class VentUtilities
{
    // These used to be looked up with AccessTools.Field over a list of candidate names. That never
    // matched anything: Il2CppInterop surfaces IL2CPP fields as PROPERTIES on the interop type, so
    // every candidate missed, IsSafeToSetCooldown answered "no" for every non-impostor, and the vent
    // button of a role that had venting granted to it never got its cooldown set at all. The members
    // are read directly below, the way MiraAPI and TOU already read them.

    /// <summary>
    /// Ensures that a vanilla ActionButton (like ImpostorVentButton) has its internal fields
    /// initialized so that SetCoolDown works for non-impostor/non-engineer roles.
    /// </summary>
    public static void InitializeVentButton(ActionButton? button)
    {
        if (button == null) return;

        try
        {
            if (button.cooldownTimerText == null)
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
                    button.cooldownTimerText = text;
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
    /// <remarks>
    /// Vanilla's SetCoolDown reads the timer text and the graphic straight off the button, and on one
    /// that is not finished being built that throws - once per frame for as long as a cooldown runs.
    /// Being an impostor is not the exemption it was treated as: their button is not always ready
    /// either.
    /// </remarks>
    public static bool IsSafeToSetCooldown(ActionButton? button)
    {
        if (button == null) return false;

        try
        {
            return button.cooldownTimerText != null && button.graphic != null;
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
