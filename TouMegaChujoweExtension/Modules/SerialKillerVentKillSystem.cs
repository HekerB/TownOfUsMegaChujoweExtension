using TownOfUs.Utilities;
using System.Collections.Generic;

namespace TouMegaChujoweExtension.Modules;

/// <summary>
/// Tracks when a Serial Killer can kill someone in a vent.
/// </summary>
public static class SerialKillerVentKillSystem
{
    private static readonly Dictionary<byte, byte> VentKillTargets = new();
    private static readonly Dictionary<byte, bool> ProcessingVentKill = new();
    private static readonly Dictionary<byte, int> EscapeVentUsages = new();

    public static void SetVentKillTarget(byte serialKillerId, PlayerControl? target)
    {
        if (target == null)
        {
            VentKillTargets.Remove(serialKillerId);
        }
        else
        {
            VentKillTargets[serialKillerId] = target.PlayerId;
        }
    }

    public static bool TryGetVentKillTarget(byte serialKillerId, out PlayerControl? target)
    {
        if (VentKillTargets.TryGetValue(serialKillerId, out var targetId))
        {
            var player = MiscUtils.PlayerById(targetId);
            if (player != null)
            {
                target = player;
                return true;
            }
        }

        target = null;
        return false;
    }

    public static void ClearAll()
    {
        VentKillTargets.Clear();
        ProcessingVentKill.Clear();
        EscapeVentUsages.Clear();
    }

    public static void ClearForPlayer(byte playerId)
    {
        VentKillTargets.Remove(playerId);
        ProcessingVentKill.Remove(playerId);
        EscapeVentUsages.Remove(playerId);
    }

    public static void SetProcessingVentKill(byte serialKillerId, bool processing)
    {
        if (processing)
            ProcessingVentKill[serialKillerId] = true;
        else
            ProcessingVentKill.Remove(serialKillerId);
    }

    public static bool IsProcessingVentKill(byte serialKillerId) =>
        ProcessingVentKill.ContainsKey(serialKillerId);

    public static void SetEscapeVentUsages(byte serialKillerId, int usages)
    {
        if (usages <= 0)
            EscapeVentUsages.Remove(serialKillerId);
        else
            EscapeVentUsages[serialKillerId] = usages;
    }

    public static bool HasEscapeVentUsages(byte serialKillerId) =>
        EscapeVentUsages.TryGetValue(serialKillerId, out var usages) && usages > 0;

    public static bool TryConsumeEscapeVentUsage(byte serialKillerId)
    {
        if (EscapeVentUsages.TryGetValue(serialKillerId, out var usages) && usages > 0)
        {
            usages--;
            if (usages <= 0)
                EscapeVentUsages.Remove(serialKillerId);
            else
                EscapeVentUsages[serialKillerId] = usages;
            return true;
        }
        return false;
    }
}