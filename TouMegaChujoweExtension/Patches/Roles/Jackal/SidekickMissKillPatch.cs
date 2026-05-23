using HarmonyLib;
using TownOfUs.Buttons.Crewmate;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TownOfUs.Utilities;
using TownOfUs.Roles;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using MiraAPI.Roles;
using TownOfUs.Options.Roles.Crewmate;
using TownOfUs.Modifiers;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Jackal;

[HarmonyPatch]
public static class SidekickMissKillPatch
{
    // Part 1: Prevent recruited Sheriffs and Officers from misfiring (committing suicide) when they kill opponents
    [HarmonyPatch(typeof(SheriffShootButton), "Misfire")]
    [HarmonyPrefix]
    public static bool SheriffMisfirePrefix(SheriffShootButton __instance)
    {
        if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.TryGetModifier<SidekickModifier>(out _))
        {
            var target = __instance.Target;
            if (target != null)
            {
                PlayerControl.LocalPlayer.RpcCustomMurder(target, MeetingCheck.OutsideMeeting);
            }
            return false; // Skip the misfire suicide logic
        }
        return true;
    }

    [HarmonyPatch(typeof(OfficerShootButton), "Misfire")]
    [HarmonyPrefix]
    public static bool OfficerMisfirePrefix(OfficerShootButton __instance)
    {
        if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.TryGetModifier<SidekickModifier>(out _))
        {
            var target = __instance.Target;
            if (target != null)
            {
                PlayerControl.LocalPlayer.RpcCustomMurder(target, MeetingCheck.OutsideMeeting);
                __instance.LoadedBullets--;
                TownOfUs.Roles.Crewmate.OfficerRole.RpcOfficerSyncBullets(
                    PlayerControl.LocalPlayer, 
                    __instance.RoundsBeforeReset, 
                    __instance.TotalBullets, 
                    __instance.LoadedBullets
                );
            }
            return false; // Skip the misfire suicide logic
        }
        return true;
    }

    // Part 1.5: Prevent normal Sheriffs and Officers from misfiring (suiciding) when shooting recruited crewmates
    [HarmonyPatch(typeof(SheriffShootButton), "OnClick")]
    [HarmonyPrefix]
    public static bool SheriffOnClickPrefix(SheriffShootButton __instance)
    {
        var target = __instance.Target;
        if (target == null) return true;

        if (target.TryGetModifier<SidekickModifier>(out _))
        {
            if (target.HasModifier<FirstDeadShield>() || target.HasModifier<BaseShieldModifier>())
            {
                return false; // Target is shielded, do nothing
            }

            // Shoot/kill target without misfire
            PlayerControl.LocalPlayer.RpcCustomMurder(target, MeetingCheck.OutsideMeeting);

            if (!MiraAPI.GameOptions.OptionGroupSingleton<SheriffOptions>.Instance.SheriffBodyReport)
            {
                Reactor.Utilities.Coroutines.Start(CoSetBodyReportable(target.PlayerId));
            }
            return false; // Skip original OnClick logic
        }
        return true;
    }

    [HarmonyPatch(typeof(OfficerShootButton), "OnClick")]
    [HarmonyPrefix]
    public static bool OfficerOnClickPrefix(OfficerShootButton __instance)
    {
        var target = __instance.Target;
        if (target == null) return true;

        if (target.TryGetModifier<SidekickModifier>(out _))
        {
            if (target.HasModifier<FirstDeadShield>() || target.HasModifier<BaseShieldModifier>())
            {
                return false; // Target is shielded, do nothing
            }

            // Shoot/kill target without misfire
            PlayerControl.LocalPlayer.RpcCustomMurder(target, MeetingCheck.OutsideMeeting);
            __instance.LoadedBullets--;
            TownOfUs.Roles.Crewmate.OfficerRole.RpcOfficerSyncBullets(
                PlayerControl.LocalPlayer, 
                __instance.RoundsBeforeReset, 
                __instance.TotalBullets, 
                __instance.LoadedBullets
            );

            if (!MiraAPI.GameOptions.OptionGroupSingleton<OfficerOptions>.Instance.CanSelfReport.Value)
            {
                Reactor.Utilities.Coroutines.Start(CoSetBodyReportable(target.PlayerId));
            }
            return false; // Skip original OnClick logic
        }
        return true;
    }

    private static System.Collections.IEnumerator CoSetBodyReportable(byte bodyId)
    {
        var waitDelegate =
            Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<Il2CppSystem.Func<bool>>((System.Func<bool>)(() => Helpers.GetBodyById(bodyId) != null));
        yield return new UnityEngine.WaitUntil(waitDelegate);
        var body = Helpers.GetBodyById(bodyId);

        if (body != null)
        {
            body.gameObject.layer = UnityEngine.LayerMask.NameToLayer("Ship");
            body.Reported = true;
        }
    }
}

// Part 2: Globally treat recruited players as Neutral Custom team instead of Crewmate
// This prevents Jailors, Sheriffs, Officers, and other roles from being punished when executing/shooting them.
[HarmonyPatch(typeof(TownOfUs.Utilities.Extensions))]
public static class SidekickAlignmentPatch
{
    [HarmonyPatch(nameof(TownOfUs.Utilities.Extensions.IsCrewmate), typeof(PlayerControl))]
    [HarmonyPrefix]
    public static bool IsCrewmatePrefix(PlayerControl player, ref bool __result)
    {
        if (player != null && player.TryGetModifier<SidekickModifier>(out _))
        {
            __result = false;
            return false;
        }
        return true;
    }

    [HarmonyPatch(nameof(TownOfUs.Utilities.Extensions.IsNeutral), typeof(PlayerControl))]
    [HarmonyPrefix]
    public static bool IsNeutralPrefix(PlayerControl player, ref bool __result)
    {
        if (player != null && player.TryGetModifier<SidekickModifier>(out _))
        {
            __result = true;
            return false;
        }
        return true;
    }

    [HarmonyPatch(nameof(TownOfUs.Utilities.Extensions.Is), typeof(PlayerControl), typeof(ModdedRoleTeams))]
    [HarmonyPrefix]
    public static bool IsModdedTeamPrefix(PlayerControl player, ModdedRoleTeams team, ref bool __result)
    {
        if (player != null && player.TryGetModifier<SidekickModifier>(out _))
        {
            if (team == ModdedRoleTeams.Crewmate)
            {
                __result = false;
                return false;
            }
            if (team == ModdedRoleTeams.Custom)
            {
                __result = true;
                return false;
            }
        }
        return true;
    }
}
