using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Utilities;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using TownOfUs.Modifiers.Game;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles.Neutral;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Jackal;

[HarmonyPatch]
public static class JackalTeamKillBlockPatch
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        var methods = new List<MethodBase>();

        try
        {
            var killButtonTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => {
                    try { return a.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .Where(t => t != null && typeof(IKillButton).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in killButtonTypes)
            {
                var method = AccessTools.Method(type, "IsTargetValid", new[] { typeof(PlayerControl) });
                if (method != null)
                {
                    methods.Add(method);
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[TOUMCE] Error in JackalTeamKillBlockPatch.TargetMethods: {ex}");
        }

        return methods.Distinct();
    }

    [HarmonyPostfix]
    public static void Postfix(ref bool __result, PlayerControl? target)
    {
        if (!__result || target == null) return;

        var local = PlayerControl.LocalPlayer;
        if (local == null) return;

        if (IsJackalAlly(local, target))
        {
            __result = false;
        }
    }

    public static bool IsJackalAlly(PlayerControl playerA, PlayerControl playerB)
    {
        if (playerA == null || playerB == null || playerA.PlayerId == playerB.PlayerId)
        {
            return false;
        }

        byte jackalIdA = 255;
        if (playerA.GetRole<JackalRole>() != null) jackalIdA = playerA.PlayerId;
        else if (playerA.TryGetModifier<SidekickModifier>(out var modA) && modA != null) jackalIdA = modA.JackalId;

        if (jackalIdA == 255 && JackalStartPatch.PendingAssignments.TryGetValue(playerA.PlayerId, out var pendingA))
        {
            jackalIdA = pendingA;
        }

        byte jackalIdB = 255;
        if (playerB.GetRole<JackalRole>() != null) jackalIdB = playerB.PlayerId;
        else if (playerB.TryGetModifier<SidekickModifier>(out var modB) && modB != null) jackalIdB = modB.JackalId;

        if (jackalIdB == 255 && JackalStartPatch.PendingAssignments.TryGetValue(playerB.PlayerId, out var pendingB))
        {
            jackalIdB = pendingB;
        }

        return jackalIdA != 255 && jackalIdA == jackalIdB;
    }
}

[HarmonyPatch(typeof(KillButton), nameof(KillButton.SetTarget))]
public static class KillButtonSetTargetPatch
{
    [HarmonyPrefix]
    public static void Prefix(KillButton __instance, ref PlayerControl target)
    {
        if (target == null) return;
        var local = PlayerControl.LocalPlayer;
        if (local == null) return;

        if (JackalTeamKillBlockPatch.IsJackalAlly(local, target))
        {
            target = null;
        }
    }
}

[HarmonyPatch]
public static class JackalGuessBlockPatch
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        var assassinClick = AccessTools.Method(typeof(AssassinModifier), "ClickGuess");
        if (assassinClick != null) yield return assassinClick;

        var doomsayerClick = AccessTools.Method(typeof(DoomsayerRole), "ClickGuess");
        if (doomsayerClick != null) yield return doomsayerClick;

        var vigilanteClick = AccessTools.Method(typeof(VigilanteRole), "ClickGuess");
        if (vigilanteClick != null) yield return vigilanteClick;

        var deputyClick = AccessTools.Method(typeof(DeputyRole), "ClickGuess");
        if (deputyClick != null) yield return deputyClick;
    }

    [HarmonyPrefix]
    public static bool Prefix(object __instance, PlayerVoteArea voteArea)
    {
        var guesser = GetGuesser(__instance);
        var target = voteArea == null ? null : MiscUtils.PlayerById(voteArea.TargetPlayerId);

        if (guesser != null && target != null)
        {
            if (JackalTeamKillBlockPatch.IsJackalAlly(guesser, target))
            {
                return false; // Skip / block guessing
            }
        }

        return true;
    }

    private static PlayerControl? GetGuesser(object instance)
    {
        return instance switch
        {
            AssassinModifier assassin => assassin.Player,
            DoomsayerRole doomsayer => doomsayer.Player,
            VigilanteRole vigilante => vigilante.Player,
            DeputyRole deputy => deputy.Player,
            _ => null
        };
    }
}

