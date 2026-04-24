using System.Collections;
using MiraAPI.Modifiers;
using Reactor.Utilities;
using TouMegaChujoweExtension.Modifiers;
using TownOfUs.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMegaChujoweExtension.Modules;

public static class VultureSystem
{
    private static readonly HashSet<byte> EatenBodies = new();
    private static readonly Dictionary<byte, float> ActiveScavenges = new();

    public static bool IsBodyEaten(byte bodyId)
    {
        return EatenBodies.Contains(bodyId);
    }

    public static void MarkBodyEaten(byte bodyId)
    {
        EatenBodies.Add(bodyId);
    }

    public static void StartScavenge(byte VultureId, float duration)
    {
        if (ActiveScavenges.ContainsKey(VultureId))
        {
            return;
        }

        ActiveScavenges[VultureId] = Time.time + duration;
        Coroutines.Start(CoScavenge(VultureId, duration));
    }

    private static IEnumerator CoScavenge(byte VultureId, float duration)
    {
        var Vulture = MiscUtils.PlayerById(VultureId);
        if (Vulture == null || Vulture.HasDied())
        {
            ActiveScavenges.Remove(VultureId);
            yield break;
        }

        var endTime = Time.time + duration;
        var updateInterval = 0.1f;
        var lastUpdate = Time.time;

        while (Time.time < endTime)
        {
            if (Vulture == null || Vulture.HasDied() || MeetingHud.Instance != null)
            {
                break;
            }

            if (Time.time - lastUpdate >= updateInterval)
            {
                UpdateScavengeArrows(Vulture);
                lastUpdate = Time.time;
            }

            yield return null;
        }

        if (Vulture != null && !Vulture.HasDied())
        {
            var modifiers = Vulture.GetModifiers<VultureBodyArrowModifier>().ToList();
            foreach (var modifier in modifiers)
            {
                Vulture.RpcRemoveModifier<VultureBodyArrowModifier>();
            }
        }

        ActiveScavenges.Remove(VultureId);
    }

    private static void UpdateScavengeArrows(PlayerControl Vulture)
    {
        if (Vulture == null || Vulture.HasDied() || !Vulture.AmOwner)
        {
            return;
        }

        var allBodies = Object.FindObjectsOfType<DeadBody>();
        var existingArrows = Vulture.GetModifiers<VultureBodyArrowModifier>().ToList();
        var existingBodyIds = existingArrows.Select(m => m.BodyId).ToHashSet();

        foreach (var arrow in existingArrows)
        {
            var bodyExists = allBodies.Any(b => b.ParentId == arrow.BodyId);
            if (!bodyExists)
            {
                Vulture.RpcRemoveModifier<VultureBodyArrowModifier>();
            }
        }

        foreach (var body in allBodies)
        {
            if (IsBodyEaten(body.ParentId) || existingBodyIds.Contains(body.ParentId))
            {
                continue;
            }

            if (body != null && body.gameObject != null && body.gameObject.activeSelf)
            {
                Vulture.AddModifier<VultureBodyArrowModifier>(body, body.ParentId);
            }
        }
    }

    public static void ClearForPlayer(byte playerId)
    {
        EatenBodies.RemoveWhere(b => false);
        ActiveScavenges.Remove(playerId);
    }

    public static void ClearAll()
    {
        EatenBodies.Clear();
        ActiveScavenges.Clear();
    }
}