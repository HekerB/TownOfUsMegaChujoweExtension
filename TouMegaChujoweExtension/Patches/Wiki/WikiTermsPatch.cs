using HarmonyLib;
using MiraAPI.GameOptions;
using System.Collections.Generic;
using System;
using TownOfUs.Assets;
using TownOfUs.Modules.Wiki;
using TownOfUs.Options;
using TownOfUs.Options.Maps;
using TouMegaChujoweExtension.Options.Roles.Neutral;

namespace TouMegaChujoweExtension.Patches.Wiki;

[HarmonyPatch(typeof(IngameWikiMinigame), nameof(IngameWikiMinigame.AddNewTerms))]
public static class WikiTermsPatch
{
    [HarmonyPostfix]
    public static void Postfix(IngameWikiMinigame __0)
    {
        try
        {
            __0._activeTerms.Add(new TermWikiInfo(
                "TOUMCETermsSymbolsTitle",
                "TOUMCETermsSymbolsInfo",
                TouRoleIcons.Lawyer));

            __0._activeTerms.Add(new TermWikiInfo(
                "TOUMCETermsShieldFlashesTitle",
                "TOUMCETermsShieldFlashesInfo",
                TouRoleIcons.Medic));

            __0._activeTerms.Add(new TermWikiInfo(
                "TOUMCETermsDraftModeTitle",
                "TOUMCETermsDraftModeInfo",
                TouRoleIcons.Traitor));

            __0._activeTerms.Add(new TermWikiInfo(
                "TOUMCETermsApocalypseTitle",
                "TOUMCETermsApocalypseInfo",
                TouRoleIcons.Pestilence));

            __0._activeTerms.Add(new TermWikiInfo(
                "TOUMCETermsDraftFactionsTitle",
                "TOUMCETermsDraftFactionsInfo",
                TouRoleIcons.Jackal));

            Info("TOUMCE wiki terms added successfully");
        }
        catch (Exception e)
        {
            Error($"Failed to add TOUMCE wiki terms: {e}");
        }
    }
}

[HarmonyPatch(typeof(IngameWikiMinigame), nameof(IngameWikiMinigame.AddNewSettings))]
public static class WikiSettingsPatch
{
    [HarmonyPostfix]
    public static void Postfix(IngameWikiMinigame __0)
    {
        try
        {
            var draftGroupList = new List<MiraAPI.GameOptions.AbstractOptionGroup>();
            var draftOpt = OptionGroupSingleton<TouMegaChujoweExtension.Options.DraftModeOptions>.Instance;
            if (draftOpt != null)
            {
                draftGroupList.Add(draftOpt);
            }

            if (draftGroupList.Count > 0)
            {
                __0._activeSettings.Add(new OptionWikiInfo(
                    "TOUMCETermsDraftModeTitle",
                    draftGroupList,
                    TouExtensionIcons.HackerRole,
                    false));
            }

            var extensionGroupList = new List<MiraAPI.GameOptions.AbstractOptionGroup>();
            void AddIfNotNull<T>() where T : MiraAPI.GameOptions.AbstractOptionGroup
            {
                var inst = OptionGroupSingleton<T>.Instance;
                if (inst != null)
                {
                    extensionGroupList.Add(inst);
                }
            }
            AddIfNotNull<EgotistExtendedOptions>();
            AddIfNotNull<ForensicExtensionOptions>();
            AddIfNotNull<MayorExtensionOptions>();
            AddIfNotNull<SonarExtendedOptions>();
            AddIfNotNull<TimeLordExtensionOptions>();
            AddIfNotNull<AdvancedSabotageOptions>();
            AddIfNotNull<VampireExtendedOptions>();
            AddIfNotNull<JackalOptions>();








            if (extensionGroupList.Count > 0)
            {
                __0._activeSettings.Add(new OptionWikiInfo(
                    "TOUMCETermsRoleExtensionsTitle",
                    extensionGroupList,
                    TouRoleIcons.Engineer,
                    false));
            }

            Info("TOUMCE draft and role extension settings added to wiki successfully");
        }
        catch (Exception e)
        {
            Error($"Failed to add TOUMCE draft settings to wiki: {e}");
        }
    }
}
