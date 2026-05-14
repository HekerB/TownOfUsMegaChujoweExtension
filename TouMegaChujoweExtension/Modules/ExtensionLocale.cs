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
    // // internal static ManualLogSource LocaleLogger { get; } = BepInEx.Logging.Logger.CreateLogSource("ExtensionLocale");

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

        string targetFile = forcePolish ? "pl_PL.xml" : "en_US.xml";

        using var resourceStream = assembly.GetManifestResourceStream("TouMegaChujoweExtension.Resources.Locale." + targetFile);
        
        if (resourceStream == null)
        {
            // LocaleLogger.LogError($"File not found: {targetFile}");
            
            if (forcePolish)
            {
                using var fallbackStream = assembly.GetManifestResourceStream("TouMegaChujoweExtension.Resources.Locale.en_US.xml");
                if (fallbackStream != null) ForceInjectTranslations(fallbackStream);
            }
            return;
        }

        ForceInjectTranslations(resourceStream);
        // LocaleLogger.LogWarning($"Successfully loaded and overwritten translations from file: {targetFile}");
    }

    private static void ForceInjectTranslations(Stream stream)
    {
        try
        {
            using StreamReader reader = new(stream);
            string content = reader.ReadToEnd();

            XmlDocument xmlDoc = new XmlDocument();
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

                            if (!TouLocale.TouLocalization.ContainsKey(langEnum))
                            {
                                TouLocale.TouLocalization[langEnum] = new System.Collections.Generic.Dictionary<string, string>();
                            }

                            TouLocale.TouLocalization[langEnum][key] = value;
                        }
                    }
                }
            }
        }
        catch (System.Exception)
        {
            // LocaleLogger.LogError($"XML parsing error");
        }
    }
}












