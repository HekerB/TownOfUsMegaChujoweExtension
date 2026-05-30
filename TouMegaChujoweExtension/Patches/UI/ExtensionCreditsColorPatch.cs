using System.Text.RegularExpressions;
using HarmonyLib;
using Reactor.Utilities;

namespace TouMegaChujoweExtension.Patches.UI;

[HarmonyPatch(typeof(ReactorCredits), "GetText")]
public static class ExtensionCreditsColorPatch
{
    private const string CreditsColor = "#96456d";
    private static readonly string CreditsLabel = "Tou Mega Chujowe Extension " + TouMegaChujoweExtensionPlugin.Version;

    private static void Postfix(ref string? __result)
    {
        if (string.IsNullOrEmpty(__result))
            return;

        var coloredLabel = $"<color={CreditsColor}><noparse>{CreditsLabel}</noparse></color>";
        var updated = Regex.Replace(
            __result,
            $@"<color=#[0-9A-Fa-f]{{3,8}}><noparse>{Regex.Escape(CreditsLabel)}</noparse></color>",
            coloredLabel);

        if (updated == __result)
            updated = __result.Replace($"<noparse>{CreditsLabel}</noparse>", coloredLabel);

        if (updated == __result)
            updated = __result.Replace(CreditsLabel, coloredLabel);

        __result = updated;
    }
}
