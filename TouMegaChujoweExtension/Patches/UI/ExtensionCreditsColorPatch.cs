using System.Text.RegularExpressions;
using HarmonyLib;
using Reactor.Utilities;

namespace TouMegaChujoweExtension.Patches.UI;

[HarmonyPatch(typeof(ReactorCredits), "GetText")]
public static class ExtensionCreditsColorPatch
{
    private const string CreditsColor = "#96456d";
    private static readonly string[] KnownLabels =
    [
        TouMegaChujoweExtensionPlugin.UncensoredDisplayName + " " + TouMegaChujoweExtensionPlugin.Version,
        TouMegaChujoweExtensionPlugin.CensoredDisplayName + " " + TouMegaChujoweExtensionPlugin.Version
    ];

    private static void Postfix(ref string? __result)
    {
        if (string.IsNullOrEmpty(__result))
            return;

        var creditsLabel = TouMegaChujoweExtensionPlugin.DisplayName + " " + TouMegaChujoweExtensionPlugin.Version;
        var coloredLabel = $"<color={CreditsColor}>{creditsLabel}</color>";

        foreach (var knownLabel in KnownLabels)
        {
            var updated = Regex.Replace(
                __result,
                $@"<color=#[0-9A-Fa-f]{{3,8}}><noparse>{Regex.Escape(knownLabel)}</noparse></color>",
                coloredLabel);

            if (updated != __result)
            {
                __result = updated;
                return;
            }

            updated = __result.Replace($"<noparse>{knownLabel}</noparse>", coloredLabel);
            if (updated != __result)
            {
                __result = updated;
                return;
            }

            updated = __result.Replace(knownLabel, coloredLabel);
            if (updated != __result)
            {
                __result = updated;
                return;
            }
        }
    }
}
