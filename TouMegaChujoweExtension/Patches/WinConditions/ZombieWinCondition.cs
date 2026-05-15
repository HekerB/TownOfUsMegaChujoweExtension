using MiraAPI.GameEnd;
using MiraAPI.Utilities;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using TouMegaChujoweExtension.GameOver;
using TouMegaChujoweExtension.Roles.Neutral;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TownOfUs;
using TownOfUs.Interfaces;
using TownOfUs.Utilities;
using TownOfUs.Extensions;
using MiraAPI.Roles;
using System.Collections.Generic;

namespace TouMegaChujoweExtension.Patches.WinConditions;

public sealed class ZombieWinCondition : IWinCondition, IWinConditionWithBlocking
{
    public int Priority => 12;

    public bool BlocksOthers => true;

    public bool IsMet(LogicGameFlowNormal gameFlow)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return false;

        int zombieCount = 0;
        int totalAlive = 0;
        int othersCount = 0;
        int impostorCount = 0;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied()) continue;
            totalAlive++;
            if (player.IsRole<ZombieRole>())
            {
                zombieCount++;
            }
            else
            {
                othersCount++;
                if (player.IsImpostorAligned()) impostorCount++;
            }
        }

        if (zombieCount == 0) return false;

        // Advantage 2 to 1 over anything
        if (zombieCount >= othersCount * 2 && othersCount > 0) return true;
        
        // 2 zombie vs 1 impostor
        if (zombieCount == 2 && impostorCount == 1 && totalAlive == 3) return true;
        
        // All others are dead
        if (othersCount == 0) return true;

        return false;
    }

    public void TriggerGameOver(LogicGameFlowNormal gameFlow)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

        List<NetworkedPlayerInfo> winners = new();
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null && (player.Data.Role is ZombieRole || player.GetModifiers<ZombieModifier>().Any()))
            {
                winners.Add(player.Data);
            }
        }

        CustomGameOver.Trigger<ZombieGameOver>(winners.ToArray());
    }
}
