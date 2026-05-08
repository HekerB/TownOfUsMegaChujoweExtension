using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Usables;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers;
using TownOfUs.Patches;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Injector;

public static class InjectorVentPatch
{
    [RegisterEvent]
    public static void PlayerCanUseEventHandler(PlayerCanUseEvent @event)
    {
        if (!@event.IsVent)
        {
            return;
        }

        if (PlayerControl.LocalPlayer == null)
        {
            return;
        }

        if (PlayerControl.LocalPlayer.HasModifier<InjectedNoVentModifier>())
        {
            @event.Cancel();
        }
    }
}

