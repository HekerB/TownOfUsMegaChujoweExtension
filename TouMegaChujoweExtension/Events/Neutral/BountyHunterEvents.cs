using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Events;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Events.Neutral;

public static class BountyHunterEvents
{
    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        var killer = @event.Source;
        var victim = @event.Target;

        if (killer.Data?.Role is not BountyHunterRole bh)
            return;

        if (bh.HasWon) return;

        bh.OnTargetKilled();

        var isSolo = OptionGroupSingleton<BountyHunterOptions>.Instance.WinMode == BountyHunterWinMode.SoloWin;

        if (bh.HasWon)
        {
            if (killer.AmOwner)
            {
                BountyHunterRole.RpcBountyHunterWin(killer);

                if (isSolo)
                {
                    DeathHandlerModifier.RpcUpdateLocalDeathHandler(PlayerControl.LocalPlayer, "DiedToWinning",
                        DeathEventHandlers.CurrentRound, DeathHandlerOverride.SetFalse,
                        lockInfo: DeathHandlerOverride.SetTrue);
                }
                else
                {
                    // Non-solo win: die immediately
                    DeathHandlerModifier.RpcUpdateLocalDeathHandler(PlayerControl.LocalPlayer, "DiedToWinning",
                        DeathEventHandlers.CurrentRound, DeathHandlerOverride.SetFalse,
                        lockInfo: DeathHandlerOverride.SetTrue);
                    PlayerControl.LocalPlayer.RpcPlayerExile();
                    
                    var notif = Helpers.CreateAndShowNotification(
                        TouLocale.GetParsed("ExtensionBHWinWithWinnersSelf"),
                        Color.white, new Vector3(0f, 1f, -20f), spr: TouExtensionIcons.BountyHunterRoleIcon.LoadAsset());
                    notif.AdjustNotification();
                }
            }
            else if (!isSolo)
            {
                 // Show notification for others
                 var notif = Helpers.CreateAndShowNotification(
                    TouLocale.GetParsed("ExtensionBHWinWithWinnersOthers").Replace("<player>", killer.Data?.PlayerName ?? "Unknown"),
                    Color.white, new Vector3(0f, 1f, -20f), spr: TouExtensionIcons.BountyHunterRoleIcon.LoadAsset());
                notif.AdjustNotification();
            }
        }
    }

}













