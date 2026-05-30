using HarmonyLib;
using MiraAPI.Roles;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Patches.UI;

[HarmonyPatch]
public static class VisibleTextCensorPatch
{
    [HarmonyPatch(typeof(TouLocale), nameof(TouLocale.Get), typeof(SupportedLangs), typeof(string), typeof(string))]
    [HarmonyPostfix]
    public static void TouLocaleGetPostfix(ref string __result)
    {
        __result = TouMegaChujoweExtensionPlugin.CensorVisibleText(__result);
    }

    [HarmonyPatch(typeof(TouLocale), nameof(TouLocale.GetParsed), typeof(SupportedLangs), typeof(string), typeof(string), typeof(Dictionary<string, string>))]
    [HarmonyPostfix]
    public static void TouLocaleGetParsedPostfix(ref string __result)
    {
        __result = TouMegaChujoweExtensionPlugin.CensorVisibleText(__result);
    }
}
