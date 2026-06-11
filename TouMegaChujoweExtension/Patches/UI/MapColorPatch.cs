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
        if (localSettings == null || !localSettings.UseRoleColorForMap.Value)
        {
            return;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            return;
        }

        var role = player.GetRoleWhenAlive();
        if (role == null)
        {
            return;
        }

        var roleColor = role is ICustomRole custom ? custom.RoleColor : role.TeamColor;
        if (__instance.ColorControl != null)
        {
            __instance.ColorControl.SetColor(roleColor);
        }
    }
}
