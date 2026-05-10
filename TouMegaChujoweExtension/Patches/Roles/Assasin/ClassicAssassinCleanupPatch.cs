using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events;
using MiraAPI.Modifiers;
using TownOfUs.Modifiers.Game;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles.Neutral;

namespace TouMegaChujoweExtension.Patches.Roles.Assasin;

public static class ClassicAssassinCleanupPatch
{
    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        if (!MeetingHud.Instance)
        {
            return;
        }

        var source = @event.Source;
        var target = @event.Target;

        if (source == null || source.Data == null || target == null || target.Data == null)
        {
            return;
        }

        var isMeetingKiller =
            source.HasModifier<AssassinModifier>() ||
            source.Data.Role is VigilanteRole ||
            source.Data.Role is DoomsayerRole ||
            source.Data.Role is JailorRole;

        if (!isMeetingKiller)
        {
            return;
        }

        if (ClassicAssassinSystem.IsActive)
        {
            if (target.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            {
                ClassicAssassinSystem.HideAllButtons();
                return;
            }

            ClassicAssassinSystem.HideForPlayer(target.PlayerId);
        }
        else
        {
            ClassicAssassinSystem.TryRefreshTablet();
        }
    }
}














