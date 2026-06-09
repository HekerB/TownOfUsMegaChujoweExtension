using MiraAPI.GameEnd;
using MiraAPI.Utilities;
using Object = UnityEngine.Object;
using Reactor.Utilities.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules;
using TownOfUs.Roles;
using UnityEngine;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;

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

        // Everyone sees the screen if it was triggered
        var firstWinner = winners[0];
        if (firstWinner?.Role == null) return true;

        var role = firstWinner.Role;
        _roleColor = role.TeamColor;

        if (role is PopeRole)
            _winText = $"{TouLocale.Get("ExtensionRolePope", "Pope")} {TouLocale.Get("ExtensionPopeWins", "Wins")}";
        else if (role is PelicanRole)
            _winText = $"{TouLocale.Get("ExtensionRolePelican", "Pelican")} {TouLocale.Get("ExtensionPelicanWins", "Wins")}";
        else if (role is PirateRole)
            _winText = $"{TouLocale.Get("ExtensionRolePirate", "Pirate")} {TouLocale.Get("ExtensionPirateWins", "Wins")}";
        else if (role is LawyerRole)
            _winText = TouLocale.Get("ExtensionLawyerWins", "Lawyer & Client Win");
        else if (role is BountyHunterRole)
            _winText = $"{TouLocale.Get("ExtensionRoleBountyHunter", "Bounty Hunter")} {TouLocale.Get("ExtensionBountyHunterWins", "Wins")}";
        else if (role is GaslighterRole)
            _winText = TouLocale.Get("ExtensionGaslighterWin", "Gaslighter wins");
        else if (role is GrimReaperRole)
            _winText = $"{TouLocale.Get("ExtensionRoleGrimReaper", "Grim Reaper")} {TouLocale.Get("ExtensionGrimReaperWins", "Wins")}";
        else if (role is BakerRole)
            _winText = $"{TouLocale.Get("ExtensionRoleBaker", "Baker")} {TouLocale.Get("ExtensionBakerWins", "Wins")}";
        else if (role is FamineRole)
            _winText = $"{TouLocale.Get("ExtensionRoleFamine", "Famine")} {TouLocale.Get("ExtensionFamineWins", "Wins")}";

        else
            _winText = TouLocale.GetParsed("ExtensionNeutralWinsFormat", "{0} Wins").Replace("{0}", role.GetRoleName());

        return true;
    }

    public override void AfterEndGameSetup(EndGameManager endGameManager)
    {
        endGameManager.BackgroundBar.material.SetColor("_Color", _roleColor);
        GameHistory.WinningFaction = $"<color=#{_roleColor.ToHtmlStringRGBA()}>{_winText}</color>";

        var text = Object.Instantiate(endGameManager.WinText, endGameManager.WinText.transform.parent);
        text.text = $"<size=4>{_winText}!</size>";
        text.color = _roleColor;

        var pos = endGameManager.WinText.transform.localPosition;
        pos.y = 1.5f;
        pos += Vector3.down * 0.15f;
        
        text.transform.localScale = Vector3.one;
        text.transform.localPosition = pos;
    }
}












