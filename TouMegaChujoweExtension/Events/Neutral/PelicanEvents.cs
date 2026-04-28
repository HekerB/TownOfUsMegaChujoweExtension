using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Events;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Crewmate;
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

        // Usuwamy strzałkę od Mystica
        try
        {
            if (victim.TryGetModifier<MysticDeathNotifierModifier>(out var mysticMod))
            {
                victim.RemoveModifier(mysticMod);
            }
        }
        catch { }
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
