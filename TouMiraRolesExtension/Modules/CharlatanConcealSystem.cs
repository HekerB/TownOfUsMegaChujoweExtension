using MiraAPI.GameOptions;
using TouMiraRolesExtension.Options.Roles.Impostor;
using TouMiraRolesExtension.Roles.Impostor;
using UnityEngine;
using Object = UnityEngine.Object;
using TMPro;

namespace TouMiraRolesExtension.Modules;

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
            
            // Restore transparency when clearing
            if (BodyCache.TryGetValue(bodyId, out var body) && body != null)
            {
                SetBodyAlpha(body, 1f);
            }
            BodyCache.Remove(bodyId);
        }
    }

    public static void ConcealBody(byte charlatanId, byte bodyId, float channelDuration)
    {
        ConcealedBodies[bodyId] = new ConcealedBody(bodyId, charlatanId, Time.time, channelDuration, false);
        
        // Cache the body reference
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

        // Check if channeling is complete
        var elapsed = Time.time - concealed.ConcealedAt;
        if (!concealed.ChannelComplete)
        {
            // Still channeling - check if channel duration has passed
            if (elapsed >= concealed.ChannelDuration)
            {
                // Channel complete - mark it and keep concealed
                ConcealedBodies[bodyId] = concealed with { ChannelComplete = true };
                return true;
            }
            // Still channeling
            return true;
        }

        // Channel complete - body remains concealed indefinitely (until manually cleared)
        return true;
    }

    public static float GetConcealedReportRange(byte bodyId)
    {
        if (!IsBodyConcealed(bodyId))
        {
            return -1f;
        }

        var options = OptionGroupSingleton<CharlatanOptions>.Instance;
        // VeryShort: 0.5f (50% of normal distance), Short: 0.75f (75% of normal distance)
        return options.ConcealReportRange == ReportRangeType.VeryShort ? 0.5f : 0.75f;
    }

    public static void UpdateBodyTransparency()
    {
        var allBodies = Object.FindObjectsOfType<DeadBody>();
        var options = OptionGroupSingleton<CharlatanOptions>.Instance;
        
        foreach (var body in allBodies)
        {
            if (IsBodyConcealed(body.ParentId))
            {
                // VeryShort: barely visible (0.1 alpha), Short: more visible (0.3 alpha)
                var alpha = options.ConcealReportRange == ReportRangeType.VeryShort ? 0.1f : 0.3f;
                SetBodyAlpha(body, alpha);
                
                // Update cache
                BodyCache[body.ParentId] = body;
            }
            else if (BodyCache.ContainsKey(body.ParentId))
            {
                // Restore transparency if no longer concealed
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

        // Set alpha for all sprite renderers in the body
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

        // Set alpha for all text components
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

