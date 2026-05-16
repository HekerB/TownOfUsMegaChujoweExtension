using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using TownOfUs.Modifiers.Game;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles.Neutral;

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
    public static void Postfix(PlayerVoteArea voteArea, ref bool __result)
    {
        if (__result) return;
        
        if (voteArea == null) return;
        
        var targetId = voteArea.TargetPlayerId;
        if (ForestallerSystem.IsForestallerRevealed(targetId))
        {
            __result = true;
        }
    }
}














