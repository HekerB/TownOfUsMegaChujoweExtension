using MiraAPI.Events.Vanilla.Usables;
using MiraAPI.Events;
using MiraAPI.Modifiers;

namespace TouMegaChujoweExtension.Patches.Roles.SerialKiller;

public static class SerialKillerVentBlockPatch
{
    [RegisterEvent]
    public static void PlayerCanUseEventHandler(PlayerCanUseEvent @event)
    {
        if (!@event.IsVent)
        {
            return;
        }

        if (PlayerControl.LocalPlayer == null)
        {
            return;
        }

        if (PlayerControl.LocalPlayer.HasModifier<SerialKillerNoVentModifier>())
        {
            @event.Cancel();
        }
    }
}


















