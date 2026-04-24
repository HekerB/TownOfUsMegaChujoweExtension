using System;
using System.Reflection;
using HarmonyLib;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Assets;

namespace TouMegaChujoweExtension.Patches.Wiki;

[HarmonyPatch]
public static class WikiTermsPatch
{
    private static MethodBase TargetMethod()
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
            addMethod.Invoke(termsList, new[] { symbolsTerm });

            // Page 2: Shield Flashes
            var flashesTerm = Activator.CreateInstance(termType,
                "TOUMCETermsShieldFlashesTitle",
                "TOUMCETermsShieldFlashesInfo",
                (object)TouRoleIcons.Medic);
            addMethod.Invoke(termsList, new[] { flashesTerm });

            // Page 3: Draft Mode
            var draftModeTerm = Activator.CreateInstance(termType,
                "TOUMCETermsDraftModeTitle",
                "TOUMCETermsDraftModeInfo",
                (object)TouRoleIcons.Traitor);
            addMethod.Invoke(termsList, new[] { draftModeTerm });

            // Page 4: Draft Factions
            var draftFactionsTerm = Activator.CreateInstance(termType,
                "TOUMCETermsDraftFactionsTitle",
                "TOUMCETermsDraftFactionsInfo",
                (object)TouRoleIcons.Jackal);
            addMethod.Invoke(termsList, new[] { draftFactionsTerm });

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
    private static MethodBase TargetMethod()
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
            var groupList = new List<MiraAPI.GameOptions.AbstractOptionGroup>
            {
                MiraAPI.GameOptions.OptionGroupSingleton<TouMegaChujoweExtension.Options.DraftModeOptions>.Instance
            };

            // Create OptionWikiInfo
            // Constructor: (string Title, List<AbstractOptionGroup> OptionGroups, LoadableAsset<Sprite> DefaultIcon, bool IsVanilla = false)
            var draftSettings = Activator.CreateInstance(infoType,
                "TOUMCETermsDraftModeTitle",
                groupList,
                (object)TouExtensionIcons.HackerRole,
                false);

            addMethod.Invoke(settingsList, new[] { draftSettings });

            Info("TOUMCE draft settings added to wiki successfully");
        }
        catch (Exception e)
        {
            Error($"Failed to add TOUMCE draft settings to wiki: {e}");
        }
    }
}
