using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using TouMegaChujoweExtension.Modifiers.Crewmate;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TownOfUs.Utilities;
using TownOfUs.Buttons;
using UnityEngine;
using System;
using System.Linq;
using TouMegaChujoweExtension;
using MiraAPI.Modifiers;
using HarmonyLib;

namespace TouMegaChujoweExtension.Events.Crewmate;

public static class TavernKeeperEvents
{
    [RegisterEvent]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        try
        {
            if (@event.Source == null || @event.Target == null || MeetingHud.Instance != null || ExileController.Instance != null) return;

            if (IsRoleblocked(@event.Source) || IsRoleblocked(@event.Target))
            {
                @event.Cancel();

                if (@event.Source.AmOwner)
                {
                    ResetLocalKillAttempt(@event.Source);
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[TOUMCE] Exception in TavernKeeper BeforeMurderEventHandler: {ex}");
        }
    }

    public static bool IsRoleblocked(PlayerControl? player)
    {
        return player != null && player.HasModifier<RoleblockedModifier>();
    }

    public static void ResetLocalKillAttempt(PlayerControl source)
    {
        source.SetKillTimer(source.GetKillCooldown());

        if (HudManager.Instance?.KillButton != null)
        {
            HudManager.Instance.KillButton.SetTarget(null);
        }

        foreach (var button in MiraAPI.Hud.CustomButtonManager.Buttons)
        {
            if (button == null || source.Data?.Role == null || !button.Enabled(source.Data.Role) || button is not IKillButton) continue;
            button.Timer = button.Cooldown;
        }

        Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.TavernKeeper, alpha: 0.5f));
    }
}

[HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
public static class TavernKeeperBlockVanillaKillClickPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool Prefix()
    {
        var local = PlayerControl.LocalPlayer;
        if (!TavernKeeperEvents.IsRoleblocked(local)) return true;

        TavernKeeperEvents.ResetLocalKillAttempt(local);
        return false;
    }
}

[HarmonyPatch(typeof(KillButton), nameof(KillButton.SetTarget))]
public static class TavernKeeperBlockVanillaKillTargetPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool Prefix(KillButton __instance)
    {
        if (!TavernKeeperEvents.IsRoleblocked(PlayerControl.LocalPlayer)) return true;

        __instance.currentTarget = null;
        return false;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckMurder))]
public static class TavernKeeperBlockCmdMurderPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool Prefix(PlayerControl __instance)
    {
        if (!TavernKeeperEvents.IsRoleblocked(__instance)) return true;
        if (__instance.AmOwner) TavernKeeperEvents.ResetLocalKillAttempt(__instance);
        return false;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
public static class TavernKeeperBlockCheckMurderPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool Prefix(PlayerControl __instance)
    {
        if (!TavernKeeperEvents.IsRoleblocked(__instance)) return true;
        if (__instance.AmOwner) TavernKeeperEvents.ResetLocalKillAttempt(__instance);
        return false;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
public static class TavernKeeperBlockMurderPlayerPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool Prefix(PlayerControl __instance)
    {
        if (!TavernKeeperEvents.IsRoleblocked(__instance)) return true;
        if (__instance.AmOwner) TavernKeeperEvents.ResetLocalKillAttempt(__instance);
        return false;
    }
}
