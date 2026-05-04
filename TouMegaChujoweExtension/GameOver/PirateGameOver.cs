using MiraAPI.GameEnd;
using Reactor.Utilities.Extensions;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Modules;
using TownOfUs.Modules.Localization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMegaChujoweExtension.GameOver;

public sealed class PirateGameOver : CustomGameOver
{
    public override bool VerifyCondition(PlayerControl playerControl, NetworkedPlayerInfo[] winners)
    {
        if (winners == null || winners.Length == 0 || playerControl == null)
        {
            return false;
        }

        return winners.Any(w => w?.Object != null && w.Object.PlayerId == playerControl.PlayerId);
    }

    public override void AfterEndGameSetup(EndGameManager endGameManager)
    {
        var winColor = TouExtensionColors.Pirate;
        var winText = $"{TouLocale.Get("ExtensionRolePirate", "Pirate")} {TouLocale.Get("ExtensionPirateWins", "Wins")}";

        GameHistory.WinningFaction = $"<color=#{winColor.ToHtmlStringRGBA()}>{winText}</color>";

        endGameManager.BackgroundBar.material.SetColor("_Color", winColor);

        var text = Object.Instantiate(endGameManager.WinText);
        text.text = $"<size=4>{winText}!</size>";
        text.color = winColor;

        var pos = endGameManager.WinText.transform.localPosition;
        pos.y = 1.5f;
        pos += Vector3.down * 0.15f;
        text.transform.localScale = new Vector3(1f, 1f, 1f);
        text.transform.position = pos;
    }
}
