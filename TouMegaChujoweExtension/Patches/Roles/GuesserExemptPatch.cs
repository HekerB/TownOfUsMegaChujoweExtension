using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using TownOfUs.Modifiers.Game;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles.Neutral;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Utilities;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Modules;

namespace TouMegaChujoweExtension.Patches.Roles;

[HarmonyPatch]
public static class GuesserExemptPatch
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        var m1 = AccessTools.Method(typeof(AssassinModifier), "IsExempt");
        if (m1 != null) yield return m1;

        var m2 = AccessTools.Method(typeof(DoomsayerRole), "IsExempt");
        if (m2 != null) yield return m2;

        var m3 = AccessTools.Method(typeof(VigilanteRole), "IsExempt");
        if (m3 != null) yield return m3;

        var m4 = AccessTools.Method(typeof(DeputyRole), "IsExempt");
        if (m4 != null) yield return m4;
    }

    [HarmonyPostfix]
    public static void Postfix(object __instance, PlayerVoteArea voteArea, ref bool __result)
    {
        if (__result) return;
        
        if (voteArea == null) return;
        
        var targetId = voteArea.TargetPlayerId;
        if (ForestallerSystem.IsForestallerRevealed(targetId))
        {
            __result = true;
            return;
        }

        // Find the guesser player from the instance
        PlayerControl? guesser = null;
        if (__instance is AssassinModifier assassin)
        {
            guesser = assassin.Player;
        }
        else if (__instance is DoomsayerRole doomsayer)
        {
            guesser = doomsayer.Player;
        }
        else if (__instance is VigilanteRole vigilante)
        {
            guesser = vigilante.Player;
        }
        else if (__instance is DeputyRole deputy)
        {
            guesser = deputy.Player;
        }

        var targetPlayer = MiscUtils.PlayerById(targetId);
        if (guesser != null && targetPlayer != null && AreOnSameJackalTeam(guesser, targetPlayer))
        {
            __result = true;
            return;
        }

        if (guesser != null && targetPlayer != null && ApocalypseUtils.AreAllied(guesser, targetPlayer))
        {
            __result = true;
        }
    }

    public static bool AreOnSameJackalTeam(PlayerControl? playerA, PlayerControl? playerB)
    {
        if (playerA == null || playerB == null || playerA.PlayerId == playerB.PlayerId)
        {
            return false;
        }

        byte jackalIdA = 255;
        if (playerA.GetRole<JackalRole>() != null) jackalIdA = playerA.PlayerId;
        else if (playerA.TryGetModifier<SidekickModifier>(out var modA) && modA != null) jackalIdA = modA.JackalId;

        if (jackalIdA == 255 && Jackal.JackalStartPatch.PendingAssignments.TryGetValue(playerA.PlayerId, out var pendingA))
        {
            jackalIdA = pendingA;
        }

        byte jackalIdB = 255;
        if (playerB.GetRole<JackalRole>() != null) jackalIdB = playerB.PlayerId;
        else if (playerB.TryGetModifier<SidekickModifier>(out var modB) && modB != null) jackalIdB = modB.JackalId;

        if (jackalIdB == 255 && Jackal.JackalStartPatch.PendingAssignments.TryGetValue(playerB.PlayerId, out var pendingB))
        {
            jackalIdB = pendingB;
        }

        return jackalIdA != 255 && jackalIdA == jackalIdB;
    }
}
