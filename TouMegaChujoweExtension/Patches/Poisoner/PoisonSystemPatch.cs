using HarmonyLib;
using TouMegaChujoweExtension.Modules;

namespace TouMegaChujoweExtension.Patches.Poisoner;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class PoisonSystemUpdatePatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        PoisonSystem.Update();
    }
}

// REMOVED PoisonMeetingStartPatch

[HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
public static class PoisonExileWrapUpPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        PoisonSystem.RoundReset();
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
public static class PoisonGameEndPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        PoisonSystem.FullReset();
    }
}
