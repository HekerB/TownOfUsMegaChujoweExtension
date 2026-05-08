using MiraAPI.Events;
using MiraAPI.Events.Mira;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Buttons.Neutral;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Patches.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Events.Neutral;

public static class ShroudEvents
{
    [RegisterEvent(1)]
    public static void MiraButtonClickEventHandler(MiraButtonClickEvent @event)
    {
        if (ShroudInteractionPatches.IsProcessingShroud) return;

        var button = @event.Button as CustomActionButton<PlayerControl>;
        var source = PlayerControl.LocalPlayer;
        var target = button?.Target;

        if (source == null || target == null || button == null || !button.CanClick()) return;
        if (source.Data?.Role is ShroudRole) return;
        if (button is ShroudAbilityButton) return;

        CheckForShroud(@event, source, target);
    }

    [RegisterEvent(1)]
    public static void BeforeMurderHandler(BeforeMurderEvent @event)
    {
        if (ShroudInteractionPatches.IsProcessingShroud || @event.IsCancelled) return;
        if (@event.Source == null || @event.Target == null) return;
        if (@event.Source.Data?.Role is ShroudRole) return;
        if (MeetingHud.Instance || ExileController.Instance) return;

        CheckForShroud(@event, @event.Source, @event.Target);
    }

    [RegisterEvent]
    public static void RoundStartHandler(RoundStartEvent @event)
    {
        if (@event.TriggeredByIntro) return;
        ShroudInteractionPatches.IsProcessingShroud = false;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null && player.HasModifier<ShroudedModifier>())
            {
                player.RemoveModifier<ShroudedModifier>();
            }
        }
    }

    private static void CheckForShroud(MiraCancelableEvent miraEvent, PlayerControl source, PlayerControl target)
    {
        if (MeetingHud.Instance || ExileController.Instance) return;
        if (source == null || target == null) return;
        if (source.HasDied() || target.HasDied()) return;
        if (!target.TryGetModifier<ShroudedModifier>(out var shroudMod)) return;
        if (source.PlayerId == shroudMod.ShroudOwnerId) return;
        if (source.PlayerId == target.PlayerId) return;
        if (source.Data?.Role is ShroudRole) return;

        miraEvent.Cancel();
        ShroudInteractionPatches.ExecuteShroudRetaliation(source, shroudMod);
    }
}
