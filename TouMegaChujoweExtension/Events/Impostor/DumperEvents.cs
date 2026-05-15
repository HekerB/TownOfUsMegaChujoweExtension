using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Player;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Extensions;
using TownOfUs.Buttons;
using System.Linq;

namespace TouMegaChujoweExtension.Events.Impostor;

public static class DumperEvents
{
    [RegisterEvent]
    public static void OnAfterMurder(AfterMurderEvent @event)
    {
        if (@event.Source == null || @event.Target == null) return;
        
        if (@event.Source.AmOwner && @event.Source.Data.Role is DumperRole)
        {
            // Track the kill locally for the dumper
            DumperSystem.MyKills.Add(@event.Target.PlayerId);
            
            // Set cooldown on Take button
            var takeButton = Buttons.Impostor.DumperTakeButton.Instance;
            if (takeButton != null)
            {
                takeButton.Timer = takeButton.Cooldown;
            }
        }
    }
}
