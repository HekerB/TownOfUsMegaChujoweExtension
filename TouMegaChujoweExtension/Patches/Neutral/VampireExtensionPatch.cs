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
    public static bool LocalVampireJustSabotaged = false;

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.RpcUpdateSystem), typeof(SystemTypes), typeof(byte))]
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
            if (amount == LightsSabotageId)
            {
                // Mark that we are the one who sabotaged
                LocalVampireJustSabotaged = true;
                return true;
            }
            
            // Block all other sabotages (Reactor, O2, Comms, etc.)
            return false;
        }

        // Block door systems if it's a door sabotage attempt
        if (systemType == SystemTypes.Doors)
        {
            return false;
        }

        return true;
    }
}
