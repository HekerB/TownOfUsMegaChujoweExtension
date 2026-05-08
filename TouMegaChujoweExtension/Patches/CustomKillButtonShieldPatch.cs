using HarmonyLib;
using System.Reflection;
using TownOfUs.Buttons;
using TownOfUs;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Utilities;
using TouMegaChujoweExtension.Roles.Crewmate;
using TouMegaChujoweExtension.Modifiers;
using TownOfUs.Options;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Modifiers.Crewmate;
using System.Linq;
using UnityEngine;
using Reactor.Utilities;

namespace TouMegaChujoweExtension.Patches;

[HarmonyPatch]
public static class CustomKillButtonShieldPatch
{
    [HarmonyPatch(typeof(TownOfUsButton), nameof(TownOfUsButton.ClickHandler))]
    [HarmonyPrefix]
    public static bool TownOfUsClickHandlerPrefix(TownOfUsButton __instance)
    {
        if (__instance == null) return true;
        var target = GetTarget(__instance);
        if (!ShieldUtils.IsHarmfulInteraction(__instance, target)) return true;
        
        return !ShieldUtils.HandleButtonShieldClick(__instance, target);
    }

    [HarmonyPatch(typeof(TownOfUsTargetButton<PlayerControl>), nameof(TownOfUsTargetButton<PlayerControl>.ClickHandler))]
    [HarmonyPrefix]
    public static bool TargetClickHandlerPrefix(TownOfUsTargetButton<PlayerControl> __instance)
    {
        if (__instance == null) return true;
        if (!ShieldUtils.IsHarmfulInteraction(__instance, __instance.Target)) return true;
        
        return !ShieldUtils.HandleButtonShieldClick(__instance, __instance.Target);
    }

    [HarmonyPatch(typeof(ActionButton), nameof(ActionButton.DoClick))]
    [HarmonyPrefix]
    public static bool ActionButtonClickHandlerPrefix(ActionButton __instance)
    {
        if (__instance == null) return true;
        
        // Use reflection to get Target, as ActionButton doesn't have it but subclasses do
        var targetProp = __instance.GetType().GetProperty("Target", BindingFlags.Instance | BindingFlags.Public);
        var target = targetProp?.GetValue(__instance) as PlayerControl;

        if (target == null) return true;
        if (!ShieldUtils.IsHarmfulInteraction(__instance, target)) return true;

        return !ShieldUtils.HandleButtonShieldClick(__instance, target);
    }

    private static PlayerControl? GetTarget(TownOfUsButton button)
    {
        if (button == null) return null;
        if (button is IAftermathablePlayerButton playerButton) return playerButton.Target;
        
        try 
        {
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var targetObj = button.GetType().GetProperty("Target", bindingFlags)?.GetValue(button) ?? 
                           button.GetType().GetField("Target", bindingFlags)?.GetValue(button) ??
                           button.GetType().GetField("_target", bindingFlags)?.GetValue(button);
            
            return targetObj as PlayerControl;
        }
        catch { return null; }
    }

    [HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
    [HarmonyPrefix]
    public static bool VanillaKillButtonClickHandlerPrefix(KillButton __instance)
    {
        if (__instance == null || __instance.gameObject == null || !__instance.gameObject.activeSelf) return true;
        
        var target = __instance.currentTarget;
        if (target == null) return true;

        if (ShieldUtils.HandleButtonShieldClick(null, target))
        {
            PlayerControl.LocalPlayer.SetKillTimer(10f); // Fallback for vanilla button
            return false;
        }
        return true;
    }
}
