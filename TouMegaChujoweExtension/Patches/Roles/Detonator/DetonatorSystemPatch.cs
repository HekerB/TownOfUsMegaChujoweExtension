using HarmonyLib;
using TouMegaChujoweExtension.Modules;

namespace TouMegaChujoweExtension.Patches.Detonator;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class DetonatorSystemHudUpdatePatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        DetonatorSystem.Update();
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
public static class DetonatorSystemMeetingUpdatePatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        DetonatorSystem.MeetingUpdate();
    }
}

[HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
public static class DetonatorExileWrapUpPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        DetonatorSystem.RoundReset();
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
public static class DetonatorGameEndPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        DetonatorSystem.FullReset();
    }
}
