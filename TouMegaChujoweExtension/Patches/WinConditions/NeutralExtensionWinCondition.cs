using HarmonyLib;
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
using TouMegaChujoweExtension.GameOver;
using MiraAPI.Modifiers;
using UnityEngine;
using TownOfUs.Roles.Neutral;
using TouMegaChujoweExtension.Modules;

namespace TouMegaChujoweExtension.Patches.WinConditions;

public sealed class NeutralExtensionWinCondition : IWinCondition, IWinConditionWithBlocking
{
    private bool _bhGameOverTriggered;
    public static bool IsApocalypseAllianceWon { get; private set; }

    public int Priority => 4;
    public bool BlocksOthers => true;

    public bool IsMet(LogicGameFlowNormal gameFlow)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return false;

        IsApocalypseAllianceWon = false;

        MarkJokerWinWithWinners();

        // 1. Pope Judgement (Original Priority 1)
        if (IsPopeWinMet()) return true;

        // 2. Bounty Hunter (Original Priority 4)
        if (IsBountyHunterWinMet()) return true;

        // 3. Joker
        if (IsJokerWinMet()) return true;

        // 4. Innocent
        if (IsInnocentWinMet()) return true;

        // 5. Pelican (Original Priority 11)
        if (IsPelicanWinMet()) return true;

        // 6. Pirate (Original Priority 12)
        if (IsPirateWinMet()) return true;

        // 7. Lawyer (Original Priority 12)
        if (IsLawyerWinMet()) return true;

        // 8. Apocalypse alliance
        if (IsApocalypseAllianceWinMet()) return true;

        // 9. Famine
        if (IsFamineWinMet()) return true;

        // 10. Death
        if (IsDeathWinMet()) return true;

        // 11. Berserker / War
        if (IsBerserkerWinMet()) return true;
        if (IsWarWinMet()) return true;

