using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events;
using MiraAPI.Modifiers;
using TownOfUs.Modifiers.Game;
using TownOfUs.Modifiers.Game.Assailant;
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

        var target = @event.Target;

        if (target == null || target.Data == null)
        {
            return;
        }

        // Who did the killing does not matter; that somebody in this meeting is now dead does. This
        // used to run only when the killer was one of the meeting killers, so a Deputy shooting an
        // Assassin left that Assassin holding a full set of live guess buttons for the rest of the
        // meeting. Pressing them did nothing - a guess by a dead guesser is refused at the far end -
        // which is exactly what a broken button looks like.
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














