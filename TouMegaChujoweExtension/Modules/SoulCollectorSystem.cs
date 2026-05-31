using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMegaChujoweExtension.Modules;

public static class SoulCollectorSystem
{
    private const float DeathBodyBrightness = 0.45f;

    private static readonly HashSet<byte> DeathBodies = [];
    private static readonly HashSet<byte> AppliedDeathBodies = [];

    public static void Clear()
    {
        DeathBodies.Clear();
        AppliedDeathBodies.Clear();
    }

    public static bool IsDeathBody(byte playerId) => DeathBodies.Contains(playerId);

    public static void MarkDeathBody(byte playerId)
    {
        DeathBodies.Add(playerId);
        AppliedDeathBodies.Remove(playerId);
        ApplyDeathBodyVisual(playerId);
    }

    public static void UpdateDeathBodies()
    {
        foreach (var bodyId in DeathBodies.ToArray())
        {
            ApplyDeathBodyVisual(bodyId);
        }
    }

    private static void ApplyDeathBodyVisual(byte playerId)
    {
        var body = Object.FindObjectsOfType<DeadBody>().FirstOrDefault(x => x.ParentId == playerId);
        if (body == null)
        {
            return;
        }

        body.Reported = true;
        if (body.myCollider != null)
        {
            body.myCollider.enabled = false;
        }

        if (AppliedDeathBodies.Contains(playerId))
        {
            return;
        }

        foreach (var renderer in body.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer == null)
            {
                continue;
            }

            var color = renderer.color;
            renderer.color = new Color(
                color.r * DeathBodyBrightness,
                color.g * DeathBodyBrightness,
                color.b * DeathBodyBrightness,
                color.a);
        }

        foreach (var text in body.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text == null)
            {
                continue;
            }

            var color = text.color;
            text.color = new Color(
                color.r * DeathBodyBrightness,
                color.g * DeathBodyBrightness,
                color.b * DeathBodyBrightness,
                color.a);
        }

        AppliedDeathBodies.Add(playerId);
    }
}
