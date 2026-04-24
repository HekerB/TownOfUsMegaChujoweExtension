using MiraAPI.Events;
using MiraAPI.Events.Mira;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Buttons;
using TownOfUs.Modifiers;
using TownOfUs.Options;
using TownOfUs.Utilities;
using Reactor.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Events;

public static class BodyguardEvents
{
    [RegisterEvent]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        var source = @event.Source;
        var target = @event.Target;

        if (source == null || target == null)
        {
            return;
        }

        var hasShield = target.HasModifier<BodyguardShieldModifier>();
        var isCancelled = @event.IsCancelled;
        Info($"[BG-Event] BeforeMurder: {source?.Data?.PlayerName} → {target?.Data?.PlayerName}, hasShield={hasShield}, alreadyCancelled={isCancelled}");

        if (isCancelled)
        {
            Info("[BG-Event] Event already cancelled by something else, skipping");
            return;
        }

        if (CheckForBodyguardShield(@event, source, target))
        {
            Info("[BG-Event] Shield BLOCKED attack!");
            ResetButtonTimer(source);
        }
    }

    [RegisterEvent]
    public static void MiraButtonClickEventHandler(MiraButtonClickEvent @event)
    {
        var source = PlayerControl.LocalPlayer;
        var button = @event.Button as CustomActionButton<PlayerControl>;
        var target = button?.Target;

        if (source == null || button == null || target == null)
        {
            return;
        }

        if (!button.CanClick())
        {
            return;
        }

        if (!ShouldCheckButtonForShield(button, target))
        {
            return;
        }

        var hasShield = target.HasModifier<BodyguardShieldModifier>();
        Info($"[BG-Event] MiraButtonClick: {source?.Data?.PlayerName} → {target?.Data?.PlayerName}, button={button.GetType().Name}, hasShield={hasShield}");

        if (CheckForBodyguardShield(@event, source, target))
        {
            Info("[BG-Event] Shield BLOCKED button attack!");
            ResetButtonTimer(source, button);
        }
    }

    private static bool ShouldCheckButtonForShield(CustomActionButton<PlayerControl> button, PlayerControl target)
    {
        if (target == null)
        {
            return false;
        }

        if (!target.HasModifier<BodyguardShieldModifier>())
        {
            return false;
        }

        // Vanilla / standard kill buttons
        if (button is IKillButton)
        {
            return true;
        }

        // Fallback for custom offensive buttons that target players but may not implement IKillButton
        var buttonName = button.GetType().Name;

        if (buttonName.Contains("Kill", StringComparison.OrdinalIgnoreCase) ||
            buttonName.Contains("Murder", StringComparison.OrdinalIgnoreCase) ||
            buttonName.Contains("Spell", StringComparison.OrdinalIgnoreCase) ||
            buttonName.Contains("Attack", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool CheckForBodyguardShield(MiraCancelableEvent @event, PlayerControl source, PlayerControl target)
    {
        if (MeetingHud.Instance || ExileController.Instance)
        {
            Info("[BG-Event] In meeting/exile, skipping");
            return false;
        }

        if (!target.HasModifier<BodyguardShieldModifier>())
        {
            Info("[BG-Event] No BodyguardShieldModifier on target");
            return false;
        }

        if (target.PlayerId == source.PlayerId)
        {
            Info("[BG-Event] Self-attack, skipping");
            return false;
        }

        @event.Cancel();
        Info("[BG-Event] Event CANCELLED");

        var shieldMod = target.GetModifier<BodyguardShieldModifier>();
        var bodyguard = shieldMod?.Bodyguard;

        Info($"[BG-Event] bodyguard={bodyguard?.Data?.PlayerName}, TutorialExists={TutorialManager.InstanceExists}, sourceAmOwner={source.AmOwner}");

        if (bodyguard != null && PelicanSystem.IsSwallowed(bodyguard.PlayerId))
        {
            Info("[BG-Event] Bodyguard is swallowed by Pelican - shield blocks but NO backlash");
            if (source.AmOwner)
            {
                Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Bodyguard));
            }
            return true;
        }

        if (bodyguard != null && (TutorialManager.InstanceExists || source.AmOwner))
        {
            Info("[BG-Event] Sending RpcBodyguardShieldAttacked");
            BodyguardRole.RpcBodyguardShieldAttacked(bodyguard, source, target);
        }

        if (source.AmOwner)
        {
            Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Bodyguard));
        }
        else
        {
            Warning($"[BG-Event] RPC NOT SENT! bodyguard={bodyguard != null}, tutorial={TutorialManager.InstanceExists}, srcOwner={source.AmOwner}");
        }

        return true;
    }

    private static void ResetButtonTimer(PlayerControl source, CustomActionButton<PlayerControl>? button = null)
    {
        var reset = OptionGroupSingleton<GeneralOptions>.Instance.TempSaveCdReset;
        button?.SetTimer(reset);

        if (source.AmOwner && source.IsImpostor())
        {
            source.SetKillTimer(reset);
        }
    }

    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        foreach (var bg in CustomRoleUtils.GetActiveRolesOfType<BodyguardRole>())
        {
            bg.BacklashReady = false;
            bg.KillModeActive = false;
            bg.LastAttacker = null;
            bg.MarkedAttackerDot = false;
        }
    }

    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        var victim = @event.Target;

        foreach (var bg in CustomRoleUtils.GetActiveRolesOfType<BodyguardRole>())
        {
            if (victim == bg.Guarded || victim == bg.Player)
            {
                bg.Clear();
            }
        }
    }

    [RegisterEvent]
    public static void EjectionEventHandler(EjectionEvent @event)
    {
        var exiled = @event.ExileController?.initData?.networkedPlayer?.Object;
        if (exiled == null) return;

        foreach (var bg in CustomRoleUtils.GetActiveRolesOfType<BodyguardRole>())
        {
            if (exiled == bg.Guarded || exiled == bg.Player)
            {
                bg.Clear();
            }
        }
    }

    [RegisterEvent]
    public static void PlayerLeaveEventHandler(PlayerLeaveEvent @event)
    {
        var player = @event.ClientData.Character;
        if (!player) return;

        foreach (var bg in CustomRoleUtils.GetActiveRolesOfType<BodyguardRole>())
        {
            if (player == bg.Guarded || player == bg.Player)
            {
                bg.Clear();
            }
        }
    }
}