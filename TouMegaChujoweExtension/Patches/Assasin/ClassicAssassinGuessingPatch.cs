using System.Reflection;
using HarmonyLib;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Modifiers.Game;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles.Neutral;

namespace TouMegaChujoweExtension.Patches.Assassin;

[HarmonyPatch(typeof(AssassinModifier), nameof(AssassinModifier.OnMeetingStart))]
public static class BlockMiraAssassinButtonsPatch
{
    public static bool Prefix(AssassinModifier __instance)
    {
        if (!ClassicAssassinSystem.IsActive)
            return true;

        if (__instance.Player.AmOwner && MeetingHud.Instance != null)
        {
            ClassicAssassinSystem.GenerateButtons(MeetingHud.Instance, __instance);
        }

        return false;
    }
}

[HarmonyPatch(typeof(VigilanteRole), nameof(VigilanteRole.OnMeetingStart))]
public static class BlockMiraVigilanteButtonsPatch
{
    public static bool Prefix(VigilanteRole __instance)
    {
        if (!ClassicAssassinSystem.IsActive)
            return true;

        if (__instance.Player.AmOwner && MeetingHud.Instance != null)
        {
            ClassicAssassinSystem.GenerateButtons(MeetingHud.Instance, __instance);
        }

        return false;
    }
}

[HarmonyPatch(typeof(DoomsayerRole), nameof(DoomsayerRole.OnMeetingStart))]
public static class BlockMiraDoomsayerButtonsPatch
{
    public static bool Prefix(DoomsayerRole __instance)
    {
        if (!ClassicAssassinSystem.IsActive)
            return true;

        if (__instance.Player.AmOwner && MeetingHud.Instance != null)
        {
            var generateReport = typeof(DoomsayerRole).GetMethod("GenerateReport",
                BindingFlags.Instance | BindingFlags.NonPublic);

            generateReport?.Invoke(__instance, null);

            ClassicAssassinSystem.GenerateButtons(MeetingHud.Instance, __instance);
        }

        return false;
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.VotingComplete))]
public static class ClassicAssassinVotingCompletePatch
{
    public static void Postfix()
    {
        if (!ClassicAssassinSystem.IsActive) return;
        ClassicAssassinSystem.HideAllButtons();
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
public static class ClassicAssassinMeetingClosePatch
{
    public static void Postfix()
    {
        if (!ClassicAssassinSystem.IsActive) return;
        ClassicAssassinSystem.Reset();
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
public static class ClassicAssassinGameEndPatch
{
    public static void Postfix()
    {
        ClassicAssassinSystem.FullReset();
    }
}

[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
public static class ClassicAssassinGameStartPatch
{
    public static void Postfix()
    {
        ClassicAssassinSystem.FullReset();
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnDisconnected))]
public static class ClassicAssassinDisconnectedPatch
{
    public static void Postfix()
    {
        ClassicAssassinSystem.FullReset();
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
public static class ClassicAssassinShipStartPatch
{
    public static void Postfix()
    {
        ClassicAssassinSystem.FullReset();
    }
}
