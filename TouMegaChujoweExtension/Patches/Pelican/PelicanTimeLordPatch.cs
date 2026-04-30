using HarmonyLib;
using TownOfUs.Modules;
using TouMegaChujoweExtension.Modules;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;
using TownOfUs.Options.Roles.Crewmate;
using MiraAPI.GameOptions;

namespace TouMegaChujoweExtension.Patches.Pelican;

[HarmonyPatch(typeof(TimeLordRewindSystem), nameof(TimeLordRewindSystem.StartRewind))]
public static class PelicanTimeLordPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        try
        {
            // Eject all swallowed players from all Pelicans when time rewinds.
            // This prevents them from being trapped inside the Pelican or becoming invisible.
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null) continue;
                
                if (PelicanSystem.IsSwallowed(player.PlayerId))
                {
                    var pelicanId = PelicanSystem.GetPelicanOf(player.PlayerId);
                    if (pelicanId.HasValue)
                    {
                        PelicanSystem.ReleaseAll(pelicanId.Value);
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"[PelicanTimeLordPatch] Error releasing swallowed players on rewind: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(TimeLordRewindSystem), nameof(TimeLordRewindSystem.ConfigureHostRevives))]
public static class TimeLordReviveFilterPatch
{
    [HarmonyPrefix]
    public static void Prefix(ref Il2CppSystem.Collections.Generic.List<(byte VictimId, float KillAgeSeconds)>? revives)
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
            UnityEngine.Debug.LogError($"[TimeLordReviveFilterPatch] Error filtering revives: {ex.Message}");
        }
    }
}
