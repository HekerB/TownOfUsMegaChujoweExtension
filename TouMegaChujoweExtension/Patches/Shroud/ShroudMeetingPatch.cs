using System.Linq;
using HarmonyLib;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Networking;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Neutral;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
public static class ShroudMeetingPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied()) continue;
            if (!player.TryGetModifier<ShroudedModifier>(out var mod)) continue;
            if (mod.WasInteractedWith) continue;

            var shroudOwner = PlayerControl.AllPlayerControls.ToArray()
                .FirstOrDefault(x => x.PlayerId == mod.ShroudOwnerId && !x.HasDied());

            if (shroudOwner == null) continue;

            shroudOwner.RpcSpecialMurder(
                player,
                isIndirect: true,
                ignoreShield: false,
                resetKillTimer: false,
                createDeadBody: false,
                teleportMurderer: false,
                showKillAnim: false,
                playKillSound: false,
                causeOfDeath: "Shroud");
        }
    }
}

/// <summary>
/// Shows kill overlay animation for the shrouded player when they die during meeting.
/// This runs on ALL clients after RpcSpecialMurder processes the death.
/// </summary>
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
public static class ShroudDeathAnimationPatch
{
    [HarmonyPostfix]
    public static void Postfix(PlayerControl __instance)
    {
        if (!MeetingHud.Instance) return;
        if (!__instance.AmOwner) return;
        if (!__instance.TryGetModifier<ShroudedModifier>(out var mod)) return;
        if (mod.WasInteractedWith) return;

        // Show kill overlay animation to the dead player (like old Assassin kill)
        if (HudManager.InstanceExists)
        {
            SoundManager.Instance.PlaySound(__instance.KillSfx, false, 0.8f);
            HudManager.Instance.KillOverlay.ShowKillAnimation(__instance.Data, __instance.Data);
        }
    }
}