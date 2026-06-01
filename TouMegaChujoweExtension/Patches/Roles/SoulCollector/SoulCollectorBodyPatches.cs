using HarmonyLib;
using TouMegaChujoweExtension.Modules;

namespace TouMegaChujoweExtension.Patches.Roles.SoulCollector;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class SoulCollectorBodyPatches
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        SoulCollectorSystem.UpdateDeathBodies();
    }
}
