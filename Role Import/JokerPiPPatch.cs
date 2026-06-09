using HarmonyLib;
using TouMegaChujoweExtension.Roles.Neutral;

namespace TouMegaChujoweExtension.Patches.Joker;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class JokerPiPPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        var local = PlayerControl.LocalPlayer;
        if (local?.Data?.Role is JokerRole jokerRole && local.AmOwner)
            jokerRole.TickPiP();
    }
}