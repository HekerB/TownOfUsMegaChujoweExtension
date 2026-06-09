using HarmonyLib;
using System.Reflection;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Buttons;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Joker;

[HarmonyPatch]
public static class JokerCloneKillButtonCanUsePatch
{
    static MethodBase TargetMethod()
        => AccessTools.Method(typeof(TownOfUsTargetButton<PlayerControl>),
            nameof(TownOfUsTargetButton<PlayerControl>.CanUse));

    [HarmonyPostfix]
    public static void Postfix(TownOfUsTargetButton<PlayerControl> __instance, ref bool __result)
    {
        if (__result) return;
        if (__instance is not IKillButton) return;

        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data == null || local.Data.IsDead) return;
        if (MeetingHud.Instance) return;
        if (HudManager.Instance?.Chat != null && HudManager.Instance.Chat.IsOpenOrOpening) return;
        if (!local.CanMove) return;

        var dist = GetDistance(__instance, JokerCloneInteractionPatches.GetKillDistanceStatic());

        if (!JokerCloneSystem.TryGetClosestClone(local.GetTruePosition(), dist, out var idx, out _))
            return;

        if (idx < 0 || idx >= JokerCloneSystem.Clones.Count) return;
        if (JokerCloneSystem.Clones[idx].IsPreview) return;

        __result = true;
    }

    private static float GetDistance(object btn, float fallback)
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
}