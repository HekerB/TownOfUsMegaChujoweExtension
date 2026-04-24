using HarmonyLib;
using TouMegaChujoweExtension.Modules;

namespace TouMegaChujoweExtension.Patches;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
public static class MirageDecoyHostUpdatePatch
{
    [HarmonyPostfix]
    public static void FixedUpdatePostfix()
    {
        MirageDecoySystem.UpdateHost();
    }
}