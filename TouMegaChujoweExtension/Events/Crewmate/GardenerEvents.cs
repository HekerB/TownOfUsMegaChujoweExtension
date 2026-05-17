using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using TouMegaChujoweExtension.Buttons.Classic.Crewmate;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TouMegaChujoweExtension.Modules;

namespace TouMegaChujoweExtension.Events.Crewmate;

public static class GardenerEvents
{
    [RegisterEvent]
    public static void CompleteTaskEvent(CompleteTaskEvent @event)
    {
        if (@event.Player.AmOwner && @event.Player.Data.Role is GardenerRole &&
            OptionGroupSingleton<GardenerOptions>.Instance.TaskUses &&
            !OptionGroupSingleton<GardenerOptions>.Instance.TrapsRemoveOnNewRound)
        {
            var button = CustomButtonSingleton<GardenerGardenButton>.Instance;
            if (button != null)
            {
                ++button.UsesLeft;
                button.SetUses(button.UsesLeft);
            }
        }
    }

    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (@event.TriggeredByIntro)
        {
            if (PlayerControl.LocalPlayer?.Data?.Role is GardenerRole)
            {
                var maxUses = OptionGroupSingleton<GardenerOptions>.Instance.MaxTraps;
                CustomButtonSingleton<GardenerGardenButton>.Instance.SetUses((int)maxUses);
            }
            return;
        }

        if (OptionGroupSingleton<GardenerOptions>.Instance.TrapsRemoveOnNewRound)
        {
            GardenerSystem.ClearAll();

            if (PlayerControl.LocalPlayer?.Data?.Role is GardenerRole)
            {
                var maxUses = OptionGroupSingleton<GardenerOptions>.Instance.MaxTraps;
                CustomButtonSingleton<GardenerGardenButton>.Instance.SetUses((int)maxUses);
            }
        }
    }
}
