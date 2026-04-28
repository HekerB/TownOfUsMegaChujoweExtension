using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Buttons.Crewmate;
using TouMegaChujoweExtension.Modifiers.Crewmate;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Events;

public static class VampireHunterEvents
{
    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (@event.TriggeredByIntro)
        {
            StakeButton.ResetFirstRound();

            if (PlayerControl.LocalPlayer.IsHost())
            {
                ToBecomeVampireHunterModifier.TryAssignAtGameStart();
            }
            return;
        }

        StakeButton.EndFirstRound();

        if (!PlayerControl.LocalPlayer.IsHost()) return;

        // Try spawn VH
        ToBecomeVampireHunterModifier.TrySpawnAfterMeeting();

        // Check if all vampires dead → convert VH
        CheckVampiresDead();
    }

    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        if (!PlayerControl.LocalPlayer.IsHost()) return;

        // Check if a VH just died
        if (@event.Target.Data.Role is VampireHunterRole)
        {
            ToBecomeVampireHunterModifier.OnVampireHunterDied();
        }

        CheckVampiresDead();
    }

    private static void CheckVampiresDead()
    {
        if (!PlayerControl.LocalPlayer.IsHost()) return;

        var livingVampires = PlayerControl.AllPlayerControls.ToArray()
            .Count(x => !x.HasDied() && x.Data.Role is VampireRole);

        if (livingVampires > 0) return;

        var hunters = PlayerControl.AllPlayerControls.ToArray()
            .Where(x => !x.HasDied() && x.Data.Role is VampireHunterRole)
            .ToList();

        foreach (var hunterPlayer in hunters)
        {
            if (hunterPlayer.Data.Role is VampireHunterRole vhRole)
            {
                vhRole.ConvertToNewRole();
            }
        }
    }
}
