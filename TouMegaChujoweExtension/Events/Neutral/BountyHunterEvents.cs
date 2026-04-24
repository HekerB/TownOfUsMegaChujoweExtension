using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Modifiers;
using TownOfUs.Utilities;
using TownOfUs.Events;
using UnityEngine;

namespace TouMegaChujoweExtension.Events.Neutral;

public static class BountyHunterEvents
{
    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        var killer = @event.Source;
        var victim = @event.Target;

        if (killer == null || victim == null || killer.Data?.Role is not BountyHunterRole)
            return;

        if (BountyHunterSystem.HasWon) return;

        BountyHunterSystem.KillsDone++;
        BountyHunterSystem.TargetKilledThisRound = true;

        var needed = (int)OptionGroupSingleton<BountyHunterOptions>.Instance.TargetsToKill.Value;
        var isSolo = OptionGroupSingleton<BountyHunterOptions>.Instance.WinMode == BountyHunterWinMode.SoloWin;

        if (BountyHunterSystem.KillsDone >= needed)
        {
            BountyHunterSystem.HasWon = true;
            BountyHunterSystem.GameEndedByBH = isSolo;
            BountyHunterSystem.ClearArrowModifiers();

            if (killer.Data?.Role is BountyHunterRole bh)
                bh.HasWon = true;

            if (killer.AmOwner)
            {
                BountyHunterRole.RpcBountyHunterWin(killer);

                if (isSolo)
                {
                    DeathHandlerModifier.RpcUpdateLocalDeathHandler(PlayerControl.LocalPlayer, "DiedToWinning",
                        DeathEventHandlers.CurrentRound, DeathHandlerOverride.SetFalse,
                        lockInfo: DeathHandlerOverride.SetTrue);
                }
            }
        }
        else if (killer.AmOwner)
        {
            BountyHunterSystem.AssignNewTarget(killer);
        }
    }

    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (@event.TriggeredByIntro)
            return;

        if (OptionGroupSingleton<BountyHunterOptions>.Instance.WinMode != BountyHunterWinMode.WinWithWinners)
            return;

        if (!BountyHunterSystem.HasWon)
            return;

        PlayerControl? bhPlayer = null;
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player?.Data?.Role is BountyHunterRole && !player.HasDied())
            {
                bhPlayer = player;
                break;
            }
        }

        if (bhPlayer == null)
            return;

        if (bhPlayer.AmOwner)
        {
            PlayerControl.LocalPlayer.RpcPlayerExile();
            var notif = Helpers.CreateAndShowNotification(
                $"<b>You have successfully completed your bounties as the {TouExtensionColors.BountyHunter.ToTextColor()}Bounty Hunter</color>! You will now spectate and win with the winning team.</b>",
                Color.white, new Vector3(0f, 1f, -20f), spr: TouExtensionIcons.BountyHunterRoleIcon.LoadAsset());
            notif.AdjustNotification();
        }
        else
        {
            var notif = Helpers.CreateAndShowNotification(
                $"<b>The {TouExtensionColors.BountyHunter.ToTextColor()}Bounty Hunter</color>, {bhPlayer.Data.PlayerName}, has completed all their bounties!</b>",
                Color.white, new Vector3(0f, 1f, -20f), spr: TouExtensionIcons.BountyHunterRoleIcon.LoadAsset());
            notif.AdjustNotification();
        }
    }
}
