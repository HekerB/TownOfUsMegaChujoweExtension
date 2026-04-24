using UnityEngine;

namespace TouMegaChujoweExtension.Modules;

public static class CharlatanDeceiveSystem
{
    private sealed record DeceiveState(byte BodyId, float ActivatedAt, float ExpiresAt);

    private static readonly Dictionary<byte, DeceiveState> ActiveDeceives = new();

    public static void ClearAll()
    {
        ActiveDeceives.Clear();
    }

    public static void ClearForPlayer(byte charlatanId)
    {
        var toRemove = ActiveDeceives.Where(kvp => kvp.Value.BodyId == charlatanId).ToList();
        foreach (var kvp in toRemove)
        {
            ActiveDeceives.Remove(kvp.Key);
        }
    }

    public static void ActivateDeceive(byte charlatanId, byte bodyId, float duration)
    {
        ActiveDeceives[charlatanId] = new DeceiveState(bodyId, Time.time, Time.time + duration);
    }

    public static bool CanDeceiveReport(byte charlatanId, byte bodyId)
    {
        if (!ActiveDeceives.TryGetValue(charlatanId, out var state))
        {
            return false;
        }

        if (state.BodyId != bodyId)
        {
            return false;
        }

        if (Time.time >= state.ExpiresAt)
        {
            ActiveDeceives.Remove(charlatanId);
            return false;
        }

        return true;
    }

    public static float GetRemainingTime(byte charlatanId)
    {
        if (!ActiveDeceives.TryGetValue(charlatanId, out var state))
        {
            return 0f;
        }

        return Mathf.Max(0f, state.ExpiresAt - Time.time);
    }
}