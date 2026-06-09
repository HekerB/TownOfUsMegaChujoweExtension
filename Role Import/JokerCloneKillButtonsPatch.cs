/*using HarmonyLib;
using System;
using System.Reflection;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Joker;

[HarmonyPatch]
public static class JokerCloneKillButtonsPatch
{
    private static bool IsBadButtonType(object btn)
    {
        var n = btn.GetType().Name;
        return n == "WerewolfRampageButton";
    }

    private static float GetDistanceFromButton(object btn, float fallback)
    {
        try
        {
            var t = btn.GetType();
            var p = t.GetProperty("Distance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && p.PropertyType == typeof(float))
                return (float)p.GetValue(btn);

            var f = t.GetField("Distance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null && f.FieldType == typeof(float))
                return (float)f.GetValue(btn);
        }
        catch { }
        return fallback;
    }

    private static bool TryGetClosestActiveClone(Vector2 from, float maxDistance, out int cloneIndex)
    {
        cloneIndex = -1;
        var best = maxDistance;
        var clones = JokerCloneSystem.Clones;

        for (int i = 0; i < clones.Count; i++)
        {
            var c = clones[i];
            if (c == null || c.IsPreview) continue;
            if (c.Fake?.body == null) continue;

            var p = (Vector2)c.Fake.body.transform.position;
            var d = Vector2.Distance(from, p);
            if (d <= best)
            {
                best = d;
                cloneIndex = i;
            }
        }

        return cloneIndex >= 0;
    }

private static bool TryKillClone(PlayerControl killer, float distance, Action onCooldown)
{
    if (killer == null || killer.Data == null || killer.Data.IsDead || MeetingHud.Instance) return false;

    if (!JokerCloneSystem.TryGetClosestClone(killer.GetTruePosition(), distance, out var idx, out _))
        return false;

    var clone = JokerCloneSystem.Clones[idx];
    if (clone.IsPreview) return false;

    JokerRole.RpcJokerCloneKilled(killer, clone.JokerId, idx);
    onCooldown?.Invoke();
    return true;
}

    [HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool KillButtonPrefix()
    {
        var local = PlayerControl.LocalPlayer;
        var dist = JokerCloneInteractionPatches.GetKillDistanceStatic();

        if (TryKillClone(local, dist, () =>
        {
            try { local!.SetKillTimer(local.GetKillCooldown()); } catch { }
        })) return false;

        return true;
    }

    [HarmonyPatch(typeof(TownOfUsButton), nameof(TownOfUsButton.ClickHandler))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool TownOfUsButtonPrefix(TownOfUsButton __instance)
    {
        if (__instance is TouMegaChujoweExtension.Buttons.Neutral.JokerPlaceCloneButton) return true;
        if (__instance is not IKillButton) return true;
        if (IsBadButtonType(__instance)) return true;

        var local = PlayerControl.LocalPlayer;
        var dist = JokerCloneInteractionPatches.GetKillDistanceStatic();

        if (TryKillClone(local, dist, () => __instance.SetTimer(__instance.Cooldown)))
            return false;

        return true;
    }

    static MethodBase TargetMethod()
        => AccessTools.Method(typeof(TownOfUsTargetButton<PlayerControl>), nameof(TownOfUsTargetButton<PlayerControl>.ClickHandler));

    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool TownOfUsTargetButtonPrefix(TownOfUsTargetButton<PlayerControl> __instance)
    {
        if (__instance is not IKillButton) return true;
        if (IsBadButtonType(__instance)) return true;

        var local = PlayerControl.LocalPlayer;
        var dist = GetDistanceFromButton(__instance, JokerCloneInteractionPatches.GetKillDistanceStatic());

        if (TryKillClone(local, dist, () => __instance.SetTimer(__instance.Cooldown)))
            return false;

        return true;
    }
}*/