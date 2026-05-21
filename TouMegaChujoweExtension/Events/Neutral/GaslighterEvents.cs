using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.Modifiers;
using MiraAPI.Events.Mira;
using TouMegaChujoweExtension.Roles.Neutral;
using TouMegaChujoweExtension.Modifiers.Neutral;
using MiraAPI.Hud;
using MiraAPI.GameOptions;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Modifiers;
using TownOfUs.Utilities;
using TownOfUs.Modules.Localization;
using Reactor.Utilities;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TouMegaChujoweExtension.Events.Neutral;

public static class GaslighterEvents
{
    [RegisterEvent]
    public static void OnMeetingEnd(EndMeetingEvent @event)
    {
        if (PlayerControl.AllPlayerControls == null) return;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player?.Data?.Role is GaslighterRole gaslighterRole)
            {
                try
                {
                    gaslighterRole.OnMeetingEnd();
                }
                catch
                {
                    // Ignore errors during meeting cleanup
                }
            }
        }
    }

    [RegisterEvent]
    public static void StartMeetingEventHandler(StartMeetingEvent @event)
    {
        if (MeetingHud.Instance == null)
        {
            return;
        }

        var cursedPlayers = new List<PlayerControl>();
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied() || !player.HasModifier<GaslighterCursedModifier>())
            {
                continue;
            }

            cursedPlayers.Add(player);
        }

        if (cursedPlayers.Count > 0)
        {
            var witchColor = ColorUtility.ToHtmlStringRGBA(TouExtensionColors.Witch);
            var title = $"<color=#{witchColor}>{TouLocale.Get("ExtensionRoleWitch", "Witch")}</color>";

            string message;
            if (cursedPlayers.Count > 1)
            {
                var playerNames = string.Join("\n", cursedPlayers.Select(p => $"  <color=#{witchColor}>{p.Data.PlayerName}</color>: They will die after this meeting"));
                var baseMessage = TouLocale.GetParsed("ExtensionWitchSpellNotificationMultiple",
                    "Multiple players have been cursed:\n<players>\nVote out or kill the Witch to save them!");
                message = baseMessage.Replace("\\n", "\n").Replace("&lt;players&gt;", playerNames).Replace("<players>", playerNames);
            }
            else
            {
                var baseMessage = TouLocale.GetParsed("ExtensionWitchSpellNotification",
                    "<player> has been cursed! They have <meetings> meeting(s) left. Vote out or kill the Witch to save them!");
                message = baseMessage.Replace("\\n", "\n")
                    .Replace("&lt;player&gt;", $"<color=#{witchColor}>{cursedPlayers[0].Data.PlayerName}</color>")
                    .Replace("<player>", $"<color=#{witchColor}>{cursedPlayers[0].Data.PlayerName}</color>")
                    .Replace("They have <meetings> meeting(s) left", "They will die after this meeting")
                    .Replace("&lt;meetings&gt; meeting(s) left", "They will die after this meeting");
            }

            MiscUtils.AddFakeChat(PlayerControl.LocalPlayer.Data, title, message, false, true);
        }
    }

    [RegisterEvent]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        var source = @event.Source;
        var target = @event.Target;

        if (CheckForGaslighterShield(@event, source, target))
        {
            ResetButtonTimer(source);
        }
    }

    [RegisterEvent]
    public static void MiraButtonClickEventHandler(MiraButtonClickEvent @event)
    {
        var source = PlayerControl.LocalPlayer;
        var button = @event.Button as CustomActionButton<PlayerControl>;
        var target = button?.Target;
        if (target == null || button is not IKillButton || !button.CanClick())
        {
            return;
        }

        if (CheckForGaslighterShield(@event, source, target))
        {
            ResetButtonTimer(source, button);
        }
    }

    private static bool CheckForGaslighterShield(MiraCancelableEvent @event, PlayerControl source, PlayerControl target)
    {
        if (MeetingHud.Instance || ExileController.Instance)
        {
            return false;
        }

        if (!target.HasModifier<GaslighterShieldModifier>() ||
            target.PlayerId == source.PlayerId ||
            (source.TryGetModifier<IndirectAttackerModifier>(out var indirect) && indirect.IgnoreShield))
        {
            return false;
        }

        @event.Cancel();

        // Visual flash for the local attacker
        if (source.AmOwner)
        {
            Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.ShieldFlashes.Medic));
        }

        return true;
    }

    private static void ResetButtonTimer(PlayerControl source, CustomActionButton<PlayerControl>? button = null)
    {
        var reset = OptionGroupSingleton<TownOfUs.Options.GeneralOptions>.Instance.TempSaveCdReset;

        button?.SetTimer(reset);

        if (!source.AmOwner || !source.IsImpostor())
        {
            return;
        }

        source.SetKillTimer(reset);
    }
}
