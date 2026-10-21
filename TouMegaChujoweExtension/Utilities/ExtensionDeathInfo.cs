using MiraAPI.Translation;
using TownOfUs.Modules;
using TownOfUs.Modules.Components;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Utilities;

/// <summary>
/// The death details TOU-Mira 1.7.3 keeps in <see cref="GameHistory.PlayerStats"/>. They used to live on a
/// DeathHandlerModifier that a player only had once dead; the method names and parameters are the old ones.
/// </summary>
public static class ExtensionDeathInfo
{
    public static int CurrentRound => HudManagerHelper.Instance != null ? HudManagerHelper.Instance.CurrentRound : 1;

    public static bool TryGet(PlayerControl? player, out PlayerStats stats)
    {
        stats = null!;
        return player != null && GameHistory.PlayerStats.TryGetValue(player.PlayerId, out stats!);
    }

    /// <summary>
    /// The old DeathHandlerModifier only existed on dead players, so reading it implied the player was dead.
    /// </summary>
    public static bool TryGetDead(PlayerControl? player, out PlayerStats stats)
    {
        stats = null!;
        return player != null && player.HasDied() && TryGet(player, out stats);
    }

    public static bool IsFullyDead(PlayerControl player) => GameHistory.IsFullyDead(player);

    public static void RpcUpdateDeathHandler(PlayerControl player, string causeOfDeath = "null", int roundOfDeath = -1,
        DeathHandlerOverride diedThisRound = DeathHandlerOverride.Ignore, string killedBy = "null",
        DeathHandlerOverride lockInfo = DeathHandlerOverride.Ignore)
    {
        GameHistory.RpcUpdateDeathHandler(player, causeOfDeath, roundOfDeath, diedThisRound, killedBy, lockInfo);
    }

    public static void RpcUpdateLocalDeathHandler(PlayerControl player, PlayerControl killedBy,
        string causeOfDeath = "null", int roundOfDeath = -1,
        DeathHandlerOverride diedThisRound = DeathHandlerOverride.Ignore, string killedByString = "null",
        DeathHandlerOverride lockInfo = DeathHandlerOverride.Ignore)
    {
        GameHistory.RpcUpdateLocalDeathHandler(player, killedBy, causeOfDeath, roundOfDeath, diedThisRound,
            killedByString, lockInfo);
    }

    /// <summary>
    /// Writes already-localized death text for this client only, like the old UpdateDeathHandlerImmediate.
    /// </summary>
    public static void UpdateDeathHandlerImmediate(PlayerControl player, string causeOfDeath = "null",
        int roundOfDeath = -1, DeathHandlerOverride diedThisRound = DeathHandlerOverride.Ignore,
        string killedBy = "null", DeathHandlerOverride lockInfo = DeathHandlerOverride.Ignore)
    {
        if (player == null || !GameHistory.PlayerStats.ContainsKey(player.PlayerId) || HudManagerHelper.Instance == null)
        {
            return;
        }

        GameHistory.UpdatePlayerDeathData(player.PlayerId, causeOfDeath, 0f, roundOfDeath, diedThisRound, killedBy,
            lockInfo: lockInfo);
    }

    /// <summary>
    /// Clears the death details of a player who came back alive (Pelican spat them out).
    /// </summary>
    public static void ClearForRevive(PlayerControl player)
    {
        if (!TryGet(player, out var stats))
        {
            return;
        }

        stats.DeathString = MiraLocaleManager.Get("Alive");
        stats.RoundOfDeath = -1;
        stats.DiedThisRound = false;
        stats.KilledBy = string.Empty;
        stats.ExtendedCauseOfDeath = string.Empty;
        stats.LockDeathInfo = false;
        stats.PlayerState = StoredPlayerState.Alive;
    }
}
