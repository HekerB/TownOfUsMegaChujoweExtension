using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;

namespace TouMegaChujoweExtension.Events.Impostor;

public static class VoodooEvents
{
    [RegisterEvent]
    public static void StartMeetingEventHandler(StartMeetingEvent @event)
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null) continue;

            if (player.TryGetModifier<VoodooScheduledCurseModifier>(out var sched))
            {
                if (sched.CurseType == VoodooEffect.Mute)
                {
                    player.AddModifier<VoodooMutedModifier>();
                }
                else if (sched.CurseType == VoodooEffect.Deafness)
                {
                    player.AddModifier<VoodooDeafenedModifier>();
                }
                player.RemoveModifier(sched);
            }
        }
    }

    [RegisterEvent]
    public static void EndMeetingEventHandler(EndMeetingEvent @event)
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null) continue;

            if (player.HasModifier<VoodooMutedModifier>())
            {
                player.RemoveModifier<VoodooMutedModifier>();
            }
            if (player.HasModifier<VoodooDeafenedModifier>())
            {
                player.RemoveModifier<VoodooDeafenedModifier>();
            }
        }
    }
}
