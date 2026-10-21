using System;
using HarmonyLib;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Roles.Sniper;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class SniperSystemUpdatePatch
{
    /// <summary>
    /// Wrapped, because this hangs off <c>HudManager.Update</c> and scans every player once a frame.
    /// <para>
    /// A single player carrying null Data - a bot mid-spawn, someone mid-teardown - makes the role scan
    /// inside throw, and a throw out of a postfix propagates out of the patched method and takes every
    /// later postfix on it with it. What the player sees then is a dead HUD: frozen in place, no
    /// buttons, abilities unable to find a target. Nothing drawn by a rifle sprite is worth that, so it
    /// fails soft and stays loud.
    /// </para>
    /// </summary>
    [HarmonyPostfix]
    public static void Postfix()
    {
        try
        {
            SniperSystem.Update();
        }
        catch (Exception ex)
        {
            Error($"[Sniper] HUD update failed soft: {ex}");
        }
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
public static class SniperMeetingStartPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        SniperSystem.RoundReset();
    }
}

[HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
public static class SniperExileWrapUpPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        SniperSystem.RoundReset();
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
public static class SniperGameEndPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        SniperSystem.RoundReset();
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CanMove), MethodType.Getter)]
public static class SniperStasisCanMovePatch
{
    [HarmonyPrefix]
    public static bool Prefix(PlayerControl __instance, ref bool __result)
    {
        if (SniperSystem.IsPlayerFrozen(__instance.PlayerId))
        {
            __result = false;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
public static class SniperStasisFixedUpdatePatch
{
    public static readonly System.Collections.Generic.HashSet<byte> FrozenPlayers = new();

    [HarmonyPostfix]
    public static void Postfix(PlayerControl __instance)
    {
        if (__instance == null) return;

        bool inStasis = SniperSystem.IsPlayerFrozen(__instance.PlayerId);
        if (inStasis && !__instance.HasDied())
        {
            if (__instance.moveable)
            {
                __instance.moveable = false;
                FrozenPlayers.Add(__instance.PlayerId);
            }
            if (__instance.MyPhysics != null && __instance.MyPhysics.body != null)
            {
                __instance.MyPhysics.body.velocity = UnityEngine.Vector2.zero;
            }
        }
        else if (FrozenPlayers.Contains(__instance.PlayerId))
        {
            __instance.moveable = true;
            FrozenPlayers.Remove(__instance.PlayerId);
        }
    }
}

[HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.FixedUpdate))]
public static class SniperStasisNetworkTransformPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool Prefix(CustomNetworkTransform __instance)
    {
        if (__instance.myPlayer != null && SniperSystem.IsPlayerFrozen(__instance.myPlayer.PlayerId))
        {
            return false;
        }
        return true;
    }
}
