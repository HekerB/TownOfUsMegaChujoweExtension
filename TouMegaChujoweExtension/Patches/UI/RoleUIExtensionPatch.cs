using HarmonyLib;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using TownOfUs.Extensions;
using MiraAPI.Utilities;
using MiraAPI.Modifiers;
using UnityEngine;
using System.Linq;

namespace TouMegaChujoweExtension.Patches.UI;

[HarmonyPatch]
public static class RoleUIExtensionPatch
{
    public static float VampireSabotageTimer = 0f;
    private static bool _lastLightsWorking = true;
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data.IsDead) return;

        if (VampireSabotageTimer > 0f)
            VampireSabotageTimer -= Time.deltaTime;

        // Detect Electrical Sabotage State
        if (ShipStatus.Instance != null)
        {
            var electrical = ShipStatus.Instance.Systems?.ContainsKey(SystemTypes.Electrical) == true 
                ? ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>() 
                : null;
                
            if (electrical != null)
            {
                bool isWorking = electrical.IsActive;
                
                // Sabotage FIXED! Start cooldown now
                if (isWorking && !_lastLightsWorking)
                {
                    var options = OptionGroupSingleton<VampireExtendedOptions>.Instance;
                    if (options != null)
                    {
                        VampireSabotageTimer = options.SabotageCooldown;
                    }
                }
                
                _lastLightsWorking = isWorking;
            }
        }

        // Vampire: Hide the sabotage button on HUD to prevent bugs, rely on TAB instead
        var vampireRole = player.GetRole<VampireRole>();
        if (vampireRole != null)
        {
            // Force global sabotage timer to zero if our custom timer is finished
            if (VampireSabotageTimer <= 0f && ShipStatus.Instance != null)
            {
                var sabotageSystem = ShipStatus.Instance.Systems[SystemTypes.Sabotage].Cast<SabotageSystemType>();
                if (sabotageSystem.Timer > 0f)
                {
                    sabotageSystem.Timer = 0f;
                }
            }

            var options = OptionGroupSingleton<VampireExtendedOptions>.Instance;
            if (options != null && options.CanOnlySabotageLights)
            {
                if (__instance.SabotageButton != null && __instance.SabotageButton.gameObject.activeSelf)
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

        var vampireRole = player.GetRole<VampireRole>();
        if (vampireRole != null)
        {
            var options = OptionGroupSingleton<VampireExtendedOptions>.Instance;
            if (options != null && options.CanOnlySabotageLights)
            {
                // Don't redirect during meetings, show normal map
                if (MeetingHud.Instance != null) return true;

                // Check Global Sabotage Status (Ignore Timer > 0 for Vampire to respect their own cooldown)
                var sabotageSystem = ShipStatus.Instance.Systems[SystemTypes.Sabotage].Cast<SabotageSystemType>();
                bool globalActive = sabotageSystem.AnyActive;

                // Check Cooldown or "Only OG" restriction
                bool canSabotage = VampireSabotageTimer <= 0f && !globalActive;
                if (options.OnlyOgCanSabotage && player.GetModifiers<VampireBittenModifier>().Any())
                {
                    canSabotage = false;
                }

                if (canSabotage)
                {
                    __instance.ShowSabotageMap();
                    return false;
                }
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

        var vampireRole = player.GetRole<VampireRole>();
        if (vampireRole == null) return;

        var options = OptionGroupSingleton<VampireExtendedOptions>.Instance;
        if (options == null || !options.CanOnlySabotageLights) return;

        if (__instance.infectedOverlay == null || __instance.infectedOverlay.allButtons == null) return;

        foreach (var button in __instance.infectedOverlay.allButtons)
        {
            if (button == null || button.gameObject == null) continue;

            var name = button.gameObject.name.ToLower();
            bool isLights = name.Contains("electrical") || name.Contains("lights");
            
            if (!isLights)
            {
                button.gameObject.SetActive(false);
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
