using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Options;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Bodyguard;

[HarmonyPatch(typeof(PlayerRoleTextExtensions), nameof(PlayerRoleTextExtensions.UpdateProtectionSymbols), typeof(string), typeof(PlayerControl), typeof(bool))]
public static class BodyguardNamePatch
{
    [HarmonyPostfix]
    public static void Postfix(ref string __result, PlayerControl player, bool hidden)
    {
        var local = PlayerControl.LocalPlayer;
        if (player == null || local == null || local.Data == null)
            return;

        var genOpt = OptionGroupSingleton<GeneralOptions>.Instance;
        bool isGhost = local.HasDied();
        bool deadKnow = isGhost && genOpt.TheDeadKnow;

        // === Shield symbol Σ ===
        if (player.TryGetModifier<BodyguardShieldModifier>(out var shieldMod))
        {
            if (shieldMod.VisibleSymbol || deadKnow)
            {
                if (!__result.Contains("Σ"))
                {
                    __result += "<color=#77B962> Σ</color>";
                }
            }
        }

        // === Green name on who attacked (After backlash) ===
        if (!hidden
            && local.Data.Role is BodyguardRole bgRole
            && bgRole.LastAttacker != null
            && bgRole.LastAttacker.PlayerId == player.PlayerId
            && (bgRole.BacklashReady || bgRole.KillModeActive) 
            && OptionGroupSingleton<BodyguardOptions>.Instance.GreenNameOnAttacker)
        {
            if (!__result.StartsWith("<color=#77B962>"))
            {
                __result = $"<color=#77B962>{__result}</color>";
            }
        }
    }
}