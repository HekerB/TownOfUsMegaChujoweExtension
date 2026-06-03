using MiraAPI.GameEnd;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using Object = UnityEngine.Object;
using Reactor.Utilities.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules;
using TownOfUs.Roles;
using TownOfUs.Roles.Neutral;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using TouMegaChujoweExtension.Modules;
using UnityEngine;

namespace TouMegaChujoweExtension.GameOver;

/// <summary>
/// Unified game over screen for all neutral roles in the extension.
/// Similar to Mira's NeutralGameOver but tailored for extension roles.
/// </summary>
public sealed class ExtensionNeutralGameOver : CustomGameOver
{
    private Color _roleColor = Color.white;
    private string _winText = "Neutral Wins";

    public override bool VerifyCondition(PlayerControl playerControl, NetworkedPlayerInfo[] winners)
    {
        if (winners == null || winners.Length == 0) return false;

        // Check if this is a Jackal / Infiltrator win
        bool isJackalWin = winners.Any(w => w != null && 
            (w.Role is JackalRole || 
             PlayerControl.AllPlayerControls.ToArray().Any(p => p != null && p.PlayerId == w.PlayerId && 
                 (p.IsRole<JackalRole>() || p.TryGetModifier<SidekickModifier>(out _)))));

        if (isJackalWin)
        {
            _roleColor = TouExtensionColors.Jackal;
            _winText = $"{TouLocale.Get("ExtensionRoleJackal", "Infiltrator")} {TouLocale.Get("ExtensionJackalWins", "Wins")}";
            return true;
        }

        var apocalypseWinnerCount = winners.Count(w => w != null && ApocalypseUtils.IsApocalypseRole(w.Role));
        var isApocalypseWin = apocalypseWinnerCount > 1 &&
                (NeutralExtensionWinCondition.IsApocalypseAllianceWon || ApocalypseUtils.WinsTogetherEnabled);
        if (isApocalypseWin)
        {
            _roleColor = TouExtensionColors.Death;
            _winText = TouLocale.Get("ExtensionApocalypseWins", "Apocalypse Wins");
            return true;
        }

        // Everyone sees the screen if it was triggered
        var firstWinner = winners[0];
        if (firstWinner?.Role == null) return true;

        var role = firstWinner.Role;
        _roleColor = role.TeamColor;

        _winText = role switch
        {
            PopeRole => $"{TouLocale.Get("ExtensionRolePope", "Pope")} {TouLocale.Get("ExtensionPopeWins", "Wins")}",
            PelicanRole => $"{TouLocale.Get("ExtensionRolePelican", "Pelican")} {TouLocale.Get("ExtensionPelicanWins", "Wins")}",
            PirateRole => $"{TouLocale.Get("ExtensionRolePirate", "Pirate")} {TouLocale.Get("ExtensionPirateWins", "Wins")}",
            LawyerRole => TouLocale.Get("ExtensionLawyerWins", "Lawyer & Client Win"),
            BountyHunterRole => $"{TouLocale.Get("ExtensionRoleBountyHunter", "Bounty Hunter")} {TouLocale.Get("ExtensionBountyHunterWins", "Wins")}",
            JokerRole => $"{TouLocale.Get("ExtensionRoleJoker", "Joker")} {TouLocale.Get("ExtensionJokerWins", "Wins")}",
            BakerRole or FamineRole => TouLocale.Get("ExtensionBakerFamineWins", "Baker / Famine Wins"),
            TouMegaChujoweExtension.Roles.Classic.Neutral.SoulCollectorRole or DeathRole => TouLocale.Get("ExtensionSoulCollectorDeathWins", "Soul Collector / Death Wins"),
            BerserkerRole => TouLocale.Get("ExtensionBerserkerWarWins", "Berserker / War Wins"),
            WarRole => TouLocale.Get("ExtensionBerserkerWarWins", "Berserker / War Wins"),
            _ => TouLocale.GetParsed("ExtensionNeutralWinsFormat", "{0} Wins").Replace("{0}", role.GetRoleName())
        };

        return true;
    }

    public override void AfterEndGameSetup(EndGameManager endGameManager)
    {
        endGameManager.BackgroundBar.material.SetColor("_Color", _roleColor);
        SetWinningFaction(_roleColor, _winText);

        var text = Object.Instantiate(endGameManager.WinText, endGameManager.WinText.transform.parent);
        text.text = $"<size=4>{_winText}!</size>";
        text.color = _roleColor;

        var pos = endGameManager.WinText.transform.localPosition;
        pos.y = 1.5f;
        pos += Vector3.down * 0.15f;

        text.transform.localScale = Vector3.one;
        text.transform.localPosition = pos;
    }

    private static void SetWinningFaction(Color color, string text)
    {
        GameHistory.WinningFaction = $"<color=#{color.ToHtmlStringRGBA()}>{text}</color>";
    }
}
