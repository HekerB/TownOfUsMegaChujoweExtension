using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;

namespace TouMegaChujoweExtension.Events.Crewmate;

public static class SpiritMasterEvents
{
    [RegisterEvent]
    public static void EjectionEventHandler(EjectionEvent @event)
    {
        if (PlayerControl.LocalPlayer.Data.Role is SpiritMasterRole spiritMaster)
        {
            spiritMaster.MediatedPlayers.ForEach(mediated => mediated.Player?.RpcRemoveModifier(mediated.UniqueId));
        }
    }
}
