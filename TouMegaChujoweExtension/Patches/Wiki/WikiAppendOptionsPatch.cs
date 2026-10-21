using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.GameOptions.OptionTypes;
using AmongUs.Data;
using System;
using System.Collections.Generic;
using TownOfUs.Modifiers.Game.Alliance;
using TownOfUs.Modules.Localization;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles.Impostor;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TownOfUs.Modules.Wiki;

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
            else if (classType == typeof(SidekickModifier))
            {
                // Clear default "Amount: X, Chance: Y" strings for Recruit
                __result = "";
            }
            else if (classType == typeof(ArcanistRole))
            {
                var options = MiscUtils.GetModdedOptionsForRole(typeof(ArcanistRole));
                if (options != null)
                {
                    var builder = new System.Text.StringBuilder();
                    builder.AppendLine(TownOfUs.TownOfUsPlugin.Culture,
                        $"\n<size=50%> \n</size><b>{TownOfUs.TownOfUsColors.Vigilante.ToTextColor()}{TouLocale.Get("Options")}</color></b>");

                    var generalOptions = new List<string>();

                    foreach (var option in options)
                    {
                        if (option == null) continue;

                        string title = "";
                        string valueStr = "";

                        switch (option)
                        {
                            case ModdedToggleOption toggleOption:
                                if (!toggleOption.Visible()) continue;
                                title = TranslationController.Instance.GetString(toggleOption.StringName);
                                valueStr = toggleOption.Value ? "True" : "False";
                                break;
                            case ModdedEnumOption enumOption:
                                if (!enumOption.Visible()) continue;
                                title = TranslationController.Instance.GetString(enumOption.StringName);
                                valueStr = TouLocale.GetParsed(enumOption.Values[enumOption.Value], enumOption.Values[enumOption.Value]);
                                break;
                            case ModdedNumberOption numberOption:
                                if (!numberOption.Visible()) continue;
                                title = TranslationController.Instance.GetString(numberOption.StringName);
                                var optionStr = numberOption.Data.GetValueString(numberOption.Value);
                                if (optionStr.Contains(".000")) optionStr = optionStr.Replace(".000", "");
                                else if (optionStr.Contains(".00")) optionStr = optionStr.Replace(".00", "");
                                else if (optionStr.Contains(".0")) optionStr = optionStr.Replace(".0", "");

                                if (numberOption is { NegativeWordValue: not "#", Value: -1 })
                                {
                                    valueStr = numberOption.NegativeWordValue;
                                }
                                else if (numberOption is { ZeroWordValue: not "#", Value: 0 })
                                {
                                    valueStr = numberOption.ZeroWordValue;
                                }
                                else
                                {
                                    valueStr = optionStr;
                                }
                                break;
                        }

                        if (string.IsNullOrEmpty(title)) continue;

                        var cardIdentifiers = new[] {
                            "Fool", "Głupiec", "Magician", "Magik", "Priestess", "Arcykapłanka",
                            "Empress", "Cesarzowa", "Emperor", "Cesarz", "Hierophant", "Hierofant",
                            "Lovers", "Kochankowie", "Chariot", "Rydwan", "Strength", "Siła",
                            "Hermit", "Pustelnik", "Wheel", "Koło", "Justice", "Sprawiedliwość",
                            "Hanged", "Wisielec", "Death", "Śmierć", "Temperance", "Umiarkowanie",
                            "Devil", "Diabeł", "Tower", "Wieża", "Star", "Gwiazda", "Moon", "Księżyc",
                            "Sun", "Słońce", "Judgement", "Sąd", "World", "Świat"
                        };

                        bool isWeight = System.Linq.Enumerable.Any(cardIdentifiers, id => title.Contains(id, StringComparison.OrdinalIgnoreCase));

                        if (!isWeight)
                        {
                            generalOptions.Add($"{title}: {valueStr}");
                        }
                    }

                    foreach (var opt in generalOptions)
                    {
                        builder.AppendLine(opt);
                    }

                    __result = builder.ToString();
                }
            }
            /* Jackal options are handled automatically because they are registered as AbstractOptionGroup<JackalRole> */
        }
        catch
        {
            // Ignore issues in formatting
        }
    }
}

[HarmonyPatch(typeof(InGameModifierWikiEntry), nameof(InGameModifierWikiEntry.SetData))]
public static class WikiModifierEntryPatch
{
    [HarmonyPostfix]
    public static void Postfix(InGameModifierWikiEntry __instance)
    {
        if (__instance.Modifier is SidekickModifier)
        {
            if (__instance.EntryAmountTmp != null && __instance.EntryAmountTmp.Value != null)
            {
                __instance.EntryAmountTmp.Value.text = "";
            }
        }
    }
}















