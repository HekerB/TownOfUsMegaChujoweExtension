using HarmonyLib;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Joker;

[HarmonyPatch]
public static class JokerCloneKillPatch
{
    private static bool TryKillClone(PlayerControl local, System.Action setCooldown)
    {
        if (local == null || local.HasDied() || MeetingHud.Instance) return false;
        if (!JokerCloneInteractionPatches.IsKillingRole(local)) return false;

        var dist = JokerCloneInteractionPatches.GetKillDistanceStatic();
        if (!JokerCloneSystem.TryGetClosestClone(local.GetTruePosition(), dist, out var idx, out _))
            return false;

        var clone = JokerCloneSystem.Clones[idx];
		if (clone.IsPreview) return false;

		JokerRole.RpcJokerCloneKilled(
		local,
		clone.JokerId,
		clone.WorldPosition.x,
		clone.WorldPosition.y
		);
        setCooldown();
        return true;
    }

    [HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool KillButtonPrefix()
    {
        var local = PlayerControl.LocalPlayer;
        return !TryKillClone(local,
            () => { try { local!.SetKillTimer(local.GetKillCooldown()); } catch { } });
    }

    [HarmonyPatch(typeof(TownOfUsButton), nameof(TownOfUsButton.ClickHandler))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool TownOfUsButtonPrefix(TownOfUsButton __instance)
    {
        if (__instance is Buttons.Neutral.JokerPlaceCloneButton) return true;
        if (__instance is not IKillButton) return true;

        var local = PlayerControl.LocalPlayer;
        return !TryKillClone(local,
            () => { try { __instance.SetTimer(__instance.Cooldown); } catch { } });
    }

    [HarmonyPatch(typeof(TownOfUsTargetButton<PlayerControl>), nameof(TownOfUsTargetButton<PlayerControl>.ClickHandler))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool TownOfUsTargetButtonPrefix(TownOfUsTargetButton<PlayerControl> __instance)
    {
        if (__instance is not IKillButton) return true;

        var local = PlayerControl.LocalPlayer;
        return !TryKillClone(local,
            () => { try { __instance.SetTimer(__instance.Cooldown); } catch { } });
    }
}