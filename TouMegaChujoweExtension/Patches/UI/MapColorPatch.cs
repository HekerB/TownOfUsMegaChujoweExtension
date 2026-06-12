using HarmonyLib;
using MiraAPI.LocalSettings;
using MiraAPI.Roles;
using TownOfUs.Extensions;
using TownOfUs.Modules;
using TownOfUs.Roles;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.UI;

[HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowNormalMap))]
public static class MapColorPatch
{
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

        if (__instance.ColorControl == null)
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
                    __instance.ColorControl.SetColor(roleColor);
                }
                break;

            case MapColorType.PlayerColor:
                var cosmetics = player.cosmetics;
                if (cosmetics != null)
                {
                    var colorId = (int)cosmetics.ColorId;
                    if (colorId >= 0 && colorId < Palette.PlayerColors.Length)
                    {
                        __instance.ColorControl.SetColor(Palette.PlayerColors[colorId]);
                    }
                }
                break;
        }
    }
}
