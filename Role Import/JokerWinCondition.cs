using MiraAPI.GameEnd;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.GameOver;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Interfaces;
using TownOfUs.Roles;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.WinConditions;

public sealed class JokerWinCondition : IWinCondition
{
    public int Priority => 5;

    public bool IsMet(LogicGameFlowNormal gameFlow)
    {
        if (!AmongUsClient.Instance.AmHost) return false;

        var options = OptionGroupSingleton<JokerOptions>.Instance;
        if (options.WinMode == JokerWinOptions.WinWithWinners) return false;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied() || player.Data?.Role is not JokerRole jokerRole)
                continue;

            if (jokerRole.WinConditionMet()) return true;
        }

        return false;
    }

    public void TriggerGameOver(LogicGameFlowNormal gameFlow)
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.Data?.Role is not JokerRole jokerRole) continue;

            if (jokerRole.WinConditionMet() && OptionGroupSingleton<JokerOptions>.Instance.WinMode != JokerWinOptions.WinWithWinners)
            {
                CustomGameOver.Trigger<JokerGameOver>(new[] { player.Data });
                return;
            }
        }
    }
}