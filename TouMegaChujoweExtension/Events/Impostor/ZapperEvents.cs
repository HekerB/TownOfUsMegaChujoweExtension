using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Modifiers;
using TownOfUs.Events;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Events.Impostor;

public static class ZapperEvents
{
    [RegisterEvent]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        if (@event.Source == null || @event.Target == null) return;

        if (@event.Source.Data.Role is ZapperRole)
        {
            DeathHandlerModifier.UpdateDeathHandlerImmediate(@event.Target, 
                TouLocale.Get("DiedToZapper"), 
                DeathEventHandlers.CurrentRound,
                DeathHandlerOverride.SetTrue,
                TouLocale.GetParsed("DiedByStringBasic").Replace("<player>", @event.Source.Data.PlayerName),
                lockInfo: DeathHandlerOverride.SetTrue);
        }
    }
}
