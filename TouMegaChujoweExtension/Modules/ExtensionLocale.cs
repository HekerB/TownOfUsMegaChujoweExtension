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

