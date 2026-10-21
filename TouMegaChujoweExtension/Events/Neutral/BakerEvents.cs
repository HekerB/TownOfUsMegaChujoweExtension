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

    // Bakers/Famines whose bread changed this frame and still need the host's recheck.
    private static readonly HashSet<byte> DirtyBreadOwners = [];
    private static bool _reevaluationRunning;

    [RegisterEvent(10000)]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (!@event.TriggeredByIntro)
        {
            return;
        }

        PendingStarvationDeaths.Clear();
        ShownStarvationAnimations.Clear();
        DirtyBreadOwners.Clear();
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

        // The host rechecks every Famine that just lost a bread holder - whoever the host is playing.
        if (victim != null)
        {
            foreach (var bread in victim.GetModifiers<BakerBreadModifier>())
            {
                QueueBreadReevaluation(bread.BakerId);
            }
        }

        var hasBakerMark = victim != null && localPlayer != null &&
                            (BakerBreadModifier.HasBreadFrom(victim, localPlayer.PlayerId) ||
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
        RoleAlertUtils.ShowRoleAlert(
            TouLocale.Get("ExtensionRoleBakerBreadTargetDiedNotif", "{0} died with your bread!")
                .Replace("{0}", victimName),
            Color.white,
            TouExtensionIcons.BakerRoleIcon.LoadAsset());

        if (victim.HasModifier<BakerBreadRevealModifier>())
        {
            victim.RemoveModifier<BakerBreadRevealModifier>();
        }

        if (victim.HasModifier<FamineStarveRevealModifier>())
        {
            victim.RemoveModifier<FamineStarveRevealModifier>();
        }
    }

    [RegisterEvent]
    public static void MiraButtonClickEventHandler(MiraButtonClickEvent @event)
    {
        // Famine only shrugs off attacks; investigating, shielding or any other targeted ability still reaches it.
        if (@event.Button is not IKillButton)
        {
            return;
        }

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
            // 2. Each Baker turns into Famine on its own bread (host-side).
            foreach (var baker in PlayerControl.AllPlayerControls.ToArray()
                         .Where(x => x != null && !x.HasDied() && x.Data?.Role is BakerRole))
            {
                var breadGivenCount = BakerBreadModifier.CountFor(baker.PlayerId);
                var breadNeeded = BakerRole.GetEffectiveBreadNeeded(baker);
                if (breadGivenCount >= breadNeeded)
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
        foreach (var famine in PlayerControl.AllPlayerControls.ToArray()
                     .Where(x => x != null && !x.HasDied() && x.Data?.Role is FamineRole))
        {
            TryUnlockFamine(famine);
        }
    }

    /// <summary>
    /// Host: a Famine whose own bread holders are all gone may starve anyone.
    /// </summary>
    public static void TryUnlockFamine(PlayerControl famine)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        if (famine == null || famine.HasDied() ||
            famine.Data?.Role is not FamineRole famineRole ||
            famineRole.CanStarveAnyone ||
            !famineRole.HadBreadTargets)
        {
            return;
        }

        if (BakerBreadModifier.CountFor(famine.PlayerId) == 0)
        {
            FamineRole.RpcUnlockFamine(famine);
        }
    }

    /// <summary>
    /// Host: recheck this Baker's Famine once the change that triggered it is visible. MiraAPI runs a modifier's
    /// OnDeactivate before the modifier leaves the player, and a death before it is reported everywhere, so the
    /// check waits a frame; every change of that frame is checked in one pass.
    /// </summary>
    public static void QueueBreadReevaluation(byte ownerId)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        DirtyBreadOwners.Add(ownerId);
        if (!_reevaluationRunning)
        {
            _reevaluationRunning = true;
            Reactor.Utilities.Coroutines.Start(CoReevaluateBreadOwners());
        }
    }

    private static IEnumerator CoReevaluateBreadOwners()
    {
        yield return null;
        yield return new WaitForFixedUpdate();

        try
        {
            foreach (var ownerId in DirtyBreadOwners.ToArray())
            {
                var owner = MiscUtils.PlayerById(ownerId);
                if (owner != null && owner.Data?.Role is FamineRole)
                {
                    TryUnlockFamine(owner);
                }
            }
        }
        finally
        {
            DirtyBreadOwners.Clear();
            _reevaluationRunning = false;
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
