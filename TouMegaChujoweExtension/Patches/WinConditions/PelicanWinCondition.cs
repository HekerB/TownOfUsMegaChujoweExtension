using MiraAPI.GameEnd;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.GameOver;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Interfaces;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.WinConditions;

public sealed class PelicanWinCondition : IWinCondition, IWinConditionWithBlocking
{
    public int Priority => 11;

    public bool BlocksOthers => true;

    public bool IsMet(LogicGameFlowNormal gameFlow)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return false;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied() || player.Data?.Role is not PelicanRole pelicanRole) continue;

            if (pelicanRole.WinConditionMet()) return true;
        }

        return false;
    }

    public void TriggerGameOver(LogicGameFlowNormal gameFlow)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied() || player.Data?.Role is not PelicanRole pelicanRole) continue;

            if (pelicanRole.WinConditionMet() && player.Data != null)
            {
                CustomGameOver.Trigger<PelicanGameOver>(new[] { player.Data });
                return;
            }
        }
    }
}