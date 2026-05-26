using MiraAPI.Events;
using MiraAPI.Events.Mira;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.Modifiers;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Buttons.Classic.Neutral;
using TownOfUs;
using TownOfUs.Buttons;
using TownOfUs.Utilities;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Roles.Neutral;
using System.Linq;
using UnityEngine;

namespace TouMegaChujoweExtension.Events.Neutral;

public static class BakerEvents
{
    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (!@event.TriggeredByIntro ||
            AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        var chance = OptionGroupSingleton<BakerOptions>.Instance.InstantFamineChance;
        if (chance <= 0f)
        {
            return;
        }

        var hasPestilence = PlayerControl.AllPlayerControls.ToArray()
            .Any(x => x != null && !x.HasDied() && x.Data?.Role is PestilenceRole);
        if (!hasPestilence || UnityEngine.Random.Range(0f, 100f) >= chance)
        {
            return;
        }

        foreach (var baker in PlayerControl.AllPlayerControls.ToArray()
                     .Where(x => x != null && !x.HasDied() && x.Data?.Role is BakerRole))
        {
            BakerRole.RpcTransformToFamine(baker);
        }
    }

    [RegisterEvent]
    public static void PlayerDeathEventHandler(PlayerDeathEvent @event)
    {
        var victim = @event.Player;
        var localPlayer = PlayerControl.LocalPlayer;
        var hasBakerMark = victim != null &&
                            (victim.HasModifier<BakerBreadModifier>() ||
                             victim.HasModifier<FamineStarvedModifier>());
        if (victim == null ||
            localPlayer == null ||
            victim.PlayerId == localPlayer.PlayerId ||
            !hasBakerMark ||
            localPlayer.HasDied() ||
            localPlayer.Data?.Role is not BakerRole and not FamineRole)
        {
            return;
        }

        Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Baker, 0.15f, 0.15f));

        var victimName = $"{TouExtensionColors.Baker.ToTextColor()}{victim.Data.PlayerName}</color>";
        var notif = Helpers.CreateAndShowNotification(
            TouLocale.Get("ExtensionRoleBakerBreadTargetDiedNotif", "{0} died with your bread!")
                .Replace("{0}", victimName),
            Color.white,
            new Vector3(0f, 1f, -20f),
            spr: TouExtensionIcons.BakerRoleIcon.LoadAsset());
        notif?.AdjustNotification();

        if (victim.HasModifier<BakerBreadRevealModifier>())
        {
            victim.RemoveModifier<BakerBreadRevealModifier>();
        }

        TryUnlockFamine();
    }

    [RegisterEvent]
    public static void MiraButtonClickEventHandler(MiraButtonClickEvent @event)
    {
        var button = @event.Button as CustomActionButton<PlayerControl>;
        var source = PlayerControl.LocalPlayer;
        var target = button?.Target;
        if (button == null ||
            source == null ||
            target == null ||
            source == target ||
            target.Data?.Role is not FamineRole)
        {
            return;
        }

        Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(Color.white, 0.15f, 0.15f));
    }

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
                var breadNeeded = BakerRole.GetEffectiveBreadNeeded(baker);
                if (breadNeeded > 0 && breadGivenCount >= breadNeeded)
                {
                    BakerRole.RpcTransformToFamine(baker);
                }
            }

            // 3. Process starvation kills if host
            var starvationTargets = PlayerControl.AllPlayerControls.ToArray()
                .Where(x => x != null && !x.HasDied() && x.HasModifier<FamineStarvedModifier>())
                .ToArray();
            foreach (var target in starvationTargets)
            {
                if (target == null || target.HasDied())
                {
                    continue;
                }

                var faminePlayer = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(x => x != null && !x.HasDied() && x.Data.Role is FamineRole);
                if (faminePlayer != null)
                {
                    faminePlayer.RpcSpecialMurder(target, isIndirect: false, ignoreShield: false, didSucceed: true, resetKillTimer: false, createDeadBody: false, teleportMurderer: false, showKillAnim: false, playKillSound: false, causeOfDeath: FamineRole.StarvedDeathReason);
                }
                else
                {
                    target.RpcSpecialMurder(target, isIndirect: false, ignoreShield: false, didSucceed: true, resetKillTimer: false, createDeadBody: false, teleportMurderer: false, showKillAnim: false, playKillSound: false, causeOfDeath: FamineRole.StarvedDeathReason);
                }

                target.RemoveModifier<FamineStarvedModifier>();
                target.RemoveModifier<BakerBreadRevealModifier>();
            }

            // 4. If Famine is active and all bread targets are gone, unlock unrestricted starving.
            TryUnlockFamine();
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

    private static void TryUnlockFamine()
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        var activeFamine = PlayerControl.AllPlayerControls.ToArray()
            .FirstOrDefault(x => x != null && !x.HasDied() && x.Data?.Role is FamineRole);
        if (activeFamine == null ||
            activeFamine.Data?.Role is not FamineRole famineRole ||
            famineRole.CanStarveAnyone ||
            !famineRole.HadBreadTargets)
        {
            return;
        }

        var anyBreadsAlive = PlayerControl.AllPlayerControls.ToArray()
            .Any(x => x != null &&
                      !x.HasDied() &&
                      x != activeFamine &&
                      (x.HasModifier<BakerBreadModifier>() || x.HasModifier<FamineStarvedModifier>()));
        if (!anyBreadsAlive)
        {
            FamineRole.RpcUnlockFamine(activeFamine);
        }
    }
}
