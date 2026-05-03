using HarmonyLib;
using TownOfUs.Roles.Crewmate;

namespace TouMegaChujoweExtension.Patches.President;

/// <summary>
/// This patch used to block President from being knighted.
/// Now it is empty (or we can remove it) to allow President + Monarch interaction.
/// </summary>
[HarmonyPatch(typeof(MonarchRole), nameof(MonarchRole.RpcKnight))]
public static class PresidentMonarchInteractionPatch
{
    [HarmonyPrefix]
    public static bool Prefix(PlayerControl player, PlayerControl target)
    {
        // Block removed - President can now be knighted!
        return true;
    }
}
