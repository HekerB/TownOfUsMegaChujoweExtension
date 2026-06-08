using System.Linq;
using MiraAPI.Events;
using MiraAPI.Events.Mira;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using Reactor.Utilities;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Events.Neutral;

public static class SoulCollectorEvents
{
    [RegisterEvent(10000)]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (@event.TriggeredByIntro)
        {
            SoulCollectorSystem.Clear();
            SoulCollectorRole.PendingDeathAnnouncement = false;
            SoulCollectorRole.DeathAnnounced = false;
        }

        if (!@event.TriggeredByIntro) return;
        if (AmongUsClient.Instance != null && !AmongUsClient.Instance.AmHost) return;

        var chance = OptionGroupSingleton<SoulCollectorOptions>.Instance.InstantDeathChance;
        if (chance <= 0f || UnityEngine.Random.Range(0f, 100f) >= chance)
        {
            return;
        }

        foreach (var soulCollector in PlayerControl.AllPlayerControls.ToArray()
                     .Where(x => x != null && !x.HasDied() && x.Data?.Role is SoulCollectorRole))
        {
            SoulCollectorRole.RpcTransformToDeath(soulCollector);
        }
    }

    [RegisterEvent]
    public static void StartMeetingEventHandler(StartMeetingEvent _)
    {
        if (AmongUsClient.Instance == null || AmongUsClient.Instance.AmHost)
        {
            RemoveExpiredMarks();
            GrantPassiveSouls();
        }

        SoulCollectorRole.ShowPendingDeathAnnouncement();
    }

    [RegisterEvent]
    public static void PlayerDeathEventHandler(PlayerDeathEvent @event)
    {
        var victim = @event.Player;
        if (victim == null || !victim.TryGetModifier<SoulReapedModifier>(out var reapedModifier))
        {
            return;
        }

        var soulCollector = MiscUtils.PlayerById(reapedModifier.SoulCollectorId);
        if (soulCollector == null ||
            soulCollector.HasDied() ||
            soulCollector.Data?.Role is not SoulCollectorRole soulCollectorRole)
        {
            return;
        }

        if (soulCollector.AmOwner)
        {
            ShowSoulCollectedFeedback(victim);
        }

        if (AmongUsClient.Instance == null || AmongUsClient.Instance.AmHost || soulCollector.AmOwner)
        {
            SoulCollectorRole.RpcSetSouls(soulCollector, soulCollectorRole.SoulsCollected + 1);
            victim.RemoveModifier<SoulReapedModifier>();
            TryTransformIfReady(soulCollector);
        }
    }

    private static void GrantPassiveSouls()
    {
        var passiveSouls = (int)OptionGroupSingleton<SoulCollectorOptions>.Instance.PassiveSoulsPerMeeting;
        if (passiveSouls <= 0)
        {
            return;
        }

        foreach (var soulCollector in PlayerControl.AllPlayerControls.ToArray()
                     .Where(x => x != null && !x.HasDied() && x.Data?.Role is SoulCollectorRole))
        {
            var role = soulCollector.GetRole<SoulCollectorRole>();
            if (role == null)
            {
                continue;
            }

            SoulCollectorRole.RpcSetSouls(soulCollector, role.SoulsCollected + passiveSouls);
            TryTransformIfReady(soulCollector);
        }
    }

    private static void RemoveExpiredMarks()
    {
        foreach (var player in PlayerControl.AllPlayerControls.ToArray())
        {
            if (player == null || !player.TryGetModifier<SoulReapedModifier>(out var modifier))
            {
                continue;
            }

            if (modifier.IsExpired())
            {
                player.RemoveModifier<SoulReapedModifier>();
            }
        }
    }

    private static void TryTransformIfReady(PlayerControl soulCollector)
    {
        if (soulCollector == null ||
            soulCollector.HasDied() ||
            soulCollector.Data?.Role is not SoulCollectorRole soulCollectorRole)
        {
            return;
        }

        var soulsNeeded = (int)OptionGroupSingleton<SoulCollectorOptions>.Instance.SoulGoal;
        if (soulsNeeded > 0 && soulCollectorRole.SoulsCollected >= soulsNeeded)
        {
            SoulCollectorRole.RpcTransformToDeath(soulCollector);
        }
    }

    private static void ShowSoulCollectedFeedback(PlayerControl victim)
    {
        Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.SoulCollector, 0.15f, 0.15f));
        PlayerControl.LocalPlayer.AddModifier<SoulDeathArrowModifier>(victim.transform.position);

        var victimName = $"{TouExtensionColors.SoulCollector.ToTextColor()}{victim.Data.PlayerName}</color>";
        var notif = Helpers.CreateAndShowNotification(
            TouLocale.Get("ExtensionRoleSoulCollectorMarkedDiedNotif", "{0}'s soul has been collected!")
                .Replace("{0}", victimName),
            Color.white,
            new Vector3(0f, 1f, -20f),
            spr: TouExtensionIcons.SoulCollectorRoleIcon.LoadAsset());
        notif?.AdjustNotification();
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

        if (target.Data?.Role is DeathRole && !source.HasModifier<TownOfUs.Modifiers.IgnoreInvulnerabilityModifier>())
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

        if (target.Data?.Role is DeathRole && !source.HasModifier<TownOfUs.Modifiers.IgnoreInvulnerabilityModifier>())
        {
            @event.Cancel();

            Coroutines.Start(MiscUtils.CoFlash(Color.white, 0.15f, 0.15f));

            button.SetTimer(button.Cooldown);
            source.SetKillTimer(source.GetKillCooldown());
        }
    }
}
