using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using TouMegaChujoweExtension.Modifiers.Crewmate;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TownOfUs.Utilities;
using TownOfUs.Buttons;
using UnityEngine;
using System;
using System.Linq;
using TouMegaChujoweExtension;
using MiraAPI.Modifiers;

namespace TouMegaChujoweExtension.Events.Crewmate;

public static class TavernKeeperEvents
{
    [RegisterEvent]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        try
        {
            if (@event.Source == null || @event.Target == null || MeetingHud.Instance != null || ExileController.Instance != null) return;

            if (@event.Target.HasModifier<RoleblockedModifier>())
            {
                @event.Cancel();

                if (@event.Source.AmOwner)
                {
                    @event.Source.SetKillTimer(@event.Source.GetKillCooldown());

                    if (HudManager.Instance != null && HudManager.Instance.KillButton != null)
                    {
                        HudManager.Instance.KillButton.SetTarget(null);
                    }

                    foreach (var button in MiraAPI.Hud.CustomButtonManager.Buttons)
                    {
                        if (button == null || !button.Enabled(@event.Source.Data.Role) || button is not IKillButton) continue;
                        button.Timer = button.Cooldown;
                    }

                    Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.TavernKeeper, alpha: 0.5f));
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[TOUMCE] Exception in TavernKeeper BeforeMurderEventHandler: {ex}");
        }
    }
}
