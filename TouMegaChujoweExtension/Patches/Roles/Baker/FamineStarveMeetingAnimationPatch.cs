using HarmonyLib;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Events.Neutral;
using TouMegaChujoweExtension.Modifiers.Neutral;

namespace TouMegaChujoweExtension.Patches.Roles.Baker;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
public static class FamineStarveMeetingAnimationPatch
{
    [HarmonyPostfix]
    public static void Postfix(PlayerControl __instance)
    {
        if (!__instance.AmOwner) return;

        var isStarvationDeath = __instance.HasModifier<FamineStarvedModifier>() ||
                                BakerEvents.PendingStarvationDeaths.Contains(__instance.PlayerId);
        if (!isStarvationDeath) return;

        BakerEvents.TryShowStarvationAnimation(__instance);
    }
}
