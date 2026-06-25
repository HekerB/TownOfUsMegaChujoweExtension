using BepInEx.Logging;
using MiraAPI.LocalSettings;
using MiraAPI.Utilities;
using System.IO;
using System.Reflection;
using System.Xml;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Modules;

public static class ExtensionLocale
{
    private static readonly HashSet<string> AdditionalRoleNameKeys =
    [
        "ExtensionRoleEclipsal",
        "ExtensionRoleVampire",
        "TouRoleDoomsayer",
        "TouRoleSpellslinger",
    ];

    public static void SearchInternalLocale()
    {
        var assembly = Assembly.GetExecutingAssembly();

        bool forcePolish = false;
        bool translateRoleNames = false;
        try
        {
            if (LocalSettingsTabSingleton<TouExtensionLocalSettings>.Instance != null)
            {
                var settings = LocalSettingsTabSingleton<TouExtensionLocalSettings>.Instance;
                forcePolish = settings.UsePolishLanguage.Value;
                translateRoleNames = settings.TranslateRoleNames.Value;
            }
        }
        catch { }

        using (var enStream = assembly.GetManifestResourceStream("TouMegaChujoweExtension.Resources.Locale.en_US.xml"))
        {
            if (enStream != null)
            {
                ForceInjectTranslations(enStream, null);
            }
        }

        using (var plStream = assembly.GetManifestResourceStream("TouMegaChujoweExtension.Resources.Locale.pl_PL.xml"))
        {
            if (plStream != null)
            {
                if (forcePolish)
                {
                    ForceInjectTranslations(plStream, null);
                }
                else
                {
                    ForceInjectTranslations(plStream, (SupportedLangs)ExtendedLangs.Polish);
                }
            }
        }

        if (!translateRoleNames)
        {
            UseEnglishRoleNames(forcePolish ? null : (SupportedLangs)ExtendedLangs.Polish);
        }
    }

    private static void UseEnglishRoleNames(SupportedLangs? targetLang)
    {
        if (!TouLocale.TouLocalization.TryGetValue(SupportedLangs.English, out var englishTranslations))
        {
            return;
        }

        var roleNames = englishTranslations
            .Where(entry => IsRoleNameKey(entry.Key, englishTranslations))
            .ToArray();

        var targetLanguages = targetLang.HasValue
            ? TouLocale.TouLocalization.Where(entry => entry.Key == targetLang.Value)
            : TouLocale.TouLocalization;

        foreach (var translations in targetLanguages)
        {
            foreach (var roleName in roleNames)
            {
                translations.Value[roleName.Key] = roleName.Value;
            }
        }
    }

    private static bool IsRoleNameKey(string key, Dictionary<string, string> englishTranslations)
    {
        if (!key.StartsWith("TouRole", System.StringComparison.Ordinal) &&
            !key.StartsWith("ExtensionRole", System.StringComparison.Ordinal))
        {
            return false;
        }

        return AdditionalRoleNameKeys.Contains(key) ||
               englishTranslations.ContainsKey(key + "IntroBlurb") ||
               englishTranslations.ContainsKey(key + "TabDescription");
    }

    private static void ForceInjectTranslations(Stream stream, SupportedLangs? targetLang)
    {
        try
        {
            using StreamReader reader = new(stream);
            string content = reader.ReadToEnd();

            XmlDocument xmlDoc = new();
            xmlDoc.LoadXml(content);

            var nodes = xmlDoc.SelectNodes("//string");

            if (nodes == null || nodes.Count == 0)
            {
                nodes = xmlDoc.SelectNodes("//entry");
            }

            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    string? key = node.Attributes?["name"]?.Value ?? node.Attributes?["key"]?.Value;
                    string value = node.InnerText;

                    if (!string.IsNullOrEmpty(key))
                    {
                        if (targetLang.HasValue)
                        {
                            var langEnum = targetLang.Value;
                            if (!TouLocale.TouLocalization.TryGetValue(langEnum, out var dict))
                            {
                                dict = [];
                                TouLocale.TouLocalization[langEnum] = dict;
                            }
                            dict[key] = value;
                        }
                        else
                        {
                            foreach (var langKey in TouLocale.LangList.Keys)
                            {
                                var langEnum = (SupportedLangs)langKey;

                                if (!TouLocale.TouLocalization.TryGetValue(langEnum, out var dict))
                                {
                                    dict = [];
                                    TouLocale.TouLocalization[langEnum] = dict;
                                }

                                dict[key] = value;
                            }
                        }
                    }
                }
            }
        }
        catch (System.Exception)
        {
        }
    }
}

