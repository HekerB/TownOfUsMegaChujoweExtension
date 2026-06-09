using HarmonyLib;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Roles.Neutral;

namespace TouMegaChujoweExtension.Patches.Joker;

[HarmonyPatch]
public static class JokerDeathCleanupPatches
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
    [HarmonyPostfix]
    public static void DiePostfix(PlayerControl __instance)
    {
        if (__instance?.Data?.Role is JokerRole)
            JokerCloneSystem.RemoveClonesForJoker(__instance.PlayerId);
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Exiled))]
    [HarmonyPostfix]
    public static void ExiledPostfix(PlayerControl __instance)
    {
        if (__instance?.Data?.Role is JokerRole)
            JokerCloneSystem.RemoveClonesForJoker(__instance.PlayerId);
    }

    [HarmonyPatch(typeof(GameData), nameof(GameData.HandleDisconnect), typeof(PlayerControl), typeof(DisconnectReasons))]
    [HarmonyPostfix]
    public static void DisconnectPostfix(PlayerControl player)
    {
        if (player?.Data?.Role is JokerRole)
            JokerCloneSystem.RemoveClonesForJoker(player.PlayerId);
    }
}