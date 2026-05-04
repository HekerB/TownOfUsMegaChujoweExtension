using MiraAPI.GameEnd;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.GameOver;
using TownOfUs.Interfaces;

namespace TouMegaChujoweExtension.Patches.WinConditions;

public sealed class BountyHunterWinCondition : IWinCondition, IWinConditionWithBlocking
{
    private bool _gameOverTriggered;

    /// <summary>
    /// Priority 4 - przed NeutralRoleWinCondition (5)
    /// </summary>
    public int Priority => 4;

    /// <summary>
    /// Blokuje inne win conditions TYLKO w trybie SoloWin gdy BH wygrał
    /// </summary>
    public bool BlocksOthers =>
        OptionGroupSingleton<BountyHunterOptions>.Instance.WinMode == BountyHunterWinMode.SoloWin
        && BountyHunterSystem.HasWon;

    public bool IsMet(LogicGameFlowNormal gameFlow)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return false;
        if (!BountyHunterSystem.HasWon) return false;
        if (_gameOverTriggered) return false;

        // Tylko SoloWin triggeruje własny GameOver
        // WinWithWinners polega na NeutralRoleWinCondition + DidWin
        if (OptionGroupSingleton<BountyHunterOptions>.Instance.WinMode != BountyHunterWinMode.SoloWin)
            return false;

        return true;
    }

    public void TriggerGameOver(LogicGameFlowNormal gameFlow)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
        if (_gameOverTriggered) return;
        _gameOverTriggered = true;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player?.Data?.Role is BountyHunterRole && player.Data != null)
            {
                CustomGameOver.Trigger<NeutralGameOver>(new[] { player.Data });
                return;
            }
        }
    }
}
