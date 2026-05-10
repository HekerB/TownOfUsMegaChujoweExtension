using HarmonyLib;
using MiraAPI.GameOptions;
using System.Collections.Generic;
using TownOfUs.Modules.Components;

namespace TouMegaChujoweExtension.Patches.Roles;

[HarmonyPatch(typeof(CrimeSceneComponent))]
public static class ForensicContaminationPatch
{
    private static readonly HashSet<int> FrozenSceneIds = new();

    [HarmonyPatch(nameof(CrimeSceneComponent.FixedUpdate))]
    [HarmonyPrefix]
    public static bool FixedUpdate_Prefix(CrimeSceneComponent __instance)
    {
        if (FrozenSceneIds.Contains(__instance.gameObject.GetInstanceID()))
        {
            return false; // Skip contamination logic
        }

        // If a meeting is active, freeze the scene for future updates
        if (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started && MeetingHud.Instance != null)
        {
            var options = OptionGroupSingleton<ForensicExtensionOptions>.Instance;
            if (options.FreezeOnMeeting)
            {
                FrozenSceneIds.Add(__instance.gameObject.GetInstanceID());
                return false;
            }
        }

        return true;
    }

    [HarmonyPatch(nameof(CrimeSceneComponent.Clear))]
    [HarmonyPostfix]
    public static void Clear_Postfix()
    {
        FrozenSceneIds.Clear();
    }
    
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.OnDestroy))]
    [HarmonyPostfix]
    public static void ShipStatus_OnDestroy_Postfix()
    {
        FrozenSceneIds.Clear();
    }
}














