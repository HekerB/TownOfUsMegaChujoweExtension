using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using TouMegaChujoweExtension.Roles.Impostor;
using TouMegaChujoweExtension.Modules;
using MiraAPI.Hud;

namespace TouMegaChujoweExtension.Events.Impostor;

public static class DetonatorEvents
{
    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        var killer = @event.Source;
        if (killer == null || killer.Data == null) return;

        // If Detonator made a normal kill
        if (killer.Data.Role is DetonatorRole)
        {
            // Reset manual detonation cooldown
            // Set creation time to now to force waiting for cooldown
            DetonatorSystem.ResetDetonateCooldown(killer.PlayerId);
            
            // Reset attach cooldown
            DetonatorSystem.ResetAttachCooldown(killer.PlayerId);
        }
    }
}
