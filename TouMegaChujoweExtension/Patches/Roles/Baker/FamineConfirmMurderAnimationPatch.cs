using HarmonyLib;
using TouMegaChujoweExtension.Events.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Networking;

namespace TouMegaChujoweExtension.Patches.Roles.Baker;

[HarmonyPatch(typeof(CustomTouMurderRpcs), nameof(CustomTouMurderRpcs.RpcConfirmSpecialMurder))]
public static class FamineConfirmMurderAnimationPatch
{
    [HarmonyPostfix]
    public static void Postfix(PlayerControl target, string causeOfDeath)
    {
        if (causeOfDeath != FamineRole.StarvedDeathReason)
        {
            return;
        }

        BakerEvents.TryShowStarvationAnimation(target);
    }
}
