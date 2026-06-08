using System;
using System.Linq;
using MiraAPI.Events;
using MiraAPI.Events.Mira;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using Reactor.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Modifiers;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Events.Neutral;

public static class WarEvents
{
    [RegisterEvent(100)]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        var target = @event.Target;
        var source = @event.Source;
        if (target == null || source == null)
        {
            return;
        }

        var isWar = target.Data?.Role is WarRole || (target.Data?.Role is BerserkerRole berserker && berserker.IsWar);

        if (isWar && !source.HasModifier<IgnoreInvulnerabilityModifier>())
        {
            @event.Cancel();

            if (PlayerControl.LocalPlayer != null && (PlayerControl.LocalPlayer == target || PlayerControl.LocalPlayer == source))
            {
                Coroutines.Start(MiscUtils.CoFlash(Color.white, 0.15f, 0.15f));
            }

            if (source.AmOwner)
            {
                source.SetKillTimer(source.GetKillCooldown());

                foreach (var button in CustomButtonManager.Buttons)
                {
                    if (button != null && button.Button != null && button.Button.gameObject.activeSelf && button is IKillButton)
                    {
                        button.SetTimer(button.Cooldown);
                    }
                }
            }
        }
    }

    [RegisterEvent]
    public static void MiraButtonClickEventHandler(MiraButtonClickEvent @event)
    {
        var button = @event.Button as CustomActionButton<PlayerControl>;
        var source = PlayerControl.LocalPlayer;
        var target = button?.Target;

        if (button == null || target == null || !button.CanClick()) return;
        if (source == null) return;
        if (target.PlayerId == source.PlayerId) return;
        if (MeetingHud.Instance || ExileController.Instance) return;

        var isWar = target.Data?.Role is WarRole || (target.Data?.Role is BerserkerRole berserker && berserker.IsWar);

        if (isWar && !source.HasModifier<IgnoreInvulnerabilityModifier>())
        {
            @event.Cancel();

            Coroutines.Start(MiscUtils.CoFlash(Color.white, 0.15f, 0.15f));

            button.SetTimer(button.Cooldown);
            source.SetKillTimer(source.GetKillCooldown());
        }
    }
}
