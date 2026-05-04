using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Impostor;

namespace TouMegaChujoweExtension.Events.Impostor;

public static class LuckyEvents
{
    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        if (!@event.Source.HasModifier<LuckyModifier>() || MeetingHud.Instance)
            return;

        var newCooldown = LuckyModifier.GetRandomKillCooldown();
        @event.Source.SetKillTimer(newCooldown);

        if (@event.Source.killTimer > 0)
        {
            @event.Source.killTimer = newCooldown;
        }
    }
}
