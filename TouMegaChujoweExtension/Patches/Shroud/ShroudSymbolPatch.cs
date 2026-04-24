using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Options;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Neutral;

[HarmonyPatch(typeof(PlayerRoleTextExtensions), nameof(PlayerRoleTextExtensions.UpdateTargetSymbols), typeof(string), typeof(PlayerControl), typeof(bool))]
public static class ShroudSymbolPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref string __result, PlayerControl player, bool hidden = false)
    {
        if (player == null) return;

        var local = PlayerControl.LocalPlayer;
        if (local == null) return;

        var genOpt = OptionGroupSingleton<GeneralOptions>.Instance;
        var isDead = local.HasDied() && genOpt.TheDeadKnow && !hidden;

        if (!player.TryGetModifier<ShroudedModifier>(out var mod)) return;

        if ((local.Data?.Role is ShroudRole && mod.ShroudOwnerId == local.PlayerId) || isDead)
        {
            __result += $" {TouExtensionColors.Shroud.ToTextColor()}♢</color>";
        }
    }
}