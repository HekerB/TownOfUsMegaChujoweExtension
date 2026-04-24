using MiraAPI.GameEnd;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.GameOver;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Interfaces;

namespace TouMegaChujoweExtension.Patches.WinConditions;

public sealed class PirateWinCondition : IWinCondition, IWinConditionWithBlocking
{
    public int Priority => 12;

    public bool BlocksOthers =>
        OptionGroupSingleton<PirateOptions>.Instance.WinMode == PirateWinMode.PirateWins;

    public bool IsMet(LogicGameFlowNormal gameFlow)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            return false;

        if (OptionGroupSingleton<PirateOptions>.Instance.WinMode != PirateWinMode.PirateWins)
            return false;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player?.Data?.Role is PirateRole pirate && pirate.WinConditionMet())
            {
                return true;
            }
        }

        return false;
    }

    public void TriggerGameOver(LogicGameFlowNormal gameFlow)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            return;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player?.Data?.Role is PirateRole pirate && pirate.WinConditionMet() && player.Data != null)
            {
                CustomGameOver.Trigger<PirateGameOver>(new[] { player.Data });
                return;
            }
        }
    }
}