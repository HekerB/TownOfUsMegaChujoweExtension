using MiraAPI.GameEnd;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.GameOver;
using TouMegaChujoweExtension.Roles.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using MiraAPI.GameOptions;
using System.Collections.Generic;
using System.Linq;
using TownOfUs.Interfaces;

namespace TouMegaChujoweExtension.Patches.WinConditions;

public sealed class GaslighterWinCondition : IWinCondition
{
    public int Priority => 15;

    public bool IsMet(LogicGameFlowNormal gameFlow)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return false;

        var options = OptionGroupSingleton<GaslighterOptions>.Instance;
        var aliveGaslighters = PlayerControl.AllPlayerControls.ToArray()
            .Where(p => p != null && !p.HasDied() && p.IsRole<GaslighterRole>())
            .ToList();
        
        if (aliveGaslighters.Count == 0) return false;

        switch (options.WinCondition)
        {
            case GaslighterWinMode.CrewmateLose:
                int aliveCrew = 0;
                int aliveImpostors = 0;
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc.HasDied()) continue;
                    if (pc.Data.Role.IsImpostor) aliveImpostors++;
                    else if (pc.IsRole<GaslighterRole>()) continue;
                    else aliveCrew++;
                }
                
                if (aliveImpostors >= aliveCrew && aliveCrew > 0) return true;
                return false;

            case GaslighterWinMode.LastStanding:
                return Helpers.GetAlivePlayers().Count <= aliveGaslighters.Count + 1;

            case GaslighterWinMode.AliveAtEnd:
                return false;

            default:
                return false;
        }
    }

    public void TriggerGameOver(LogicGameFlowNormal gameFlow)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

        List<NetworkedPlayerInfo> winners = new();
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p != null && !p.HasDied() && p.IsRole<GaslighterRole>())
            {
                winners.Add(p.Data);
            }
        }

        CustomGameOver.Trigger<GaslighterGameOver>(winners.ToArray());
    }
}
