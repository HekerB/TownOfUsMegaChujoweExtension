using MiraAPI.GameEnd;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.GameOver;
using TownOfUs.Interfaces;
using TownOfUs.Modules;
using TownOfUs.Modifiers;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Options;
using MiraAPI.Modifiers;
using UnityEngine;
using TownOfUs.Roles.Neutral;

namespace TouMegaChujoweExtension.Patches.WinConditions;

/// <summary>
/// Centralized win condition manager for all neutral roles added by the extension.
/// Merges logic from individual win condition files to stay organized like TOU-Mira.
/// </summary>
public sealed class NeutralExtensionWinCondition : IWinCondition, IWinConditionWithBlocking
{
    private bool _bhGameOverTriggered;

    /// <summary>
    /// Priority 4 - matches BountyHunter's original priority to run before Mira's NeutralRoleWinCondition (5).
    /// </summary>
    public int Priority => 4;

    public bool BlocksOthers => true;

    public bool IsMet(LogicGameFlowNormal gameFlow)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return false;

        MarkJokerWinWithWinners();

        // 1. Pope Judgement (Original Priority 1)
        if (IsPopeWinMet()) return true;

        // 2. Bounty Hunter (Original Priority 4)
        if (IsBountyHunterWinMet()) return true;

        // 3. Joker
        if (IsJokerWinMet()) return true;

        // 4. Pelican (Original Priority 11)
        if (IsPelicanWinMet()) return true;

        // 5. Pirate (Original Priority 12)
        if (IsPirateWinMet()) return true;

        // 6. Lawyer (Original Priority 12)
        if (IsLawyerWinMet()) return true;

        // 7. Baker/Famine + Plaguebearer/Pestilence alliance
        if (IsBakerFaminePlagueAllianceWinMet()) return true;

        // 8. Famine
        if (IsFamineWinMet()) return true;

        // 9. Jackal (Original Priority 15)
        if (IsJackalWinMet()) return true;

