using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Modifiers;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Buttons.Classic.Neutral;
using TownOfUs.Utilities;
using TownOfUs.Extensions;
using TownOfUs.Roles.Neutral;
using System.Linq;
using UnityEngine;

namespace TouMegaChujoweExtension.Events.Neutral;

public static class BakerEvents
{
    [RegisterEvent]
    public static void StartMeetingEventHandler(StartMeetingEvent @event)
    {
        if (MeetingHud.Instance == null)
        {
            return;
        }

        // 1. Reset Baker's bread given flags
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null && player.Data != null && player.Data.Role is BakerRole bakerRole)
            {
                bakerRole.BreadGivenThisRound = false;
            }
        }

        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            // 2. Check for Baker transformation into Famine (host-side)
            var baker = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(x => x != null && !x.HasDied() && x.Data.Role is BakerRole);
            if (baker != null)
            {
                var breadGivenCount = PlayerControl.AllPlayerControls.ToArray()
                    .Count(x => x != null && !x.HasDied() && x.HasModifier<BakerBreadModifier>());
                var breadNeeded = (int)OptionGroupSingleton<BakerOptions>.Instance.BreadNeeded;
                if (breadGivenCount >= breadNeeded)
                {
                    BakerRole.RpcTransformToFamine(baker);
                }
            }

            // 3. Process starvation kills if host
            foreach (var target in PlayerControl.AllPlayerControls)
            {
                if (target == null || target.HasDied() || !target.HasModifier<FamineStarvedModifier>())
                {
                    continue;
                }

                var faminePlayer = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(x => x != null && !x.HasDied() && x.Data.Role is FamineRole);
                if (faminePlayer != null)
                {
                    faminePlayer.RpcSpecialMurder(target, isIndirect: false, ignoreShield: false, didSucceed: true, resetKillTimer: false, createDeadBody: false, teleportMurderer: false, showKillAnim: false, playKillSound: false, causeOfDeath: "Starved");
                }
                else
                {
                    target.RpcSpecialMurder(target, isIndirect: false, ignoreShield: false, didSucceed: true, resetKillTimer: false, createDeadBody: false, teleportMurderer: false, showKillAnim: false, playKillSound: false, causeOfDeath: "Starved");
                }

                target.RemoveModifier<FamineStarvedModifier>();
            }

            // 4. If Famine is active, check if all targets who had bread are dead. If so, starve everyone else.
            var activeFamine = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(x => x != null && !x.HasDied() && x.Data.Role is FamineRole);
            if (activeFamine != null)
            {
                bool anyBreadsAlive = PlayerControl.AllPlayerControls.ToArray()
                    .Any(x => x != null && !x.HasDied() && x != activeFamine && (x.HasModifier<BakerBreadModifier>() || x.HasModifier<FamineStarvedModifier>()));
                if (!anyBreadsAlive)
                {
                    foreach (var player in PlayerControl.AllPlayerControls)
                    {
                        if (player == null || player.HasDied() || player == activeFamine || player.Data.Role is FamineRole || player.Data.Role is PestilenceRole)
                        {
                            continue;
                        }
                        activeFamine.RpcSpecialMurder(player, isIndirect: false, ignoreShield: false, didSucceed: true, resetKillTimer: false, createDeadBody: false, teleportMurderer: false, showKillAnim: false, playKillSound: false, causeOfDeath: "Starved");
                    }
                }
            }
        }
    }

    [RegisterEvent]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        var target = @event.Target;
        var source = @event.Source;
        if (target == null || source == null)
        {
            return;
        }

        if (target.Data.Role is FamineRole)
        {
            if (PlayerControl.LocalPlayer != null && (PlayerControl.LocalPlayer == target || PlayerControl.LocalPlayer == source))
            {
                Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(Color.white, 0.15f, 0.15f));
            }
        }
    }

    [RegisterEvent]
    public static void OnMeetingEnd(EndMeetingEvent @event)
    {
        // Reset bread given flags and buttons
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null && player.Data != null && player.Data.Role is BakerRole bakerRole)
            {
                bakerRole.BreadGivenThisRound = false;
            }
        }

        var giveButton = MiraAPI.Hud.CustomButtonSingleton<BakerGiveButton>.Instance;
        if (giveButton != null)
        {
            giveButton.Timer = giveButton.Cooldown;
        }
    }
}
