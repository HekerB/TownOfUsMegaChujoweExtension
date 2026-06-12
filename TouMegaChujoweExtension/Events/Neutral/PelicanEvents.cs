using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.Events;
using MiraAPI.Modifiers;
using TownOfUs.Events;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Events.Neutral;

public static class PelicanEvents
{
    [RegisterEvent]
    public static void StartMeetingEventHandler(StartMeetingEvent @event)
    {
        PelicanSystem.StopSpectatingPelican();
        PelicanSystem.HideSwallowedNotification();

        // Digest all swallowed players locally on every client to ensure correct state immediately.
        // If we are the host, calling PelicanSystem.DigestAll will also properly sync the death to other clients.
        DigestAllPelicans();

        var local = PlayerControl.LocalPlayer;
        if (local != null && local.Data?.Role is PelicanRole)
        {
            var swallowed = PelicanSystem.GetSwallowedByPelican(local.PlayerId);
            if (swallowed.Count > 0)
            {
                PelicanRole.RpcPelicanDigest(local);
            }
        }
    }

    private static void DigestAllPelicans()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null) continue;
            if (PelicanSystem.GetSwallowedByPelican(player.PlayerId).Count > 0)
            {
                PelicanSystem.DigestAll(player.PlayerId);
            }
        }
    }

    [RegisterEvent(501)]
    public static void AfterMurderCleanupHandler(AfterMurderEvent @event)
    {
        var victim = @event.Target;
        if (victim == null) return;

        if (!PelicanSystem.IsDigestKillVictim(victim.PlayerId)) return;

        try
        {
            if (victim.TryGetModifier<MysticDeathNotifierModifier>(out var mysticMod))
            {
                victim.RemoveModifier(mysticMod);
            }
        }
        catch
        {
            // Ignore potential issues when removing Mystic modifier during cleanup
        }
    }

    [RegisterEvent]
    public static void PlayerDeathEventHandler(PlayerDeathEvent @event)
    {
        var deadPlayer = @event.Player;
        if (deadPlayer != null)
        {
            var swallowed = PelicanSystem.GetSwallowedByPelican(deadPlayer.PlayerId);
            if (swallowed.Count > 0)
            {
                PelicanSystem.ReleaseAll(deadPlayer.PlayerId);
            }
        }
    }

    [RegisterEvent]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        if (@event.Target != null && TouMegaChujoweExtension.Modules.PelicanSystem.IsSwallowed(@event.Target.PlayerId))
        {
            @event.Cancel();
        }
    }
}