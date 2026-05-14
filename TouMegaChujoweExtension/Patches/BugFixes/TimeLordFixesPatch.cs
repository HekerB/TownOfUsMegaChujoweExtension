using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using System.Linq;
using System;
using TownOfUs.Modules.Anims;
using TownOfUs.Modules;
using TownOfUs.Networking;
using TownOfUs.Options.Roles.Crewmate;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.BugFixes;

[HarmonyPatch]
public static class TimeLordFixesPatch
{
    // Fix: Force visibility when DeathStateSync says a player is alive (revived).
    // This fixes the bug where Time Lord revives a player, but they are only visible to the Time Lord,
    // because other clients did not properly trigger the visual Revive() logic.
    [HarmonyPatch(typeof(TimeLordRewindSystem), nameof(TimeLordRewindSystem.ReviveFromRewind))]
    [HarmonyPostfix]
    public static void ReviveFromRewindPostfix(PlayerControl revived)
    {
        if (revived == null || revived.Data == null) return;

        // Force visibility immediately after the revive
        if (!revived.HasDied())
        {
            revived.Visible = true;
            if (revived.Data.Role is IAnimated animatedRole)
            {
                animatedRole.IsVisible = true;
                animatedRole.SetVisible();
            }

            foreach (var modifier in revived.GetModifiers<BaseModifier>())
            {
                if (modifier is IAnimated animatedMod)
                {
                    animatedMod.IsVisible = true;
                    animatedMod.SetVisible();
                }
            }
        }
    }

    // Fix: Pelican swallow lock. Eject all swallowed players at the END of the rewind,
    // so they are not permanently trapped if they were swallowed during/before the rewind.
    [HarmonyPatch(typeof(TimeLordRewindSystem), nameof(TimeLordRewindSystem.CancelRewindForMeeting))]
    [HarmonyPostfix]
    public static void CancelRewindForMeetingPostfix()
    {
        EjectAllSwallowed();
    }

    [HarmonyPatch(typeof(TimeLordRewindSystem), nameof(TimeLordRewindSystem.StartRewind))]
    [HarmonyPostfix]
    public static void StartRewindPostfix()
    {
        // Also eject when starting just to be safe and prevent position desyncs during rewind
        EjectAllSwallowed();
    }

    // New: Handle normal rewind end (falling edge of IsRewinding)
    private static bool _wasRewinding;

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    public static void HudUpdatePostfix()
    {
        bool isRewinding = TimeLordRewindSystem.IsRewinding;
        if (_wasRewinding && !isRewinding)
        {
            EjectAllSwallowed();
        }
        _wasRewinding = isRewinding;
    }

    private static void EjectAllSwallowed()
    {
        try
        {
            var history = Math.Clamp(OptionGroupSingleton<TimeLordOptions>.Instance.RewindHistorySeconds, 0.25f, 120f);
            var cutoff = DateTime.UtcNow - TimeSpan.FromSeconds(history);

            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null) continue;
                
                var swallowTime = PelicanSystem.GetSwallowTime(player.PlayerId);
                if (swallowTime.HasValue && swallowTime.Value > cutoff)
                {
                    PelicanSystem.ReleaseSinglePlayer(player.PlayerId);
                }
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"[TimeLordFixesPatch] Error releasing swallowed players: {ex.Message}");
        }
    }

    [HarmonyPatch(typeof(TimeLordRewindSystem), nameof(TimeLordRewindSystem.ConfigureHostRevives))]
    [HarmonyPrefix]
    public static void ConfigureHostRevivesPrefix(ref Il2CppSystem.Collections.Generic.List<(byte VictimId, float KillAgeSeconds)>? revives)
    {
        if (revives == null || revives.Count == 0) return;

        try
        {
            var now = DateTime.UtcNow;
            var history = Math.Clamp(OptionGroupSingleton<TimeLordOptions>.Instance.RewindHistorySeconds, 0.25f, 120f);
            var cutoff = now - TimeSpan.FromSeconds(history);

            for (int i = revives.Count - 1; i >= 0; i--)
            {
                var entry = revives[i];
                var deadPlayer = GameHistory.KilledPlayers.ToArray().FirstOrDefault(x => x.VictimId == entry.Item1);
                if (deadPlayer != null && deadPlayer.KillTime < cutoff)
                {
                    revives.RemoveAt(i);
                }
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"[TimeLordFixesPatch] Error filtering revives: {ex.Message}");
        }
    }
}













