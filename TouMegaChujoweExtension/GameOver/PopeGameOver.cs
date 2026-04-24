using MiraAPI.GameEnd;
using Reactor.Utilities.Extensions;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Modules;
using TownOfUs.Modules.Localization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMegaChujoweExtension.GameOver;

public sealed class PopeGameOver : CustomGameOver
{
    public override bool VerifyCondition(PlayerControl playerControl, NetworkedPlayerInfo[] winners)
    {
        if (winners == null || playerControl == null)
            return false;

        return winners.Any(w => w != null && w.PlayerId == playerControl.PlayerId);
    }

    public override void AfterEndGameSetup(EndGameManager endGameManager)
    {
        var winColor = TouExtensionColors.Pope;
        var winText = $"{TouLocale.Get("ExtensionRolePope", "Pope")} {TouLocale.Get("ExtensionPopeWins", "Wins")}";

        endGameManager.BackgroundBar.material.SetColor("_Color", winColor);
        GameHistory.WinningFaction = $"<color=#{winColor.ToHtmlStringRGBA()}>{winText}</color>";

        var text = Object.Instantiate(endGameManager.WinText, endGameManager.WinText.transform.parent);
        text.text = $"<size=4>{winText}!</size>";
        text.color = winColor;

        var pos = endGameManager.WinText.transform.localPosition;
        pos.y = 1.5f;
        pos += Vector3.down * 0.15f;
        
        text.transform.localScale = Vector3.one;
        text.transform.localPosition = pos;
    }
}