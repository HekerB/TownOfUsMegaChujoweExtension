using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using System.Collections.Generic;
using System.Linq;
using TouMegaChujoweExtension.Options.Modifiers.Neutral;
using TownOfUs.Modifiers.Game.Alliance;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modules;

public static class VentTrapSystem
{
    private sealed record TrapEntry(byte OwnerId, int RoundsRemaining);


    private static readonly Dictionary<int, TrapEntry> Traps = new();

    public static bool TryGetTraprId(int ventId, out byte traprId)
    {
        if (Traps.TryGetValue(ventId, out var entry))
        {
            traprId = entry.OwnerId;
            return true;
        }

        traprId = default;
        return false;
    }

    public static bool IsTrapped(int ventId) => Traps.ContainsKey(ventId);

    public static void Place(int ventId, byte traprId)
    {
        var rounds = (int)OptionGroupSingleton<TrapperOptions>.Instance.TrapRoundsLast;
        Traps[ventId] = new TrapEntry(traprId, rounds);
    }

    public static void Remove(int ventId)
    {
        Traps.Remove(ventId);
    }

    public static void DecrementRoundsAndRemoveExpired()
    {
        var roundsLast = (int)OptionGroupSingleton<TrapperOptions>.Instance.TrapRoundsLast;
        if (roundsLast <= 0 || Traps.Count == 0)
        {
            return;
        }

        var toRemove = new List<int>();
        var toUpdate = new List<KeyValuePair<int, TrapEntry>>();

        foreach (var kvp in Traps)
        {
            var newRemaining = kvp.Value.RoundsRemaining - 1;
            if (newRemaining <= 0)
            {
                toRemove.Add(kvp.Key);
            }
            else
            {
                toUpdate.Add(new(kvp.Key, kvp.Value with { RoundsRemaining = newRemaining }));
            }
        }

        foreach (var ventId in toRemove)
        {
            Traps.Remove(ventId);
        }

        foreach (var kvp in toUpdate)
        {
            Traps[kvp.Key] = kvp.Value;
        }
    }

    public static void ClearAll()
    {
        Traps.Clear();
    }

    public static void ClearOwnedBy(byte traprId)
    {
        if (Traps.Count == 0)
        {
            return;
        }

        var toRemove = new List<int>();
        foreach (var kvp in Traps)
        {
            if (kvp.Value.OwnerId == traprId)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var ventId in toRemove)
        {
            Traps.Remove(ventId);
        }
    }

    public static bool IsEligibleToBeTrapped(PlayerControl pc)
    {
        if (pc == null || pc.HasDied())
        {
            return false;
        }

        var targets = OptionGroupSingleton<TrapperOptions>.Instance.TrapTargets;
        var isTarget = targets switch
        {
            VentTrapTargets.Impostors => pc.IsImpostor(),
            VentTrapTargets.ImpostorsAndNeutrals => pc.IsImpostor() || pc.IsNeutral(),
            VentTrapTargets.All => true,
            _ => pc.IsImpostor() || pc.IsNeutral()
        };

        if (isTarget) return true;

        // Crewmates who can vent should also be eligible if they are using a vent
        if (pc.Data?.Role is EngineerRole || pc.Data?.Role is EngineerTouRole) return true;

        // Check for Egotist if they have venting enabled
        if (pc.HasModifier<EgotistModifier>() && OptionGroupSingleton<EgotistExtendedOptions>.Instance.CanVent) return true;

        // Dynamic check for any MiraAPI modifier that grants venting ability (Doctor's effect, etc.)
        if (pc.GetModifiers<BaseModifier>().Any(m => m.CanVent() == true)) return true;

        return false;
    }

    public static Vector2 GetVentTopPosition(Vent vent)
    {
        return (Vector2)vent.transform.position + new Vector2(0f, 0.3636f);
    }

    public static IEnumerable<int> GetTrapsOwnedBy(byte traprId)
    {
        return Traps.Where(kvp => kvp.Value.OwnerId == traprId).Select(kvp => kvp.Key);
    }

    public static IEnumerable<KeyValuePair<int, int>> GetTrapEntriesOwnedBy(byte traprId)
    {
        return Traps
            .Where(kvp => kvp.Value.OwnerId == traprId)
            .Select(kvp => new KeyValuePair<int, int>(kvp.Key, kvp.Value.RoundsRemaining));
    }
}














