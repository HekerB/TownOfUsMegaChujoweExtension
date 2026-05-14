using HarmonyLib;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Options;
using TownOfUs.Buttons;
using TownOfUs.Events;
using TownOfUs.Roles;

namespace TouMegaChujoweExtension.Patches.General;

[HarmonyPatch]
public static class FirstRoundKillBlockPatch
{
    private static bool IsKillBlocked()
    {
        var options = OptionGroupSingleton<ExtensionGeneralOptions>.Instance;
        if (!options.DisableKillsFirstRound) return false;
        
        return DeathEventHandlers.CurrentRound <= 1;
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
    [HarmonyPrefix]
    public static bool PrefixCheckMurder()
    {
        if (IsKillBlocked()) return false;
        return true;
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    [HarmonyPrefix]
    public static bool PrefixMurderPlayer()
    {
        if (IsKillBlocked()) return false;
        return true;
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
    [HarmonyPrefix]
    public static bool PrefixDie()
    {
        // Only block if it's not a meeting/exile and it's the first round
        if (MeetingHud.Instance != null || ExileController.Instance != null) return true;
        
        if (IsKillBlocked()) return false;
        return true;
    }

    [HarmonyPatch(typeof(TownOfUsButton), nameof(TownOfUsButton.Enabled))]
    [HarmonyPostfix]
    public static void PostfixButtonEnabled(TownOfUsButton __instance, ref bool __result)
    {
        if (!__result) return;
        
        // If it's a kill button and kills are blocked, disable it
        if (__instance is IKillButton && IsKillBlocked())
        {
            __result = false;
        }
    }
}
