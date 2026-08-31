using HarmonyLib;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Player;
using TownOfUs.Events.Neutral;

namespace TouMegaChujoweExtension.Patches.Roles;

/// <summary>
/// An exile the Innocent engineered belongs to the Innocent alone.
/// </summary>
/// <remarks>
/// Getting themselves voted out is normally the Jester's entire win condition, and an Executioner's
/// target being voted out is theirs - but not when somebody else forced the vote. Without these, a
/// taunted Jester was ejected and the Jester and the Innocent both won off the same vote, which reads
/// as the town losing to two different people at once.
/// </remarks>
[HarmonyPatch]
public static class InnocentTauntClaimPatches
{
    [HarmonyPatch(typeof(JesterEvents), nameof(JesterEvents.EjectionEventHandler))]
    [HarmonyPrefix]
    public static bool JesterEjectionPrefix(EjectionEvent @event)
    {
        return !IsTauntClaimed(@event?.ExileController?.initData?.networkedPlayer?.Object);
    }

    [HarmonyPatch(typeof(JesterEvents), nameof(JesterEvents.PlayerDeathEventHandler))]
    [HarmonyPrefix]
    public static bool JesterDeathPrefix(PlayerDeathEvent @event)
    {
        return @event?.DeathReason != DeathReason.Exile || !IsTauntClaimed(@event.Player);
    }

    [HarmonyPatch(typeof(ExecutionerEvents), nameof(ExecutionerEvents.EjectionEventHandler))]
    [HarmonyPrefix]
    public static bool ExecutionerEjectionPrefix(EjectionEvent @event)
    {
        return !IsTauntClaimed(@event?.ExileController?.initData?.networkedPlayer?.Object);
    }

    [HarmonyPatch(typeof(ExecutionerEvents), nameof(ExecutionerEvents.PlayerDeathEventHandler))]
    [HarmonyPrefix]
    public static bool ExecutionerDeathPrefix(PlayerDeathEvent @event)
    {
        return @event?.DeathReason != DeathReason.Exile || !IsTauntClaimed(@event.Player);
    }

    private static bool IsTauntClaimed(PlayerControl? exiled)
    {
        return exiled != null && exiled.Pointer != IntPtr.Zero && InnocentRole.IsClaimedByTaunt(exiled.PlayerId);
    }
}
