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
    public static void StartMeetingEventHandler()
    {
        PelicanSystem.StopSpectatingPelican();
        PelicanSystem.HideSwallowedNotification();

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
}