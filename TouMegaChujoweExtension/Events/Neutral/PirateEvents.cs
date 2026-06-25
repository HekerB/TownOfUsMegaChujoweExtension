using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Utilities;
using TownOfUs.Modules.Localization;
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
                    var roleName = TouLocale.Get("ExtensionRolePirate", "Pirate");
                    var message = TouLocale.Get(
                            "ExtensionPirateCompletedDuelsOwner",
                            "You have successfully completed your duels as the {0}! You will now spectate and win with the winning team.")
                        .Replace("{0}", $"{TouExtensionColors.Pirate.ToTextColor()}{roleName}</color>");

                    var notif = Helpers.CreateAndShowNotification(
                        $"<b>{message}</b>",
                        Color.white,
                        new Vector3(0f, 1f, -20f),
                        spr: TouExtensionIcons.PirateRoleIcon.LoadAsset());

                    notif?.AdjustNotification();
                }
                else
                {
                    var roleName = TouLocale.Get("ExtensionRolePirate", "Pirate");
                    var message = TouLocale.Get(
                            "ExtensionPirateCompletedDuelsOther",
                            "The {0}, {1}, has completed all required duels!")
                        .Replace("{0}", $"{TouExtensionColors.Pirate.ToTextColor()}{roleName}</color>")
                        .Replace("{1}", player.Data.PlayerName);

                    var notif = Helpers.CreateAndShowNotification(
                        $"<b>{message}</b>",
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












