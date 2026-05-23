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

        var targetPlayer = MiscUtils.PlayerById(targetId);
        if (guesser != null && targetPlayer != null && AreOnSameJackalTeam(guesser, targetPlayer))
        {
            __result = true;
        }
    }

    public static bool AreOnSameJackalTeam(PlayerControl? playerA, PlayerControl? playerB)
    {
        if (playerA == null || playerB == null) return false;
        if (playerA.PlayerId == playerB.PlayerId) return true;

        var sidekickA = playerA.GetModifier<SidekickModifier>();
        var sidekickB = playerB.GetModifier<SidekickModifier>();

        if (sidekickA != null && sidekickB != null)
        {
            return sidekickA.JackalId == sidekickB.JackalId && sidekickA.JackalId != 255;
        }

        var jackalA = playerA.GetRole<JackalRole>();
        if (jackalA != null && sidekickB != null && sidekickB.JackalId == playerA.PlayerId)
        {
            return true;
        }

        var jackalB = playerB.GetRole<JackalRole>();
        if (jackalB != null && sidekickA != null && sidekickA.JackalId == playerB.PlayerId)
        {
            return true;
        }

        return false;
    }
}
