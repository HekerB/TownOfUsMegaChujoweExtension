using AmongUs.GameOptions;
using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using System.Linq;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.UI;

[HarmonyPatch]
public static class RoleUIExtensionPatch
{
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data == null || player.Data.Role == null || player.Data.IsDead) return;

        var isImpostor = player.Data.Role.TeamType == RoleTeamTypes.Impostor;
        var vampireRole = player.GetRole<VampireRole>();

        if (vampireRole != null || isImpostor)
        {
            var options = OptionGroupSingleton<VampireExtendedOptions>.Instance;
            if (options != null && options.CanOnlySabotageLights)
            {
                // Hide the sabotage button on HUD for Vampires (they use TAB/Map)
                if (vampireRole != null && __instance.SabotageButton != null && __instance.SabotageButton.gameObject.activeSelf)
                {
                    __instance.SabotageButton.gameObject.SetActive(false);
                }
            }
        }
    }

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowNormalMap))]
    [HarmonyPrefix]
    public static bool ShowNormalMapPrefix(MapBehaviour __instance)
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data.IsDead) return true;

        var isImpostor = player.Data.Role.TeamType == RoleTeamTypes.Impostor;
        var vampireRole = player.GetRole<VampireRole>();
        if (vampireRole != null || isImpostor)
        {
            var options = OptionGroupSingleton<VampireExtendedOptions>.Instance;
            if (options != null && options.CanOnlySabotageLights)
            {
                // Don't redirect during meetings
                if (MeetingHud.Instance != null) return true;

                // Check "Only OG" restriction
                bool isBitten = player.GetModifiers<VampireBittenModifier>().Any();
                if (vampireRole != null && options.OnlyOgCanSabotage && isBitten) return true;

                // Always show sabotage map if they can sabotage, even if on cooldown (to see the timer)
                __instance.ShowSabotageMap();
                return false;
            }
        }
        return true;
    }

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowSabotageMap))]
    [HarmonyPostfix]
    public static void ShowSabotageMapPostfix(MapBehaviour __instance)
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data.IsDead) return;

        var isImpostor = player.Data.Role.TeamType == RoleTeamTypes.Impostor;
        var vampireRole = player.GetRole<VampireRole>();
        if (vampireRole == null && !isImpostor) return;

        var options = OptionGroupSingleton<VampireExtendedOptions>.Instance;
        if (options == null || !options.CanOnlySabotageLights) return;

        if (__instance.infectedOverlay == null || __instance.infectedOverlay.allButtons == null) return;

        foreach (var button in __instance.infectedOverlay.allButtons)
        {
            if (button == null || button.gameObject == null) continue;

            var name = button.gameObject.name.ToLower();
            bool isLights = name.Contains("electrical") || name.Contains("lights");
            
            if (!isLights && vampireRole != null) // Only hide for Vampires
            {
                button.gameObject.SetActive(false);
            }
            else if (isLights)
            {
                // Both Vampires and Impostors now share the global sabotage timer
                float currentTimer = 0f;
                if (ShipStatus.Instance != null && ShipStatus.Instance.Systems != null && ShipStatus.Instance.Systems.ContainsKey(SystemTypes.Sabotage))
                {
                    currentTimer = ShipStatus.Instance.Systems[SystemTypes.Sabotage].Cast<SabotageSystemType>().Timer;
                }

                var timerField = button.GetType().GetField("cooldownTimer", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                 ?? button.GetType().GetField("Timer", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (timerField != null)
                {
                    timerField.SetValue(button, currentTimer);
                }
            }
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    [HarmonyPostfix]
    public static void MeetingHudUpdatePostfix(MeetingHud __instance)
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data.IsDead) return;

        var vampireRole = player.GetRole<VampireRole>();
        if (vampireRole != null)
        {
            var options = OptionGroupSingleton<VampireExtendedOptions>.Instance;
            if (options != null && options.CanOnlySabotageLights)
            {
                // Find MapButton using reflection to enable normal map opening during meetings
                var mapButtonField = AccessTools.Field(typeof(MeetingHud), "MapButton");
                var mapButtonObj = mapButtonField?.GetValue(__instance) as Component;
                
                if (mapButtonObj != null)
                {
                    mapButtonObj.gameObject.SetActive(true);
                    var passiveButton = mapButtonObj.GetComponent<PassiveButton>();
                    if (passiveButton != null)
                    {
                        passiveButton.enabled = true;
                    }
                }
            }
        }
    }
}













