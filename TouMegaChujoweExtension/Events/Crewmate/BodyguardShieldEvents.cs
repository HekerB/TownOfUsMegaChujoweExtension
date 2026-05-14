using MiraAPI.Events;
using MiraAPI.Events.Mira;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using Reactor.Utilities;
using TownOfUs.Buttons;
using TownOfUs.Modifiers;
using TownOfUs.Options;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Events.Crewmate;

public static class BodyguardShieldEvents
{
    [RegisterEvent]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        var source = @event.Source;
        var target = @event.Target;

        if (source == null || target == null) return;
        if (MeetingHud.Instance || ExileController.Instance) return;

        if (!target.TryGetModifier<BodyguardShieldModifier>(out var bgMod)) return;
        if (target.PlayerId == source.PlayerId) return;
        if (source.TryGetModifier<IndirectAttackerModifier>(out var indirect) && indirect.IgnoreShield) return;

        @event.Cancel();
        Logger<TouMegaChujoweExtensionPlugin>.Info(
            $"[BodyguardShieldEvents] Murder from {source.Data.PlayerName} blocked by Bodyguard shield on {target.Data.PlayerName}");

        if (bgMod.Bodyguard != null && (TutorialManager.InstanceExists || source.AmOwner))
        {
            BodyguardRole.RpcBodyguardShieldAttacked(bgMod.Bodyguard, source, target);
        }

        if (source.AmOwner)
        {
            ResetKillButton(source);
        }
    }

    [RegisterEvent]
    public static void MiraButtonClickEventHandler(MiraButtonClickEvent @event)
    {
        var button = @event.Button as CustomActionButton<PlayerControl>;
        var source = PlayerControl.LocalPlayer;
        var target = button?.Target;

        if (target == null || button is not IKillButton || !button.CanClick()) return;
        if (source == null) return;
        if (MeetingHud.Instance || ExileController.Instance) return;

        if (!target.TryGetModifier<BodyguardShieldModifier>(out var bgMod)) return;
        if (target.PlayerId == source.PlayerId) return;
        if (source.TryGetModifier<IndirectAttackerModifier>(out var indirect) && indirect.IgnoreShield) return;

        @event.Cancel();
        Logger<TouMegaChujoweExtensionPlugin>.Info(
            $"[BodyguardShieldEvents] Button click from {source.Data.PlayerName} blocked by Bodyguard shield on {target.Data.PlayerName}");

        if (bgMod.Bodyguard != null)
        {
            BodyguardRole.RpcBodyguardShieldAttacked(bgMod.Bodyguard, source, target);
        }

        ResetKillButton(source, button);
    }

    private static void ResetKillButton(PlayerControl source, CustomActionButton<PlayerControl>? button = null)
    {
        var duration = 10f;

        button?.SetTimer(duration);
        source.SetKillTimer(duration);
    }
}
