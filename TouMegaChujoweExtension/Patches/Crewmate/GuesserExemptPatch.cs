using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using TownOfUs.Modifiers.Game;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles.Neutral;
using TouMegaChujoweExtension.Modules;

namespace TouMegaChujoweExtension.Patches.Crewmate;

[HarmonyPatch]
public static class GuesserExemptPatch
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return typeof(AssassinModifier).GetMethod("IsExempt", BindingFlags.Public | BindingFlags.Instance)!;
        yield return typeof(DoomsayerRole).GetMethod("IsExempt", BindingFlags.Public | BindingFlags.Instance)!;
        yield return typeof(VigilanteRole).GetMethod("IsExempt", BindingFlags.Public | BindingFlags.Instance)!;
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
