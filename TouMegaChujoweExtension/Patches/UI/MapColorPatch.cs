using HarmonyLib;
using MiraAPI.LocalSettings;
using MiraAPI.Roles;
using Reactor.Utilities;
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
        ApplyConfiguredMapColor(__instance);
        Coroutines.Start(CoKeepConfiguredMapColor(__instance));
    }

    private static System.Collections.IEnumerator CoKeepConfiguredMapColor(MapBehaviour map)
    {
        for (var i = 0; i < 30; i++)
        {
            if (map == null || !map.isActiveAndEnabled)
            {
                yield break;
            }

            ApplyConfiguredMapColor(map);
            yield return null;
        }
    }

    private static void ApplyConfiguredMapColor(MapBehaviour map)
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
                    ApplyRoleColor(map, roleColor);
                }
                break;

            case MapColorType.PlayerColor:
                ApplyPlayerColor(map, player);
                break;
        }
    }

    private static void ApplyRoleColor(MapBehaviour map, Color color)
    {
        map.ColorControl?.SetColor(color);
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