        return false;
    }

    public void TriggerGameOver(LogicGameFlowNormal gameFlow)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

        // Check and trigger in priority order (Pope first as it's a definite game ender)

        // 1. Pope
        if (IsPopeWinMet())
        {
            // Handled in PopeJudgementSystem for precise timing after the animation
            return;
        }

        // 2. Joker
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied() || player.Data?.Role is not JokerRole jokerRole) continue;
            if (jokerRole.WinConditionMet() && player.Data != null)
            {
                CustomGameOver.Trigger<ExtensionNeutralGameOver>(new[] { player.Data });
                return;
            }
        }

        // 3. Pelican
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied() || player.Data?.Role is not PelicanRole pelicanRole) continue;
            if (pelicanRole.WinConditionMet() && player.Data != null)
            {
                CustomGameOver.Trigger<ExtensionNeutralGameOver>(new[] { player.Data });
                return;
            }
        }

        // 4. Bounty Hunter
        if (IsBountyHunterWinMet())
        {
            _bhGameOverTriggered = true;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player?.Data?.Role is BountyHunterRole && player.Data != null)
                {
                    CustomGameOver.Trigger<ExtensionNeutralGameOver>(new[] { player.Data });
                    return;
                }
            }
        }

        // 5. Pirate
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player?.Data?.Role is PirateRole pirate && pirate.WinConditionMet() && player.Data != null)
            {
                CustomGameOver.Trigger<ExtensionNeutralGameOver>(new[] { player.Data });
                return;
            }
        }

        // 6. Lawyer
        TriggerLawyerWin();

        // 7. Baker/Famine + Plaguebearer/Pestilence alliance
        if (IsBakerFaminePlagueAllianceWinMet())
        {
            var winners = GetBakerFaminePlagueAllianceWinners();
            if (winners.Length > 0)
            {
                CustomGameOver.Trigger<ExtensionNeutralGameOver>(winners);
                return;
            }
        }

        // 8. Famine
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied() || player.Data?.Role is not FamineRole famineRole) continue;
            if (famineRole.WinConditionMet() && player.Data != null)
            {
                CustomGameOver.Trigger<ExtensionNeutralGameOver>(new[] { player.Data });
                return;
            }
        }

        // 9. Jackal
        if (IsJackalWinMet())
        {
            var winners = PlayerControl.AllPlayerControls.ToArray()
                .Where(p => p.GetRole<JackalRole>() != null || p.TryGetModifier<SidekickModifier>(out _))
                .Select(p => p.Data)
                .ToArray();
            CustomGameOver.Trigger<ExtensionNeutralGameOver>(winners);
        }
    }

    #region Role Specific Checks

    private bool IsPopeWinMet()
    {
        bool isJudgementActive = PopeJudgementSystem.Instance != null && PopeJudgementSystem.Instance.Stage >= PopeJudgementStage.Countdown;
        bool popeRaceCondition = false;

        if (isJudgementActive)
        {
            var alive = PlayerControl.AllPlayerControls.ToArray().Where(x => !x.HasDied() && !x.Data.Disconnected).ToList();
            if (alive.Count > 0 && alive.All(x => x.Data.Role is PopeRole))
            {
                popeRaceCondition = true;
            }
        }

        return PopeJudgementSystem.GlobalBombFinished || 
               (PopeJudgementSystem.Instance != null && PopeJudgementSystem.Instance.Stage == PopeJudgementStage.Finished) || 
               popeRaceCondition;
    }

    private bool IsBountyHunterWinMet()
    {
        if (!BountyHunterSystem.HasWon) return false;
        if (_bhGameOverTriggered) return false;

        return OptionGroupSingleton<BountyHunterOptions>.Instance.WinMode == BountyHunterWinMode.SoloWin;
    }

    private bool IsPelicanWinMet()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied() || player.Data?.Role is not PelicanRole pelicanRole) continue;
            if (pelicanRole.WinConditionMet()) return true;
        }
        return false;
    }

    private bool IsJokerWinMet()
    {
        if (OptionGroupSingleton<JokerOptions>.Instance.WinMode != JokerWinOptions.SoloWin) return false;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied() || player.Data?.Role is not JokerRole jokerRole) continue;
            if (jokerRole.WinConditionMet()) return true;
        }

        return false;
    }

    private static void MarkJokerWinWithWinners()
    {
        var options = OptionGroupSingleton<JokerOptions>.Instance;
        if (options.WinMode != JokerWinOptions.WinWithWinners ||
            JokerCloneSystem.KilledCloneCount < (int)options.KillsToWin)
        {
            return;
        }

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.Data?.Role is not JokerRole jokerRole || jokerRole.MetWinCon)
            {
                continue;
            }

            jokerRole.MetWinCon = true;
            TownOfUs.Modifiers.DeathHandlerModifier.RpcUpdateLocalDeathHandler(
                player,
                "DiedToWinning",
                TownOfUs.Events.DeathEventHandlers.CurrentRound,
                TownOfUs.Modifiers.DeathHandlerOverride.SetFalse,
                lockInfo: TownOfUs.Modifiers.DeathHandlerOverride.SetTrue);
        }
    }

    private bool IsPirateWinMet()
    {
        if (OptionGroupSingleton<PirateOptions>.Instance.WinMode != PirateWinMode.PirateWins) return false;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player?.Data?.Role is PirateRole pirate && pirate.WinConditionMet())
            {
                return true;
            }
        }
        return false;
    }

    private bool IsFamineWinMet()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied() || player.Data?.Role is not FamineRole famineRole) continue;
            if (famineRole.WinConditionMet()) return true;
        }

        return false;
    }

    private bool IsBakerFaminePlagueAllianceWinMet()
    {
        if (!OptionGroupSingleton<ExtensionGameMechanicOptions>.Instance.BakerFaminePlagueAlliance)
        {
            return false;
        }

        var alivePlayers = Helpers.GetAlivePlayers();
        if (alivePlayers.Count < 2)
        {
            return false;
        }

        var hasBakerTeam = alivePlayers.Any(IsBakerFamineAllianceRole);
        var hasPlagueTeam = alivePlayers.Any(IsPlagueAllianceRole);
        return hasBakerTeam &&
               hasPlagueTeam &&
               alivePlayers.All(x => IsBakerFamineAllianceRole(x) || IsPlagueAllianceRole(x));
    }

    private static NetworkedPlayerInfo[] GetBakerFaminePlagueAllianceWinners()
    {
        return Helpers.GetAlivePlayers()
            .Where(x => x != null && (IsBakerFamineAllianceRole(x) || IsPlagueAllianceRole(x)))
            .Select(x => x.Data)
            .Where(x => x != null)
            .ToArray();
    }

    private static bool IsBakerFamineAllianceRole(PlayerControl player)
    {
        return player?.Data?.Role is BakerRole or FamineRole;
    }

    private static bool IsPlagueAllianceRole(PlayerControl player)
    {
        return player?.Data?.Role is PlaguebearerRole or PestilenceRole;
    }

    private bool IsLawyerWinMet()
    {
        if (LawyerWinConditionState.Triggered) return false;
        if (OptionGroupSingleton<LawyerOptions>.Instance.WinMode == LawyerWinMode.WinWithClient) return false;

        return IsLawyerDuoMet() || IsLawyerParityMet();
    }

    private bool IsLawyerDuoMet()
    {
        var alivePlayers = Helpers.GetAlivePlayers();
        if (alivePlayers.Count != 2) return false;

        foreach (var lawyerPc in PlayerControl.AllPlayerControls)
        {
            if (lawyerPc == null || lawyerPc.HasDied() || !lawyerPc.IsRole<LawyerRole>()) continue;

            var client = LawyerUtils.FindClientForLawyer(lawyerPc.PlayerId);
            if (client == null || client.HasDied()) continue;

            var alivePlayerIds = alivePlayers.Select(ap => ap.PlayerId).ToHashSet();
            if (!alivePlayerIds.Contains(lawyerPc.PlayerId) || !alivePlayerIds.Contains(client.PlayerId)) continue;

            if (ClientHasWonAlone(client)) continue;

            return true;
        }
        return false;
    }

    private bool IsLawyerParityMet()
    {
        var alivePlayers = Helpers.GetAlivePlayers();
        if (alivePlayers.Count != 3) return false;
        if (MiscUtils.ImpAliveCount != 1) return false;
        if (MiscUtils.NKillersAliveCount > 0 || MiscUtils.GameHaltersAliveCount > 0 || MiscUtils.CrewKillersAliveCount > 0) return false;

        foreach (var lawyerPc in PlayerControl.AllPlayerControls)
        {
            if (lawyerPc == null || lawyerPc.HasDied() || !lawyerPc.IsRole<LawyerRole>()) continue;

            var client = LawyerUtils.FindClientForLawyer(lawyerPc.PlayerId);
            if (client == null || client.HasDied()) continue;

            var alivePlayerIds = alivePlayers.Select(ap => ap.PlayerId).ToHashSet();
            if (!alivePlayerIds.Contains(lawyerPc.PlayerId) || !alivePlayerIds.Contains(client.PlayerId)) continue;

            if (ClientHasWonAlone(client)) continue;

            if (IsKillerClient(client) || client.IsCrewmate()) return true;
        }
        return false;
    }

    private bool ClientHasWonAlone(PlayerControl client)
    {
        if (client == null || client.HasDied()) return false;
        var clientRole = client.GetRoleWhenAlive();
        if (clientRole is ITownOfUsRole townOfUsRole) return townOfUsRole.WinConditionMet();
        return false;
    }

    private bool IsKillerClient(PlayerControl client)
    {
        return client != null && (client.IsImpostorAligned() || client.Is(RoleAlignment.NeutralKilling));
    }

    private void TriggerLawyerWin()
    {
        if (LawyerWinConditionState.Triggered) return;

        var alivePlayers = Helpers.GetAlivePlayers();
        var winners = new HashSet<NetworkedPlayerInfo>();

        foreach (var lawyerPc in PlayerControl.AllPlayerControls)
        {
            if (lawyerPc == null || lawyerPc.HasDied() || !lawyerPc.IsRole<LawyerRole>()) continue;

            var client = LawyerUtils.FindClientForLawyer(lawyerPc.PlayerId);
            if (client == null || client.HasDied() || lawyerPc.Data == null || client.Data == null) continue;

            var alivePlayerIds = alivePlayers.Select(ap => ap.PlayerId).ToHashSet();
            if (!alivePlayerIds.Contains(lawyerPc.PlayerId) || !alivePlayerIds.Contains(client.PlayerId)) continue;

            var lawyerRole = lawyerPc.GetRole<LawyerRole>();
            if (lawyerRole != null) lawyerRole.AboutToWin = true;

            winners.Add(lawyerPc.Data);
            winners.Add(client.Data);
        }

        if (winners.Count >= 2)
        {
            LawyerWinConditionState.MarkTriggered();
            CustomGameOver.Trigger<ExtensionNeutralGameOver>(winners.ToArray());
        }
    }

    private bool IsJackalWinMet()
    {
        var alive = Helpers.GetAlivePlayers();
        if (alive.Count == 0) return false;

        // Find the first alive Jackal to check their specific team parity
        foreach (var jackalPc in PlayerControl.AllPlayerControls)
        {
            if (jackalPc == null || jackalPc.HasDied()) continue;
            var jackal = jackalPc.GetRole<JackalRole>();
            if (jackal != null && jackal.WinConditionMet()) return true;
        }

        return false;
    }

    #endregion
}














