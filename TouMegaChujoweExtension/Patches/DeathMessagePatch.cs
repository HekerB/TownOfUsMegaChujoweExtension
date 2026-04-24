using HarmonyLib;
using TouMegaChujoweExtension.Modules;

namespace TouMegaChujoweExtension.Patches;

/// <summary>
/// Universal patch - checks DeathMessageRegistry after every kill. 
/// One patch for ALL roles in the extension. Though idk if it works lol
/// </summary>
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
public static class DeathMessagePatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(PlayerControl __instance, PlayerControl target)
    {
        DeathMessageRegistry.HandleMurder(__instance, target);
    }
}