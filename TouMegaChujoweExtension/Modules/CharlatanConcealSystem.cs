using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using UnityEngine;
using Object = UnityEngine.Object;
using TMPro;

namespace TouMegaChujoweExtension.Modules;

public static class CharlatanConcealSystem
{
    private sealed record ConcealedBody(byte BodyId, byte CharlatanId, float ConcealedAt, float ChannelDuration, bool ChannelComplete);

    private static readonly Dictionary<byte, ConcealedBody> ConcealedBodies = new();
    private static readonly Dictionary<byte, DeadBody> BodyCache = new();

    public static void ClearAll()
    {
        ConcealedBodies.Clear();
        BodyCache.Clear();
    }

    public static void ClearForPlayer(byte charlatanId)
    {
        var toRemove = ConcealedBodies.Where(kvp => kvp.Value.CharlatanId == charlatanId).ToList();
        foreach (var kvp in toRemove)
        {
            var bodyId = kvp.Key;
            ConcealedBodies.Remove(bodyId);

            if (BodyCache.TryGetValue(bodyId, out var body) && body != null)
            {
                SetBodyAlpha(body, 1f);
            }
            BodyCache.Remove(bodyId);
        }
    }

    public static void ConcealBody(byte charlatanId, byte bodyId, float channelDuration)
    {
        if (IsBodyConcealed(bodyId))
        {
            return;
        }

        ConcealedBodies[bodyId] = new ConcealedBody(bodyId, charlatanId, Time.time, channelDuration, false);

        var body = Object.FindObjectsOfType<DeadBody>().FirstOrDefault(x => x.ParentId == bodyId);
        if (body != null)
        {
            BodyCache[bodyId] = body;
        }
    }

    public static void MarkChannelComplete(byte bodyId)
    {
        if (ConcealedBodies.TryGetValue(bodyId, out var concealed))
        {
            ConcealedBodies[bodyId] = concealed with { ChannelComplete = true };
        }
    }

    public static bool IsBodyConcealed(byte bodyId)
    {
        if (!ConcealedBodies.TryGetValue(bodyId, out var concealed))
        {
            return false;
        }

        var elapsed = Time.time - concealed.ConcealedAt;
        if (!concealed.ChannelComplete)
        {
            if (elapsed >= concealed.ChannelDuration)
            {
                ConcealedBodies[bodyId] = concealed with { ChannelComplete = true };
                return true;
            }
            return true;
        }

        return true;
    }

    public static float GetConcealedReportRange(byte bodyId)
    {
        if (!IsBodyConcealed(bodyId))
        {
            return -1f;
        }

        var options = OptionGroupSingleton<CharlatanOptions>.Instance;
        return options.ConcealReportRange switch
        {
            ReportRangeType.ExtremelyShort => 0.3f,
            ReportRangeType.VeryShort => 0.5f,
            ReportRangeType.Short => 0.75f,
            _ => 0.5f
        };
    }

    public static void UpdateBodyTransparency()
    {
        var allBodies = Object.FindObjectsOfType<DeadBody>();
        var options = OptionGroupSingleton<CharlatanOptions>.Instance;

        foreach (var body in allBodies)
        {
            if (IsBodyConcealed(body.ParentId))
            {
                var alpha = options.ConcealReportRange switch
                {
                    ReportRangeType.ExtremelyShort => 0.08f,
                    ReportRangeType.VeryShort => 0.15f,
                    ReportRangeType.Short => 0.4f,
                    _ => 0.1f
                };
                SetBodyAlpha(body, alpha);

                BodyCache[body.ParentId] = body;
            }
            else if (BodyCache.ContainsKey(body.ParentId))
            {
                SetBodyAlpha(body, 1f);
                BodyCache.Remove(body.ParentId);
            }
        }
    }

    private static void SetBodyAlpha(DeadBody body, float alpha)
    {
        if (body == null)
        {
            return;
        }

        foreach (var sr in body.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null)
            {
                continue;
            }

            var c = sr.color;
            c.a = Mathf.Clamp01(alpha);
            sr.color = c;
        }

        foreach (var tmp in body.GetComponentsInChildren<TMP_Text>(true))
        {
            if (tmp == null)
            {
                continue;
            }

            var c = tmp.color;
            c.a = Mathf.Clamp01(alpha);
            tmp.color = c;
        }
    }
}
