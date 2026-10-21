using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Roles.Icenberg;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CanMove), MethodType.Getter)]
public static class IcenbergFreezeCanMovePatch
{
    [HarmonyPostfix]
    public static void Postfix(PlayerControl __instance, ref bool __result)
    {
        if (__result && __instance != null && __instance.HasModifier<IcenbergFrozenModifier>())
        {
            __result = false;
        }
    }
}

[HarmonyPatch(typeof(LogicOptions), nameof(LogicOptions.GetPlayerSpeedMod))]
public static class IcenbergBlizzardSpeedPatch
{
    [HarmonyPostfix]
    public static void Postfix(PlayerControl pc, ref float __result)
    {
        if (pc != null && pc.HasModifier<IcenbergBlizzardModifier>())
        {
            __result *= IcenbergBlizzardModifier.SpeedMultiplier;
        }
    }
}

public static class IcenbergEvents
{
    // A game can end, or the local player can leave, with an effect still on screen.
    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (@event.TriggeredByIntro)
        {
            IcenbergOverlay.Clear();
        }
    }

    [RegisterEvent]
    public static void StartMeetingEventHandler(StartMeetingEvent @event)
    {
        _ = @event;
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.HasDied())
        {
            IcenbergOverlay.Clear();
        }
    }
}
