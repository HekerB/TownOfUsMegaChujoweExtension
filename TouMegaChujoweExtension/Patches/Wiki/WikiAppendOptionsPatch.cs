using System;
using HarmonyLib;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Options.Modifiers;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Modifiers.Game.Alliance;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Wiki;

[HarmonyPatch]
public static class WikiAppendOptionsPatch
{
    [HarmonyPatch(typeof(MiscUtils), nameof(MiscUtils.AppendOptionsText))]
    [HarmonyPostfix]
    public static void Postfix(Type classType, ref string __result)
    {
        try
        {
            if (classType == typeof(SonarRole))
            {
                var opts = OptionGroupSingleton<SonarExtendedOptions>.Instance;
                if (opts != null)
                {
                    // Format exactly like base mod: "Option Name: Option Value"
                    __result += $"\nBetter Sonar: {(opts.BetterSonar ? "True" : "False")}";
                    if (opts.BetterSonar)
                    {
                        __result += $"\nBetter Sonar Mode: {(opts.Mode == SonarDisplayMode.ArrowAndMap ? "Arrow + Map" : "Map Only")}";
                    }
                }
            }
            else if (classType == typeof(EgotistModifier))
            {
                var opts = OptionGroupSingleton<EgotistExtendedOptions>.Instance;
                if (opts != null)
                {
                    __result += $"\nCan Vent: {(opts.CanVent.Value ? "True" : "False")}";
                    if (opts.CanVent.Value)
                    {
                        __result += $"\nMax Vent Time: {opts.MaxVentTime.Value}s";
                        __result += $"\nVent Cooldown: {opts.VentCooldown.Value}s";
                    }
                    __result += $"\nImpostor Vision: {(opts.ImpostorVision.Value ? "True" : "False")}";
                }
            }
        }
        catch
        {
            // Ignore issues in formatting
        }
    }
}
