using AmongUs.GameOptions;
using HarmonyLib;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Patches.Joker;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Joker;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class JokerCloneHighlightPatches
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(HudManager __instance)
    {
        if (__instance == null || MeetingHud.Instance || PlayerControl.LocalPlayer == null) return;

        var local = PlayerControl.LocalPlayer;

        if (JokerCloneInteractionPatches.IsKillingRole(local))
        {
            var dist = GetKillDistance();
            if (JokerCloneSystem.TryGetClosestClone(local.GetTruePosition(), dist, out _, out _))
            {
                if (__instance.KillButton != null && __instance.KillButton.isActiveAndEnabled)
                    __instance.KillButton.SetEnabled();

                var color = JokerCloneInteractionPatches.GetKillColor(local);
                JokerCloneSystem.UpdateLocalOutline(local.GetTruePosition(), dist, color);
                return;
            }
        }

        JokerCloneSystem.ClearLocalOutline();
    }

    private static float GetKillDistance()
    {
        var opts = GameOptionsManager.Instance?.currentNormalGameOptions;
        if (opts == null) return 1.0f;

        var killDistances = opts.GetFloatArray(FloatArrayOptionNames.KillDistances);
        var idx = Math.Clamp(opts.KillDistance, 0, killDistances.Length - 1);
        return killDistances[idx];
    }
}