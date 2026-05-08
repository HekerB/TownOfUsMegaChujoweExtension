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
        return !ShieldUtils.HandleButtonShieldClick(__instance, GetTarget(__instance));
    }

    [HarmonyPatch(typeof(TownOfUsTargetButton<PlayerControl>), nameof(TownOfUsTargetButton<PlayerControl>.ClickHandler))]
    [HarmonyPrefix]
    public static bool TargetClickHandlerPrefix(TownOfUsTargetButton<PlayerControl> __instance)
    {
        if (__instance == null) return true;
        if (__instance.Target == null) return true;
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

        return !ShieldUtils.HandleButtonShieldClick(__instance, target);
    }

    private static PlayerControl? GetTarget(TownOfUsButton button)
    {
        if (button is IAftermathablePlayerButton playerButton) return playerButton.Target;
        
#pragma warning disable S3011
        var bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        return (button.GetType().GetProperty("Target", bindingFlags)?.GetValue(button) ?? 
                button.GetType().GetField("Target", bindingFlags)?.GetValue(button) ??
                button.GetType().GetField("_target", bindingFlags)?.GetValue(button)) as PlayerControl;
#pragma warning restore S3011
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
            return false;
        }
        return true;
    }
}
