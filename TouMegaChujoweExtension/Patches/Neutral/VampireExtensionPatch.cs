using HarmonyLib;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Neutral;

[HarmonyPatch]
public static class VampireExtensionPatch
{
    // sabotageId for Lights is 7 on most maps
    private const byte LightsSabotageId = 7;

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.RpcUpdateSystem))]
    [HarmonyPrefix]
    public static bool RpcUpdateSystemPrefix(SystemTypes systemType, byte amount)
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data.IsDead) return true;

        var role = player.GetRole<VampireRole>();
        if (role == null) return true;

        var options = OptionGroupSingleton<VampireExtendedOptions>.Instance;
        if (options == null || !options.CanOnlySabotageLights) return true;

        // If it's the Sabotage system, we only allow Lights (Id 7)
        if (systemType == SystemTypes.Sabotage)
        {
            if (amount == LightsSabotageId) return true;
            
            // Block all other sabotages (Reactor, O2, Comms, etc.)
            return false;
        }

        // Block door systems (SystemTypes.Doors is 1 on Skeld, but maps vary)
        // We block anything that isn't the sabotage system we just checked
        // because "Only Lights" means exactly that.
        
        // Note: Some maps use specific door systems, but they all inherit from SystemTypes or use RpcUpdateSystem.
        // We block them all for the Vampire with this option.
        return false;
    }
}
