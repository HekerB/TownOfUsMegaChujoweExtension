using HarmonyLib;
using TouMegaChujoweExtension.Modules;

namespace TouMegaChujoweExtension.Patches.Poisoner;

[HarmonyPatch]
public static class PoisonerReviveCleanupPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Revive))]
    public static void OnRevive(PlayerControl __instance)
    {
        PoisonDeathAnimSystem.RestoreBodyRenderers(__instance.PlayerId);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.RpcEndGame))]
    public static void OnGameEnd()
    {
        PoisonDeathAnimSystem.CleanupAll();
    }
}
