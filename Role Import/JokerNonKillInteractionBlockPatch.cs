/*using AmongUs.GameOptions;
using HarmonyLib;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Joker;

[HarmonyPatch(typeof(TownOfUsButton), nameof(TownOfUsButton.ClickHandler))]
public static class JokerNonKillInteractionBlockPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool Prefix(TownOfUsButton __instance)
    {
        if (__instance is Buttons.Neutral.JokerPlaceCloneButton) return true;

        var local = PlayerControl.LocalPlayer;
        if (local == null || local.HasDied() || MeetingHud.Instance) return true;

        var localPos = local.GetTruePosition();
        var dist = GetKillDistance();

        if (!JokerCloneSystem.TryGetClosestClone(localPos, dist, out var cloneIndex, out _))
            return true;

        if (JokerCloneInteractionPatches.IsKillingRole(local))
        {
            var clone = JokerCloneSystem.Clones[cloneIndex];
            JokerRole.RpcJokerCloneKilled(local, clone.JokerId, cloneIndex);
        }

        try { __instance.SetTimer(__instance.Cooldown); } catch { }

        return false;
    }

    private static float GetKillDistance()
    {
        var opts = GameOptionsManager.Instance?.currentNormalGameOptions;
        if (opts == null) return 1.0f;

        var killDistances = opts.GetFloatArray(FloatArrayOptionNames.KillDistances);
        var idx = Math.Clamp(opts.KillDistance, 0, killDistances.Length - 1);
        return killDistances[idx];
    }
}*/