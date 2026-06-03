using HarmonyLib;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Modifiers.Game;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Roles.Apocalypse;

[HarmonyPatch]
public static class ApocalypseGuessBlockPatch
{
    public static IEnumerable<System.Reflection.MethodBase> TargetMethods()
    {
        var assassinClick = AccessTools.Method(typeof(AssassinModifier), "ClickGuess");
        if (assassinClick != null) yield return assassinClick;

        var doomsayerClick = AccessTools.Method(typeof(DoomsayerRole), "ClickGuess");
        if (doomsayerClick != null) yield return doomsayerClick;

        var vigilanteClick = AccessTools.Method(typeof(VigilanteRole), "ClickGuess");
        if (vigilanteClick != null) yield return vigilanteClick;
    }

    public static bool Prefix(object __instance, PlayerVoteArea voteArea)
    {
        var guesser = GetGuesser(__instance);
        var target = voteArea == null ? null : MiscUtils.PlayerById(voteArea.TargetPlayerId);

        return !ApocalypseUtils.AreAllied(guesser, target);
    }

    private static PlayerControl? GetGuesser(object instance)
    {
        return instance switch
        {
            AssassinModifier assassin => assassin.Player,
            DoomsayerRole doomsayer => doomsayer.Player,
            VigilanteRole vigilante => vigilante.Player,
            _ => null
        };
    }
}
