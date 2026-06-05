using MiraAPI.Events;
using MiraAPI.Events.Mira;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.Modifiers;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Utilities;
using System.Collections;
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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TouMegaChujoweExtension.Events.Neutral;

public static class BakerEvents
{
    public static readonly HashSet<byte> PendingStarvationDeaths = [];
    public static readonly HashSet<byte> ShownStarvationAnimations = [];

    [RegisterEvent(10000)]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (!@event.TriggeredByIntro)
        {
            return;
        }

        PendingStarvationDeaths.Clear();
        ShownStarvationAnimations.Clear();
        BakerRole.PendingFamineAnnouncement = false;
        BakerRole.FamineAnnounced = false;

        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        var chance = OptionGroupSingleton<BakerOptions>.Instance.InstantFamineChance;
        if (chance <= 0f)
        {
            return;
        }

        if (UnityEngine.Random.Range(0f, 100f) >= chance)
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

        if (victim.HasModifier<FamineStarveRevealModifier>())
        {
            victim.RemoveModifier<FamineStarveRevealModifier>();
        }

        TryUnlockFamine();
    }

    [RegisterEvent]
    public static void MiraButtonClickEventHandler(MiraButtonClickEvent @event)
    {
        var button = @event.Button as CustomActionButton<PlayerControl>;
        var source = PlayerControl.LocalPlayer;
        var target = button?.Target;

        if (target == null || button == null || !button.CanClick()) return;
        if (source == null) return;
        if (target.PlayerId == source.PlayerId) return;
        if (MeetingHud.Instance || ExileController.Instance) return;

        if (target.Data?.Role is FamineRole && !source.HasModifier<TownOfUs.Modifiers.IgnoreInvulnerabilityModifier>())
        {
            @event.Cancel();

            Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(Color.white, 0.15f, 0.15f));

            button.SetTimer(button.Cooldown);
            source.SetKillTimer(source.GetKillCooldown());
        }
    }

    [RegisterEvent]
    public static void StartMeetingEventHandler(StartMeetingEvent _)
    {
        if (MeetingHud.Instance == null)
        {
            return;
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

                Reactor.Utilities.Coroutines.Start(CoClearStarveMarkersAfterDeath(target));
            }

            // 4. If Famine is active and all bread targets are gone, unlock unrestricted starving.
            TryUnlockFamine();
        }

        BakerRole.ShowPendingFamineAnnouncement();
    }

    [RegisterEvent(100)]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        var target = @event.Target;
        var source = @event.Source;
        if (target == null || source == null)
        {
            return;
        }

        if (target.Data.Role is FamineRole && !source.HasModifier<TownOfUs.Modifiers.IgnoreInvulnerabilityModifier>())
        {
            @event.Cancel();

            if (PlayerControl.LocalPlayer != null && (PlayerControl.LocalPlayer == target || PlayerControl.LocalPlayer == source))
            {
                Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(Color.white, 0.15f, 0.15f));
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
    public static void OnMeetingEnd(EndMeetingEvent _)
    {
        // Reset buttons

        var giveButton = MiraAPI.Hud.CustomButtonSingleton<BakerGiveButton>.Instance;
        if (giveButton != null)
        {
            giveButton.Timer = giveButton.Cooldown;
        }
    }

    public static void TryUnlockFamine()
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
                      x.HasModifier<BakerBreadModifier>());
        if (!anyBreadsAlive)
        {
            FamineRole.RpcUnlockFamine(activeFamine);
        }
    }

    public static void TryShowStarvationAnimation(PlayerControl target)
    {
        if (target == null ||
            !target.AmOwner ||
            ShownStarvationAnimations.Contains(target.PlayerId))
        {
            return;
        }

        PendingStarvationDeaths.Add(target.PlayerId);
        Reactor.Utilities.Coroutines.Start(CoTryShowStarvationAnimation(target));
    }

    private static IEnumerator CoTryShowStarvationAnimation(PlayerControl target)
    {
        var timer = 0f;
        while (target != null &&
               (!target.HasDied() ||
                !HudManager.InstanceExists ||
                HudManager.Instance.KillOverlay == null ||
                target.Data == null) &&
               timer < 3f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (target == null ||
            !target.AmOwner ||
            !HudManager.InstanceExists ||
            HudManager.Instance.KillOverlay == null ||
            target.Data == null ||
            !ShownStarvationAnimations.Add(target.PlayerId))
        {
            yield break;
        }

        PendingStarvationDeaths.Remove(target.PlayerId);
        SoundManager.Instance.PlaySound(target.KillSfx, false, 0.8f);
        HudManager.Instance.KillOverlay.ShowKillAnimation(target.Data, target.Data);
    }

    private static IEnumerator CoClearStarveMarkersAfterDeath(PlayerControl target)
    {
        yield return new WaitForSeconds(0.75f);

        if (target == null)
        {
            yield break;
        }

        if (target.HasModifier<FamineStarvedModifier>())
        {
            target.RemoveModifier<FamineStarvedModifier>();
        }

        if (target.HasModifier<BakerBreadRevealModifier>())
        {
            target.RemoveModifier<BakerBreadRevealModifier>();
        }

        if (target.HasModifier<FamineStarveRevealModifier>())
        {
            target.RemoveModifier<FamineStarveRevealModifier>();
        }
    }
}
