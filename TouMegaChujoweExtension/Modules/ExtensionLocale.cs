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


    public static void SearchInternalLocale()
    {
        var assembly = Assembly.GetExecutingAssembly();

        bool forcePolish = false;
        try
        {
            if (LocalSettingsTabSingleton<TouExtensionLocalSettings>.Instance != null)
            {
                forcePolish = LocalSettingsTabSingleton<TouExtensionLocalSettings>.Instance.UsePolishLanguage.Value;
            }
        }
        catch { /* Fallback */ }

        string resourceName = forcePolish
            ? "TouMegaChujoweExtension.Resources.Locale.pl_PL.xml"
            : "TouMegaChujoweExtension.Resources.Locale.en_US.xml";
        using var resourceStream = assembly.GetManifestResourceStream(resourceName);

        if (resourceStream == null && forcePolish)
        {
            using var fallbackStream = assembly.GetManifestResourceStream("TouMegaChujoweExtension.Resources.Locale.en_US.xml");
            if (fallbackStream != null)
            {
                ForceInjectTranslations(fallbackStream);
            }

            return;
        }

        if (resourceStream != null)
        {
            ForceInjectTranslations(resourceStream);
        }
    }

    private static void ForceInjectTranslations(Stream stream)
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
        catch (System.Exception)
        {
            // Ignore parsing errors; translations will fall back to default game text
        }
    }
}
