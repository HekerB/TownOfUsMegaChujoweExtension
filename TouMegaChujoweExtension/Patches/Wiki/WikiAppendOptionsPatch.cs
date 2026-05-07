using System;
using HarmonyLib;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Options.Modifiers;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles.Impostor;
using TownOfUs.Roles.Neutral;
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
                    __result += $"\nCan Vent: {(opts.CanVent ? "True" : "False")}";
                    if (opts.CanVent)
                    {
                        __result += $"\nMax Vent Time: {opts.MaxVentTime}s";
                        __result += $"\nVent Cooldown: {opts.VentCooldown}s";
                    }
                    __result += $"\nImpostor Vision: {(opts.ImpostorVision ? "True" : "False")}";
                }
            }
            else if (classType == typeof(ForensicRole))
            {
                var opts = OptionGroupSingleton<ForensicExtensionOptions>.Instance;
                if (opts != null)
                {
                    var title = TouLocale.Get("ExtensionOptionForensicFreezeOnMeeting");
                    __result += $"\n{title}: {(opts.FreezeOnMeeting ? "True" : "False")}";
                }
            }
            else if (classType == typeof(TimeLordRole))
            {
                var opts = OptionGroupSingleton<TimeLordExtensionOptions>.Instance;
                if (opts != null)
                {
                    var title = TouLocale.Get("ExtensionOptionTimeLordRewindSpeed");
                    __result += $"\n{title}: {opts.RewindSpeed}x";
                }
            }
            else if (classType == typeof(MayorRole))
            {
                var opts = OptionGroupSingleton<MayorExtensionOptions>.Instance;
                if (opts != null)
                {
                    var title = TouLocale.Get("ExtensionOptionMayorVoteCount");
                    __result += $"\n{title}: {opts.VoteCount}";
                }
            }
            else if (classType == typeof(MirrorcasterRole))
            {
                var opts = OptionGroupSingleton<MirrorCasterExtensionOptions>.Instance;
                if (opts != null)
                {
                    var title = TouLocale.Get("ExtensionOptionMirrorCasterMoveWhileMenu");
                    __result += $"\n{title}: {(opts.MoveWhileMenu ? "True" : "False")}";
                }
            }
            else if (classType == typeof(VampireRole))
            {
                var opts = OptionGroupSingleton<VampireExtendedOptions>.Instance;
                if (opts != null)
                {
                    __result += $"\n{TouLocale.Get("ExtensionOptionVampireCanOnlySabotageLights")}: {(opts.CanOnlySabotageLights ? "True" : "False")}";
                    if (opts.CanOnlySabotageLights)
                    {
                        __result += $"\n{TouLocale.Get("ExtensionOptionVampireOnlyOgCanSabotage")}: {(opts.OnlyOgCanSabotage ? "True" : "False")}";
                    }
                }
            }
        }
        catch
        {
            // Ignore issues in formatting
        }
    }
}
