using HarmonyLib;
using MiraAPI.LocalSettings;
using MiraAPI.Roles;
using TownOfUs.Extensions;
using TownOfUs.Modules;
using TownOfUs.Roles;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.UI;

[HarmonyPatch]
public static class MapColorPatch
{
    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowNormalMap))]
    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowSabotageMap))]
    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowCountOverlay))]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPostfix]
    public static void Postfix(MapBehaviour __instance)
    {
        var localSettings = LocalSettingsTabSingleton<TouExtensionLocalSettings>.Instance;
        if (localSettings == null)
        {
            return;
        }

        var mode = localSettings.MapColor.Value;
        if (mode == MapColorType.Off)
        {
            return;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            return;
        }

        switch (mode)
        {
            case MapColorType.Role:
                var role = player.GetRoleWhenAlive();
                if (role != null)
                {
                    var roleColor = role is ICustomRole custom ? custom.RoleColor : role.TeamColor;
                    ApplyRoleColor(__instance, roleColor);
                }
                break;

            case MapColorType.PlayerColor:
                ApplyPlayerColor(__instance, player);
                break;
        }
    }

    private static void ApplyRoleColor(MapBehaviour map, Color color)
    {
        map.ColorControl?.SetColor(color);
        ApplySolidColor(map.HerePoint, color);
        ApplySolidColor(map.TrackedHerePoint, color);
    }

    private static void ApplyPlayerColor(MapBehaviour map, PlayerControl player)
    {
        var colorId = player.cosmetics != null ? (int)player.cosmetics.ColorId : -1;
        if (colorId < 0 || colorId >= Palette.PlayerColors.Length)
        {
            return;
        }

        map.ColorControl?.SetColor(Palette.PlayerColors[colorId]);

        if (map.HerePoint != null)
        {
            player.SetPlayerMaterialColors(map.HerePoint);
        }

        if (map.TrackedHerePoint != null)
        {
            player.SetPlayerMaterialColors(map.TrackedHerePoint);
        }
    }

    private static void ApplySolidColor(SpriteRenderer? renderer, Color color)
    {
        if (renderer == null)
        {
            return;
        }

        PlayerMaterial.SetColors(color, renderer);
        renderer.color = color;
    }
}

[HarmonyPatch(typeof(TownOfUs.Patches.MapBehaviourPatch), nameof(TownOfUs.Patches.MapBehaviourPatch.Postfix))]
public static class MapBehaviourPatchDisablePatch
{
    [HarmonyPrefix]
    public static bool Prefix()
    {
        var localSettings = LocalSettingsTabSingleton<TouExtensionLocalSettings>.Instance;
        if (localSettings != null && localSettings.MapColor.Value != MapColorType.Off)
        {
            return false;
        }
        return true;
    }
}
