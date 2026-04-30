using HarmonyLib;
using TownOfUs.Buttons;
using TownOfUs;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Utilities;
using UnityEngine;
using TownOfUs.Options;
using TouMegaChujoweExtension.Roles.Crewmate;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Modifiers.Crewmate;
using System.Linq;

namespace TouMegaChujoweExtension.Patches;

[HarmonyPatch]
public static class CustomKillButtonShieldPatch
{
    [HarmonyPatch(typeof(TownOfUsButton), nameof(TownOfUsButton.ClickHandler))]
    [HarmonyPrefix]
    public static bool TownOfUsClickHandlerPrefix(TownOfUsButton __instance)
    {
        // Try to get target from button instance via reflection if it's not a known target button
        if (__instance is IAftermathablePlayerButton playerButton) {
            return HandleShieldClick(__instance, playerButton.Target);
        }
        
        // Some buttons might have a private or protected target field/property
        var bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        var targetValue = (__instance.GetType().GetProperty("Target", bindingFlags)?.GetValue(__instance) ?? 
                          __instance.GetType().GetField("Target", bindingFlags)?.GetValue(__instance) ??
                          __instance.GetType().GetField("_target", bindingFlags)?.GetValue(__instance)) as PlayerControl;
        
        if (targetValue != null) {
            return HandleShieldClick(__instance, targetValue);
        }

        return true;
    }

    // Patch for target-based buttons (Sheriff, VH, etc.)
    [HarmonyPatch(typeof(TownOfUsTargetButton<PlayerControl>), nameof(TownOfUsTargetButton<PlayerControl>.ClickHandler))]
    [HarmonyPrefix]
    public static bool TargetClickHandlerPrefix(TownOfUsTargetButton<PlayerControl> __instance)
    {
        return HandleShieldClick(__instance, __instance.Target);
    }

    // Patch for basic Mira buttons (just in case)
    [HarmonyPatch(typeof(CustomActionButton<PlayerControl>), nameof(CustomActionButton<PlayerControl>.ClickHandler))]
    [HarmonyPrefix]
    public static bool MiraClickHandlerPrefix(CustomActionButton<PlayerControl> __instance)
    {
        return HandleShieldClick(__instance, __instance.Target);
    }

    private static bool HandleShieldClick(CustomActionButton button, PlayerControl target)
    {
        if (button is not IKillButton) return true;
        if (target == null) return true;

        var attacker = PlayerControl.LocalPlayer;
        if (attacker == null) return true;

        var shieldType = target.GetShieldType();
        if (shieldType == ShieldType.None) return true;

        // Check for Bodyguard specific option "Can Kill Crew Killing"
        if (shieldType == ShieldType.Bodyguard && attacker.Data.Role.GetRoleAlignment() == RoleAlignment.CrewmateKilling)
        {
            var options = OptionGroupSingleton<BodyguardOptions>.Instance;
            if (!options.CanKillCrewKilling)
            {
                // If option is OFF, the shield DOES NOT protect against Crewmate Killing roles.
                return true;
            }
        }

        // Flash effect for the attacker (Sheriff/VH)
        ShieldUtils.TriggerShieldFlash(attacker, shieldType);

        // RPCs for specific shields (Bodyguard, Medic)
        if (shieldType == ShieldType.Bodyguard)
        {
            var bgMod = target.GetModifiers<BodyguardShieldModifier>().FirstOrDefault();
            if (bgMod != null && bgMod.Bodyguard != null)
            {
                BodyguardRole.RpcBodyguardShieldAttacked(bgMod.Bodyguard, attacker, target);
            }
        }
        else if (shieldType == ShieldType.Medic)
        {
            var medicMod = target.GetModifiers<MedicShieldModifier>().FirstOrDefault();
            if (medicMod != null)
            {
                MedicRole.RpcMedicShieldAttacked(medicMod.Medic, attacker, target);
            }
        }

        // Set cooldown and cancel murder
        var saveCd = OptionGroupSingleton<GeneralOptions>.Instance.TempSaveCdReset;
        
        float duration = saveCd;
        switch (shieldType)
        {
            case ShieldType.Medic:
            case ShieldType.Bodyguard:
            case ShieldType.Fairy:
            case ShieldType.Mercenary:
                duration = 10f;
                break;
            case ShieldType.Warden:
                duration = 1f;
                break;
            case ShieldType.Cleric:
                duration = 5f;
                break;
            case ShieldType.Mirrorcaster:
                duration = attacker.GetKillCooldown();
                break;
            default:
                duration = saveCd;
                break;
        }

        button.Timer = duration;
        return false;
    }
}
