using HarmonyLib;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Extensions;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Impostor;

[HarmonyPatch]
public static class DumperPatches
{
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    public static void HudManagerUpdatePostfix()
    {
        if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started) return;
        DumperSystem.Update();
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.OnEnable))]
    [HarmonyPrefix]
    public static void ShipStatusOnEnablePrefix()
    {
        DumperSystem.Reset();
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPrefix]
    public static void MeetingHudStartPrefix()
    {
        DumperSystem.Reset();
    }
}
