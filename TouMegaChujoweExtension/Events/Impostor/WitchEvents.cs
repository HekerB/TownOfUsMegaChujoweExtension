using BepInEx.Logging;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using Reactor.Utilities;
using System.Collections;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Networking;
using TownOfUs.Utilities;
using TouMegaChujoweExtension.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Events.Impostor;

public static class WitchEvents
{
    // // private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("WitchEvents");
    private static readonly List<PlayerControl> PendingSpellDeaths = new();
    private static int _meetingCount;
    private static bool _processingDeaths;

    public static int GetCurrentMeetingCount() => _meetingCount;

    private static bool HasAnyWitch()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null && player.IsRole<WitchRole>())
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAnyHexedPlayers()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null && player.HasModifier<WitchSpellboundModifier>())
            {
                return true;
            }
        }

        return false;
    }

    [RegisterEvent]
    public static void StartMeetingEventHandler(StartMeetingEvent @event)
    {
        _meetingCount++;
        // Logger.LogWarning($"[Witch] StartMeetingEventHandler: Meeting count incremented to {_meetingCount}");


        WitchRole.SendBatchedNotifications();

        if (MeetingHud.Instance == null)
        {
            return;
        }

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || !player.HasModifier<WitchSpellboundModifier>())
            {
                continue;
            }

            var voteArea = MeetingHud.Instance.playerStates.FirstOrDefault(x => x.TargetPlayerId == player.PlayerId);
            if (voteArea != null)
            {
                voteArea.NameText.color = TouExtensionColors.Witch;
            }
        }

        Coroutines.Start(CoMonitorMeetingEnd());
    }

    private static IEnumerator CoMonitorMeetingEnd()
    {
        while (MeetingHud.Instance != null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (!HasAnyWitch() || !HasAnyHexedPlayers())
        {
            yield break;
        }

        // Logger.LogWarning($"[Witch] CoMonitorMeetingEnd: Meeting ended, processing spell deaths");

        if (!_processingDeaths)
        {
            _processingDeaths = true;
            Coroutines.Start(CoProcessSpellDeaths());
        }
        else
        {
            // Logger.LogWarning($"[Witch] CoMonitorMeetingEnd: Deaths already being processed via EjectionEventHandler, skipping");
        }
    }

    [RegisterEvent]
    public static void EjectionEventHandler(EjectionEvent @event)
    {
        var exiled = @event.ExileController?.initData?.networkedPlayer?.Object;
        if (exiled == null)
        {
            return;
        }

        if (exiled.IsRole<WitchRole>())
        {
            // Logger.LogWarning($"[Witch] EjectionEventHandler: Witch {exiled.Data.PlayerName} (ID: {exiled.PlayerId}) was voted out, clearing their spellbound modifiers");
            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
            {
                WitchRole.RpcWitchClearSpellboundByWitch(PlayerControl.LocalPlayer, exiled.PlayerId);
            }
            return;
        }

        if (!HasAnyWitch() || !HasAnyHexedPlayers())
        {
            return;
        }

        // Logger.LogWarning($"[Witch] EjectionEventHandler: Starting spell death processing. Meeting count: {_meetingCount}");
        _processingDeaths = true;
        Coroutines.Start(CoProcessSpellDeaths());
    }

    private static IEnumerator CoProcessSpellDeaths()
    {
        try
        {
            while (MeetingHud.Instance != null)
            {
                yield return new WaitForSeconds(0.05f);
            }

            yield return new WaitForSeconds(0.1f);


            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                yield break;
            }

            if (!HasAnyWitch() || !HasAnyHexedPlayers())
            {
                yield break;
            }

            // Logger.LogWarning($"[Witch] CoProcessSpellDeaths: Starting coroutine. Meeting count: {_meetingCount}");
            // Logger.LogWarning($"[Witch] CoProcessSpellDeaths: Meeting ended, checking witch status");

            var witchAlive = false;

            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || !player.IsRole<WitchRole>())
                {
                    continue;
                }

                if (!player.HasDied())
                {
                    witchAlive = true;
                }
            }

            // Logger.LogWarning($"[Witch] CoProcessSpellDeaths: Witch alive: {witchAlive}");

            if (!witchAlive)
            {
                // Logger.LogWarning($"[Witch] CoProcessSpellDeaths: All Witches are dead, clearing all spellbound modifiers");
                WitchRole.RpcWitchClearAllSpellbound(PlayerControl.LocalPlayer);
                PendingSpellDeaths.Clear();
                yield break;
            }

            var options = OptionGroupSingleton<WitchOptions>.Instance;
            var meetingsUntilDeath = options.MeetingsUntilDeath;
            // Logger.LogWarning(
            //     $"[Witch] CoProcessSpellDeaths: Meetings until death: {meetingsUntilDeath}, Current meeting count: {_meetingCount}");
            
            var spellboundPlayers = new List<PlayerControl>();
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc != null && pc.HasModifier<WitchSpellboundModifier>())
                {
                    spellboundPlayers.Add(pc);
                }
            }

            // Logger.LogWarning($"[Witch] CoProcessSpellDeaths: Found {spellboundPlayers.Count} spellbound players");

            foreach (var player in spellboundPlayers)
            {
                if (player == null || player.HasDied())
                {
                    // Logger.LogWarning(
                    //     $"[Witch] CoProcessSpellDeaths: Skipping {player?.Data?.PlayerName ?? "null"} - already dead or null");
                    continue;
                }

                var modifier = player.GetModifier<WitchSpellboundModifier>();
                if (modifier == null)
                {
                    // Logger.LogWarning($"[Witch] CoProcessSpellDeaths: Skipping {player.Data.PlayerName} - no modifier found");
                    continue;
                }

                var hexingWitch = MiscUtils.PlayerById(modifier.WitchId);
                if (hexingWitch == null || hexingWitch.HasDied() || !hexingWitch.IsRole<WitchRole>())
                {
                    // Logger.LogWarning(
                    //     $"[Witch] CoProcessSpellDeaths: Witch {modifier.WitchId} who hexed {player.Data.PlayerName} is dead, clearing modifier");
                    WitchRole.RpcWitchClearSpellboundPlayer(PlayerControl.LocalPlayer, player.PlayerId);
                    continue;
                }

                var meetingsSinceSpell = _meetingCount - modifier.SpellCastMeeting;
                var meetingsSinceSpellFloat = (float)meetingsSinceSpell;
                // Logger.LogWarning(
                //     $"[Witch] CoProcessSpellDeaths: Player {player.Data.PlayerName} - SpellCastMeeting: {modifier.SpellCastMeeting}, CurrentMeetingCount: {_meetingCount}, MeetingsSinceSpell: {meetingsSinceSpellFloat}, MeetingsUntilDeath: {meetingsUntilDeath}");

                if (meetingsSinceSpellFloat >= meetingsUntilDeath)
                {
                    // Logger.LogWarning(
                    //     $"[Witch] CoProcessSpellDeaths: Player {player.Data.PlayerName} should die! ({meetingsSinceSpellFloat} >= {meetingsUntilDeath})");
                }
                else
                {
                    // Logger.LogWarning(
                    //     $"[Witch] CoProcessSpellDeaths: Player {player.Data.PlayerName} not dying yet ({meetingsSinceSpellFloat} < {meetingsUntilDeath})");
                    continue;
                }

                var shouldDie = true;

                var shieldType = player.GetShieldType();
                if (shieldType != ShieldType.None)
                {
                    // Logger.LogWarning($"[Witch] CoProcessSpellDeaths: Player {player.Data.PlayerName} is protected by {shieldType}");
                    shouldDie = false;
                }

                if (shouldDie)
                {
                    // Logger.LogWarning(
                    //     $"[Witch] CoProcessSpellDeaths: Attempting to kill {player.Data.PlayerName}, witch found: {hexingWitch != null}");

                    if (hexingWitch != null)
                    {
                        // Logger.LogWarning($"[Witch] CoProcessSpellDeaths: Calling RpcSpecialMurder on {player.Data.PlayerName}");
                        hexingWitch.RpcSpecialMurder(
                            player,
                            isIndirect: true,
                            ignoreShield: false,
                            didSucceed: true,
                            resetKillTimer: true,
                            createDeadBody: false,
                            teleportMurderer: false,
                            showKillAnim: true,
                            playKillSound: false,
                            causeOfDeath: "Witch");
                    }
                    else
                    {
                        // Logger.LogWarning($"[Witch] CoProcessSpellDeaths: Witch not found, using fallback RpcMurderPlayer");
                        player.RpcMurderPlayer(player, true);
                    }

                    WitchRole.RpcWitchClearSpellboundPlayer(PlayerControl.LocalPlayer, player.PlayerId);
                    // Logger.LogWarning($"[Witch] CoProcessSpellDeaths: Cleared modifier from {player.Data.PlayerName}");
                }
                else
                {
                    // Logger.LogWarning($"[Witch] CoProcessSpellDeaths: Player {player.Data.PlayerName} survived due to shield");
                    WitchRole.RpcWitchClearSpellboundPlayer(PlayerControl.LocalPlayer, player.PlayerId);
                }
            }

            PendingSpellDeaths.Clear();
        }
        finally
        {
            _processingDeaths = false;
        }
    }

    [RegisterEvent]
    public static void PlayerDeathEventHandler(PlayerDeathEvent @event)
    {
        var victim = @event.Player;
        if (victim == null)
        {
            return;
        }


        if (victim.IsRole<WitchRole>())
        {
            // If a Witch dies during a meeting, wait for the meeting to end before processing
            if (MeetingHud.Instance != null)
            {
                if (!HasAnyHexedPlayers())
                {
                    return;
                }

                if (!_processingDeaths)
                {
                    _processingDeaths = true;
                    Coroutines.Start(CoProcessSpellDeaths());
                }

                return;
            }

            // If a Witch dies outside a meeting, clear only their spellbound modifiers immediately
            // Logger.LogWarning($"[Witch] PlayerDeathEventHandler: Witch {victim.Data.PlayerName} (ID: {victim.PlayerId}) died, clearing their spellbound modifiers");
            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
            {
                WitchRole.RpcWitchClearSpellboundByWitch(PlayerControl.LocalPlayer, victim.PlayerId);
            }
        }
    }

    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {

        if (@event.TriggeredByIntro)
        {
            _meetingCount = 0;
        }
    }
}
