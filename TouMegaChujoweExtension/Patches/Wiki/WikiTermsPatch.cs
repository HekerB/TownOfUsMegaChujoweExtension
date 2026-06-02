using HarmonyLib;
using MiraAPI.GameOptions;
using System.Collections.Generic;
using System.Reflection;
using System;
using TownOfUs.Assets;
using TownOfUs.Options.Maps;
using TownOfUs.Options;
using TouMegaChujoweExtension.Options.Roles.Neutral;

namespace TouMegaChujoweExtension.Patches.Wiki;

[HarmonyPatch]
public static class WikiTermsPatch
{
    public static MethodInfo TargetMethod()
    {
        var wikiType = AccessTools.TypeByName("TownOfUs.Modules.Wiki.IngameWikiMinigame");
        return AccessTools.Method(wikiType, "AddNewTerms");
    }

    [HarmonyPostfix]
    public static void Postfix(object __0)
    {
        try
        {
            var wikiType = AccessTools.TypeByName("TownOfUs.Modules.Wiki.IngameWikiMinigame");
            var termType = AccessTools.TypeByName("TownOfUs.Modules.Wiki.TermWikiInfo");

            if (wikiType == null || termType == null)
            {
                Warning("Wiki types not found - skipping TOUMCE terms");
                return;
            }

            var termsField = AccessTools.Field(wikiType, "_activeTerms");
            var termsList = termsField?.GetValue(__0);
            var addMethod = termsList?.GetType().GetMethod("Add");

            if (addMethod == null)
            {
                Warning("_activeTerms.Add not found");
                return;
            }

            // Page 1: Symbols
            var symbolsTerm = Activator.CreateInstance(termType,
                "TOUMCETermsSymbolsTitle",
                "TOUMCETermsSymbolsInfo",
                (object)TouRoleIcons.Lawyer);
            addMethod.Invoke(termsList, [symbolsTerm]);

            // Page 2: Shield Flashes
            var flashesTerm = Activator.CreateInstance(termType,
                "TOUMCETermsShieldFlashesTitle",
                "TOUMCETermsShieldFlashesInfo",
                (object)TouRoleIcons.Medic);
            addMethod.Invoke(termsList, [flashesTerm]);

            // Page 3: Draft Mode
            var draftModeTerm = Activator.CreateInstance(termType,
                "TOUMCETermsDraftModeTitle",
                "TOUMCETermsDraftModeInfo",
                (object)TouRoleIcons.Traitor);
            addMethod.Invoke(termsList, [draftModeTerm]);

            // Page 4: Apocalypse
            var apocalypseTerm = Activator.CreateInstance(termType,
                "TOUMCETermsApocalypseTitle",
                "TOUMCETermsApocalypseInfo",
                (object)TouRoleIcons.Pestilence);
            addMethod.Invoke(termsList, [apocalypseTerm]);

            // Page 5: Draft Factions
            var draftFactionsTerm = Activator.CreateInstance(termType,
                "TOUMCETermsDraftFactionsTitle",
                "TOUMCETermsDraftFactionsInfo",
                (object)TouRoleIcons.Jackal);
            addMethod.Invoke(termsList, [draftFactionsTerm]);

            Info("TOUMCE wiki terms added successfully");
        }
        catch (Exception e)
        {
            Error($"Failed to add TOUMCE wiki terms: {e}");
        }
    }
}

[HarmonyPatch]
public static class WikiSettingsPatch
{
    public static MethodInfo TargetMethod()
    {
        var wikiType = AccessTools.TypeByName("TownOfUs.Modules.Wiki.IngameWikiMinigame");
        return AccessTools.Method(wikiType, "AddNewSettings");
    }

    [HarmonyPostfix]
    public static void Postfix(object __0)
    {
        try
        {
            var wikiType = AccessTools.TypeByName("TownOfUs.Modules.Wiki.IngameWikiMinigame");
            var infoType = AccessTools.TypeByName("TownOfUs.Modules.Wiki.OptionWikiInfo");

            if (wikiType == null || infoType == null) return;

            var settingsField = AccessTools.Field(wikiType, "_activeSettings");
            var settingsList = settingsField?.GetValue(__0);
            var addMethod = settingsList?.GetType().GetMethod("Add");

            if (addMethod == null) return;

            // Create List<AbstractOptionGroup>
            var draftGroupList = new List<MiraAPI.GameOptions.AbstractOptionGroup>
            {
                MiraAPI.GameOptions.OptionGroupSingleton<TouMegaChujoweExtension.Options.DraftModeOptions>.Instance
            };

            // Create OptionWikiInfo for Draft
            var draftSettings = Activator.CreateInstance(infoType,
                "TOUMCETermsDraftModeTitle",
                draftGroupList,
                (object)TouExtensionIcons.HackerRole,
                false);

            addMethod.Invoke(settingsList, [draftSettings]);

            // Create List<AbstractOptionGroup> for Role Extensions
            var extensionGroupList = new List<MiraAPI.GameOptions.AbstractOptionGroup>
            {
                MiraAPI.GameOptions.OptionGroupSingleton<EgotistExtendedOptions>.Instance,
                MiraAPI.GameOptions.OptionGroupSingleton<ForensicExtensionOptions>.Instance,
                MiraAPI.GameOptions.OptionGroupSingleton<MayorExtensionOptions>.Instance,
                MiraAPI.GameOptions.OptionGroupSingleton<SonarExtendedOptions>.Instance,
                MiraAPI.GameOptions.OptionGroupSingleton<TimeLordExtensionOptions>.Instance,
                MiraAPI.GameOptions.OptionGroupSingleton<AdvancedSabotageOptions>.Instance,
                MiraAPI.GameOptions.OptionGroupSingleton<VampireExtendedOptions>.Instance,
                MiraAPI.GameOptions.OptionGroupSingleton<JackalOptions>.Instance
            };

            // Create OptionWikiInfo for Role Extensions
            var roleExtensionsSettings = Activator.CreateInstance(infoType,
                "TOUMCETermsRoleExtensionsTitle",
                extensionGroupList,
                (object)TouRoleIcons.Engineer,
                false);

            addMethod.Invoke(settingsList, [roleExtensionsSettings]);

            Info("TOUMCE draft and role extension settings added to wiki successfully");
        }
        catch (Exception e)
        {
            Error($"Failed to add TOUMCE draft settings to wiki: {e}");
        }
    }
}
