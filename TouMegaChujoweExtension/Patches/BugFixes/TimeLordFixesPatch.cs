using HarmonyLib;
using MiraAPI.Modifiers;
using TownOfUs.Modules;
using TownOfUs.Networking;
using TouMegaChujoweExtension.Modules;
using UnityEngine;
using TownOfUs.Options.Roles.Crewmate;
using MiraAPI.GameOptions;
using System.Linq;
using System;
using TownOfUs.Modules.Anims;
using TownOfUs.Utilities;

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
        try
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null) continue;
                
                if (PelicanSystem.IsSwallowed(player.PlayerId))
                {
                    var pelicanId = PelicanSystem.GetPelicanOf(player.PlayerId);
                    if (pelicanId.HasValue)
                    {
                        PelicanSystem.ReleaseAll(pelicanId.Value);
                        
                        // Force visibility just in case ReleaseAll didn't apply it immediately
                        if (!player.HasDied())
                        {
                            player.Visible = true;
                            player.moveable = true;
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"[TimeLordFixesPatch] Error releasing swallowed players on rewind end: {ex.Message}");
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
                var deadPlayer = GameHistory.KilledPlayers.FirstOrDefault(x => x.VictimId == entry.Item1);
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