        // 12. Jackal (Original Priority 15)
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
                CustomGameOver.Trigger<ExtensionNeutralGameOver>([player.Data]);
                return;
            }
        }

        // 3. Pelican
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied() || player.Data?.Role is not PelicanRole pelicanRole) continue;
            if (pelicanRole.WinConditionMet() && player.Data != null)
            {
                CustomGameOver.Trigger<ExtensionNeutralGameOver>([player.Data]);
                return;
            }
        }

        // 4. Innocent
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied() || player.Data?.Role is not InnocentRole innocentRole) continue;
            if (innocentRole.WinConditionMet() && player.Data != null)
            {
                CustomGameOver.Trigger<ExtensionNeutralGameOver>([player.Data]);
                return;
            }
        }

        // 5. Bounty Hunter
        if (IsBountyHunterWinMet())
        {
            _bhGameOverTriggered = true;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player?.Data?.Role is BountyHunterRole && player.Data != null)
                {
                    CustomGameOver.Trigger<ExtensionNeutralGameOver>([player.Data]);
                    return;
                }
            }
        }

        // 6. Pirate
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player?.Data?.Role is PirateRole pirate && pirate.WinConditionMet() && player.Data != null)
            {
                CustomGameOver.Trigger<ExtensionNeutralGameOver>([player.Data]);
                return;
            }
        }

        // 7. Lawyer
        TriggerLawyerWin();

        // 8. Apocalypse alliance
        if (IsApocalypseAllianceWinMet())
        {
            var winners = GetApocalypseAllianceWinners();
            if (winners.Length > 0)
            {
                IsApocalypseAllianceWon = true;
                CustomGameOver.Trigger<ExtensionNeutralGameOver>(winners);
                return;
            }
        }

        // 9. Famine
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied() || player.Data?.Role is not FamineRole famineRole) continue;
            if (famineRole.WinConditionMet() && player.Data != null)
            {
                CustomGameOver.Trigger<ExtensionNeutralGameOver>([player.Data]);
                return;
            }
        }

        // 10. Death
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied() || player.Data?.Role is not DeathRole deathRole) continue;
            if (deathRole.WinConditionMet() && player.Data != null)
            {
                CustomGameOver.Trigger<ExtensionNeutralGameOver>([player.Data]);
                return;
            }
        }

        // 11. Berserker / War
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied() || player.Data?.Role is not BerserkerRole berserkerRole) continue;
            if (berserkerRole.WinConditionMet() && player.Data != null)
            {
                var winners = PlayerControl.AllPlayerControls.ToArray()
                    .Where(p => p != null && !p.HasDied() && p.Data?.Role is BerserkerRole && p.Data != null)
                    .Select(p => p.Data)
                    .ToArray();
                CustomGameOver.Trigger<ExtensionNeutralGameOver>(winners);
                return;
            }
        }
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied() || player.Data?.Role is not WarRole warRole) continue;
            if (warRole.WinConditionMet() && player.Data != null)
            {
                var winners = PlayerControl.AllPlayerControls.ToArray()
                    .Where(p => p != null && !p.HasDied() && p.Data?.Role is WarRole && p.Data != null)
                    .Select(p => p.Data)
                    .ToArray();
                CustomGameOver.Trigger<ExtensionNeutralGameOver>(winners);
                return;
            }
        }

        // 12. Jackal
        var winningJackalId = GetWinningJackalId();
        if (winningJackalId.HasValue)
        {
            var winners = PlayerControl.AllPlayerControls.ToArray()
                .Where(p => p.PlayerId == winningJackalId.Value ||
                            (p.TryGetModifier<SidekickModifier>(out var m) && m.JackalId == winningJackalId.Value))
                .Select(p => p.Data)
                .ToArray();
            CustomGameOver.Trigger<ExtensionNeutralGameOver>(winners);
        }
    }

    #region Role Specific Checks

    private static bool IsPopeWinMet()
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

    private static bool IsPelicanWinMet()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied() || player.Data?.Role is not PelicanRole pelicanRole) continue;
            if (pelicanRole.WinConditionMet()) return true;
        }
        return false;
    }

    private static bool IsInnocentWinMet()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied() || player.Data?.Role is not InnocentRole innocentRole) continue;
            if (innocentRole.WinConditionMet()) return true;
        }

        return false;
    }

    private static bool IsJokerWinMet()
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

    private static bool IsPirateWinMet()
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

    private static bool IsFamineWinMet()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied() || player.Data?.Role is not FamineRole famineRole) continue;
            if (famineRole.WinConditionMet()) return true;
        }

        return false;
    }

    private static bool IsBerserkerWinMet()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied() || player.Data?.Role is not BerserkerRole berserkerRole) continue;
            if (berserkerRole.WinConditionMet()) return true;
        }

        return false;
    }

    private static bool IsWarWinMet()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied() || player.Data?.Role is not WarRole warRole) continue;
            if (warRole.WinConditionMet()) return true;
        }

        return false;
    }

    private static bool IsDeathWinMet()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied() || player.Data?.Role is not DeathRole deathRole) continue;
            if (deathRole.WinConditionMet()) return true;
        }

        return false;
    }

    private static bool IsApocalypseAllianceWinMet()
    {
        if (!OptionGroupSingleton<ExtensionGameMechanicOptions>.Instance.ApocalypseWinsTogether)
        {
            return false;
        }

        var alivePlayers = Helpers.GetAlivePlayers();
        if (alivePlayers.Count < 2)
        {
            return false;
        }

        return alivePlayers.All(ApocalypseUtils.IsApocalypsePlayer);
    }

    private static NetworkedPlayerInfo[] GetApocalypseAllianceWinners()
    {
        return [.. Helpers.GetAlivePlayers()
            .Where(ApocalypseUtils.IsApocalypsePlayer)
            .Select(x => x.Data)
            .Where(x => x != null)];
    }

    private static bool IsLawyerWinMet()
    {
        if (LawyerWinConditionState.Triggered) return false;
        if (OptionGroupSingleton<LawyerOptions>.Instance.WinMode == LawyerWinMode.WinWithClient) return false;

        return IsLawyerDuoMet() || IsLawyerParityMet();
    }

    private static bool IsLawyerDuoMet()
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

    private static bool IsLawyerParityMet()
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

    private static bool ClientHasWonAlone(PlayerControl client)
    {
        if (client == null || client.HasDied()) return false;
        var clientRole = client.GetRoleWhenAlive();
        if (clientRole is ITownOfUsRole townOfUsRole) return townOfUsRole.WinConditionMet();
        return false;
    }

    private static bool IsKillerClient(PlayerControl client)
    {
        return client != null && (client.IsImpostorAligned() || client.Is(RoleAlignment.NeutralKilling));
    }

    private static void TriggerLawyerWin()
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
            CustomGameOver.Trigger<ExtensionNeutralGameOver>([.. winners]);
        }
    }

    public static byte? GetWinningJackalId()
    {
        var alive = Helpers.GetAlivePlayers();
        if (alive.Count == 0) return null;

        foreach (var jackalPc in PlayerControl.AllPlayerControls)
        {
            if (jackalPc == null) continue;
            var jackal = jackalPc.GetRole<JackalRole>();
            if (jackal == null) continue;

            var jackalTeam = alive.Where(p =>
                p != null && p.Pointer != IntPtr.Zero &&
                (p.PlayerId == jackalPc.PlayerId ||
                 (p.TryGetModifier<SidekickModifier>(out var m) && m.JackalId == jackalPc.PlayerId))
            ).ToList();

            var jackalTeamCount = jackalTeam.Count;

            if (jackalTeamCount > 0 && alive.Count == jackalTeamCount)
            {
                return jackalPc.PlayerId;
            }
        }

        return null;
    }

    private static bool IsJackalWinMet() => GetWinningJackalId().HasValue;

    #endregion
}

[HarmonyPatch(typeof(PestilenceRole), nameof(PestilenceRole.DidWin))]
public static class PestilenceAllianceDidWinPatch
{
    [HarmonyPrefix]
    public static bool Prefix(PestilenceRole __instance, GameOverReason gameOverReason, ref bool __result)
    {
        if (gameOverReason == CustomGameOver.GameOverReason<ExtensionNeutralGameOver>() &&
            NeutralExtensionWinCondition.IsApocalypseAllianceWon)
        {
            __result = true;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(PlaguebearerRole), nameof(PlaguebearerRole.DidWin))]
public static class PlaguebearerAllianceDidWinPatch
{
    [HarmonyPrefix]
    public static bool Prefix(PlaguebearerRole __instance, GameOverReason gameOverReason, ref bool __result)
    {
        if (gameOverReason == CustomGameOver.GameOverReason<ExtensionNeutralGameOver>() &&
            NeutralExtensionWinCondition.IsApocalypseAllianceWon)
        {
            __result = true;
            return false;
        }
        return true;
    }
}


