using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Events.Neutral;

public static class PirateEvents
{
    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (@event.TriggeredByIntro)
            return;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player?.Data?.Role is not PirateRole pirate)
                continue;

            if (OptionGroupSingleton<PirateOptions>.Instance.WinMode == PirateWinMode.PirateWinsWithOthers &&
                pirate.HasCompletedDuels &&
                !player.HasDied())
            {
                if (player.AmOwner)
                {
                    PlayerControl.LocalPlayer.RpcPlayerExile();

                    var notif = Helpers.CreateAndShowNotification(
                        $"<b>You have successfully completed your duels as the {TouExtensionColors.Pirate.ToTextColor()}Pirate</color>! You will now spectate and win with the winning team.</b>",
                        Color.white,
                        new Vector3(0f, 1f, -20f),
                        spr: TouExtensionIcons.PirateRoleIcon.LoadAsset());

                    notif?.AdjustNotification();
                }
                else
                {
                    var notif = Helpers.CreateAndShowNotification(
                        $"<b>The {TouExtensionColors.Pirate.ToTextColor()}Pirate</color>, {player.Data.PlayerName}, has completed all required duels!</b>",
                        Color.white,
                        new Vector3(0f, 1f, -20f),
                        spr: TouExtensionIcons.PirateRoleIcon.LoadAsset());

                    notif?.AdjustNotification();
                }

                pirate.DuelTargetId = byte.MaxValue;
                pirate.ResetDuelState();
                continue;
            }

            pirate.DuelTargetId = byte.MaxValue;
            pirate.ResetDuelState();
        }
    }
}












