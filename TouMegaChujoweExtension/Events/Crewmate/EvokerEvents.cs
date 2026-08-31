using MiraAPI.Events;
using MiraAPI.Events.Mira;
using MiraAPI.Events.Vanilla.Gameplay;
using TouMegaChujoweExtension.Modules;

namespace TouMegaChujoweExtension.Events.Crewmate;

public static class EvokerEvents
{
    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (@event.TriggeredByIntro)
        {
            EvokerSystem.VerifiedPlayers.Clear();
        }
    }

    /// <summary>
    /// Blocks every custom ability button while the local player is blinded.
    /// <para>
    /// This replaces roughly thirty hand-written Harmony patches (Pelican, Shroud, RC-XD, Witch,
    /// Sniper, Outlaw, Kamikaze, Baker, SoulCollector, Death, BountyHunter, Poisoner...). Those
    /// existed because patching the base method (<c>TownOfUsButton.ClickHandler</c> / <c>CanUse</c>)
    /// does not intercept a derived button that overrides it, so every role with an overridden click
    /// needed its own patch - and any role nobody remembered to add simply stayed unblocked. Mira
    /// raises this event centrally for all custom buttons, overrides included, so the block now
    /// covers every role automatically.
    /// </para>
    /// </summary>
    [RegisterEvent(1)]
    public static void MiraButtonClickEventHandler(MiraButtonClickEvent @event)
    {
        if (EvokerSystem.IsLocalPlayerBlocked())
        {
            @event.Cancel();
        }
    }

    /// <summary>
    /// Catches kills that never go through a button (RPC-driven or indirect murders) for any blinded
    /// player, whatever role or modifier granted the kill.
    /// </summary>
    [RegisterEvent(1)]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        if (EvokerSystem.IsBlindActive && EvokerSystem.IsBlindTarget(@event.Source))
        {
            @event.Cancel();
        }
    }
}
