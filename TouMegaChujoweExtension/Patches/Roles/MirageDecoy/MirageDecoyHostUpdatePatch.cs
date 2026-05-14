using HarmonyLib;

namespace TouMegaChujoweExtension.Patches.Roles.MirageDecoy;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
public static class MirageDecoyHostUpdatePatch
{
    [HarmonyPostfix]
    public static void FixedUpdatePostfix()
    {
        MirageDecoySystem.UpdateHost();
    }
}














