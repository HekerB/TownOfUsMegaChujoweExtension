using HarmonyLib;
using MiraAPI.LocalSettings;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Modules.Localization;
using TownOfUs.Options.Roles.Neutral;
using TownOfUs.Roles.Neutral;

namespace TouMegaChujoweExtension.Patches;

[HarmonyPatch]
public static class RenameDoomsayerToForeteller
{
    private static bool IsEnabled =>
        LocalSettingsTabSingleton<TouExtensionLocalSettings>.Instance.RenameDoomsayerToForeteller.Value;

    private static string ReplaceAll(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        return text
            .Replace("Doomsayer", "Foreteller")
            .Replace("doomsayer", "foreteller")
            .Replace("DOOMSAYER", "FORETELLER");
    }

    [HarmonyPatch(typeof(DoomsayerRole), nameof(DoomsayerRole.RoleName), MethodType.Getter)]
    [HarmonyPatch(typeof(DoomsayerOptions), nameof(DoomsayerOptions.GroupName), MethodType.Getter)]
    [HarmonyPostfix]
    public static void RoleName(ref string __result)
    {
        if (!IsEnabled) return;
        __result = "Foreteller";
    }

    [HarmonyPatch(typeof(DoomsayerRole), nameof(DoomsayerRole.RoleDescription), MethodType.Getter)]
    [HarmonyPatch(typeof(DoomsayerRole), nameof(DoomsayerRole.RoleLongDescription), MethodType.Getter)]
    [HarmonyPatch(typeof(DoomsayerRole), nameof(DoomsayerRole.GetAdvancedDescription))]
    [HarmonyPatch(typeof(TouLocale), nameof(TouLocale.Get), typeof(SupportedLangs), typeof(string), typeof(string))]
    [HarmonyPatch(typeof(TouLocale), nameof(TouLocale.GetParsed), typeof(SupportedLangs), typeof(string), typeof(string), typeof(System.Collections.Generic.Dictionary<string, string>))]
    [HarmonyPostfix]
    public static void RoleDescriptionAndLocale(ref string __result)
    {
        if (!IsEnabled) return;
        __result = ReplaceAll(__result);
    }
}
