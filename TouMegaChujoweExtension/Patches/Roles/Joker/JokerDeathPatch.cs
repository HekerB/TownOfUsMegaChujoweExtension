using HarmonyLib;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Roles.Joker;

[HarmonyPatch]
public static class JokerDeathPatch
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
    [HarmonyPostfix]
    public static void PostfixDie(PlayerControl __instance)
    {
        if (__instance == null || __instance.Data == null)
        {
            return;
        }

        if (__instance.IsRole<JokerRole>())
        {
            JokerCloneSystem.RemoveClonesForJoker(__instance.PlayerId);
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
    [HarmonyPostfix]
    public static void PostfixLeave([HarmonyArgument(0)] InnerNet.ClientData client)
    {
        if (client?.Character != null && client.Character.IsRole<JokerRole>())
        {
            JokerCloneSystem.RemoveClonesForJoker(client.Character.PlayerId);
        }
    }
}
