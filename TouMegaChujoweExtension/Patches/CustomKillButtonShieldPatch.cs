using HarmonyLib;
using TownOfUs.Buttons;
using TownOfUs;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Utilities;
using UnityEngine;
using TownOfUs.Options;
using TouMegaChujoweExtension.Roles.Crewmate;
using TouMegaChujoweExtension.Modifiers;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Modifiers.Crewmate;

namespace TouMegaChujoweExtension.Patches;

[HarmonyPatch(typeof(TownOfUsTargetButton<PlayerControl>), nameof(TownOfUsTargetButton<PlayerControl>.ClickHandler))]
public static class CustomKillButtonShieldPatch
{
    [HarmonyPostfix]
    public static void Postfix(TownOfUsTargetButton<PlayerControl> __instance)
    {
        if (__instance is not IKillButton) return;

        var target = __instance.Target;
        if (target == null) return;

        if (!PlayerControl.LocalPlayer.Is(RoleAlignment.CrewmateKilling)) return;

        var shieldType = target.GetShieldType();
        if (shieldType == ShieldType.None) return;

        // 1. Trigger the shield flash for the local player (attacker)
        ShieldUtils.TriggerShieldFlash(PlayerControl.LocalPlayer, shieldType);

        // 1.5. Trigger specific shield reactions (RPCs)
        if (shieldType == ShieldType.Bodyguard)
        {
            if (target.TryGetModifier<BodyguardShieldModifier>(out var bgMod) && bgMod.Bodyguard != null)
            {
                BodyguardRole.RpcBodyguardShieldAttacked(bgMod.Bodyguard, PlayerControl.LocalPlayer, target);
            }
        }
        else if (shieldType == ShieldType.Medic)
        {
            if (target.TryGetModifier<MedicShieldModifier>(out var medicMod))
            {
                MedicRole.RpcMedicShieldAttacked(medicMod.Medic, PlayerControl.LocalPlayer, target);
            }
        }

        // 2. Enforce the shield cooldown duration
        var saveCd = OptionGroupSingleton<GeneralOptions>.Instance.TempSaveCdReset;
        float duration = saveCd;
        switch (shieldType)
        {
            case ShieldType.Mirrorcaster:
                duration = PlayerControl.LocalPlayer.GetKillCooldown();
                break;
            case ShieldType.Bodyguard:
                duration = 10f;
                break;
            case ShieldType.Medic:
                duration = 10f;
                break;
            case ShieldType.Warden:
                duration = 1f;
                break;
            case ShieldType.Cleric:
                duration = 5f;
                break;
            case ShieldType.Fairy:
                duration = 10f;
                break;
            case ShieldType.Mercenary:
                duration = 10f;
                break;
            default:
                duration = saveCd;
                break;
        }
        
        __instance.Timer = duration;
    }
}
