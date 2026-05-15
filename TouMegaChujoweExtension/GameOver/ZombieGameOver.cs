using MiraAPI.GameEnd;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules;
using TownOfUs;
using UnityEngine;

namespace TouMegaChujoweExtension.GameOver;

public sealed class ZombieGameOver : CustomGameOver
{
    public override bool VerifyCondition(PlayerControl playerControl, NetworkedPlayerInfo[] winners)
    {
        return winners != null && winners.Any(w => w?.Object != null && w.Object.PlayerId == playerControl.PlayerId);
    }

    public override void AfterEndGameSetup(EndGameManager endGameManager)
    {
        var winColor = TouExtensionColors.Zombie;
        var winText = TouLocale.Get("ExtensionZombieWin", "The Horde wins");

        GameHistory.WinningFaction = $"<color=#{ColorUtility.ToHtmlStringRGBA(winColor)}>{winText}</color>";

        endGameManager.BackgroundBar.material.SetColor("_Color", winColor);

        var text = UnityEngine.Object.Instantiate(endGameManager.WinText);
        text.text = $"<size=4>{winText}!</size>";
        text.color = winColor;

        var pos = endGameManager.WinText.transform.localPosition;
        pos.y = 1.5f;
        pos += Vector3.down * 0.15f;
        text.transform.localScale = new Vector3(1f, 1f, 1f);
        text.transform.position = pos;
    }
}
