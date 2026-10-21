using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using MiraAPI.Translation;

namespace TouMegaChujoweExtension.Modules;

/// <summary>
/// Loads the extension's strings into MiraAPI's locale tables, which TOU-Mira 1.7.3 reads through
/// <see cref="MiraLocaleManager"/> (TouLocale is only a shim over it now).
/// <para>
/// English goes into every language as the base, Polish into Polish - or into every language when
/// "Use Polish" is on. With "Translate Role Names" off, role names are put back to English afterwards.
/// Every value the extension overwrites is remembered the first time, so toggling either setting at
/// runtime starts again from the untouched tables instead of from the previous run's leftovers.
/// </para>
/// </summary>
public static class ExtensionLocale
{
    private const string EnglishResource = "TouMegaChujoweExtension.Resources.Locale.en_US.xml";
    private const string PolishResource = "TouMegaChujoweExtension.Resources.Locale.pl_PL.xml";

    private const string TouRolePrefix = "TownOfUsMira.Role.";

    private static readonly HashSet<string> AdditionalRoleNameKeys =
    [
        "ExtensionRoleEclipsal",
        "ExtensionRoleVampire",
        "TownOfUsMira.Role.Doomsayer",
        "TownOfUsMira.Role.Spellslinger",
    ];

    // The extension renamed TOU's Trapper to Revealer under keys TOU never had.
    private static readonly Dictionary<string, string[]> ManualAliases = new()
    {
        ["TouRoleRevealer"] = ["TownOfUsMira.Role.Trapper"],
        ["TouRoleRevealerIntroBlurb"] = ["TownOfUsMira.Role.Trapper.IntroBlurb", "TownOfUsMira.Role.Trapper.MedDescription"],
        ["TouRoleRevealerTabDescription"] = ["TownOfUsMira.Role.Trapper.TabDescription"],
        ["TouRoleRevealerWikiDescription"] = ["TownOfUsMira.Role.Trapper.WikiDescription"],
        ["TouRoleRevealerTrap"] = ["TownOfUsMira.Role.TrapperTrap"],
        ["TouRoleRevealerTrapWikiDescription"] = ["TownOfUsMira.Role.TrapperTrap.WikiDescription"],
        ["TouRoleRevealerMessageTitle"] = ["TownOfUsMira.Role.TrapperMessageTitle"],
        ["TouRoleRevealerNoPlayers"] = ["TownOfUsMira.Role.TrapperNoPlayers"],
        ["TouRoleRevealerNotEnoughPLayers"] = ["TownOfUsMira.Role.TrapperNotEnoughPLayers"],
        ["TouRoleRevealerRolesCaught"] = ["TownOfUsMira.Role.TrapperRolesCaught"],
    };

    private static readonly string[] DescriptionSuffixes = ["IntroBlurb", "TabDescription", "WikiDescription", "MedDescription"];

    // TOU-Mira 1.7.1 markup the extension's XML still uses: \%nl\%, \%color=#..\%, \%player\% ...
    private static readonly Regex EscapedTag = new(@"\\%([^%\\]+)\\%", RegexOptions.Compiled);

    // language -> key -> value before the extension touched it (null: the key did not exist)
    private static Dictionary<MiraLanguage, Dictionary<string, string?>>? _pristine;

    public static void SearchInternalLocale()
    {
        var forcePolish = false;
        var translateRoleNames = false;
        try
        {
            if (LocalSettingsTabSingleton<TouExtensionLocalSettings>.Instance != null)
            {
                var settings = LocalSettingsTabSingleton<TouExtensionLocalSettings>.Instance;
                forcePolish = settings.UsePolishLanguage.Value;
                translateRoleNames = settings.TranslateRoleNames.Value;
            }
        }
        catch
        {
            // Settings tab not created yet: defaults apply.
        }

        try
        {
            var english = WithTouAliases(ReadResource(EnglishResource));
            var polish = WithTouAliases(ReadResource(PolishResource));
            var languages = MiraLocaleManager.LangList.Keys.ToList();
            foreach (var language in languages)
            {
                MiraLocaleManager.Locale.TryAdd(language, []);
            }

            var roleNameKeys = FindRoleNameKeys(english);
            CapturePristine(languages, english.Keys.Concat(polish.Keys).Concat(roleNameKeys));
            RestorePristine();

            foreach (var language in languages)
            {
                Inject(language, english);
            }

            if (forcePolish)
            {
                foreach (var language in languages)
                {
                    Inject(language, polish);
                }
            }
            else
            {
                Inject(MiraLanguage.Polish, polish);
            }

            if (!translateRoleNames)
            {
                var targets = forcePolish ? languages : [MiraLanguage.Polish];
                foreach (var key in roleNameKeys)
                {
                    var englishName = english.TryGetValue(key, out var own) ? own : PristineEnglish(key);
                    if (englishName == null)
                    {
                        continue;
                    }

                    foreach (var language in targets)
                    {
                        MiraLocaleManager.Locale[language][key] = englishName;
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Error($"Extension locale failed to load: {ex}");
        }
    }

    private static void Inject(MiraLanguage language, Dictionary<string, string> strings)
    {
        var table = MiraLocaleManager.Locale[language];
        foreach (var entry in strings)
        {
            table[entry.Key] = entry.Value;
        }
    }

    private static void CapturePristine(List<MiraLanguage> languages, IEnumerable<string> keys)
    {
        _pristine ??= [];
        var keyList = keys.Distinct().ToList();
        foreach (var language in languages)
        {
            if (!_pristine.TryGetValue(language, out var saved))
            {
                saved = [];
                _pristine[language] = saved;
            }

            var table = MiraLocaleManager.Locale[language];
            foreach (var key in keyList)
            {
                if (!saved.ContainsKey(key))
                {
                    saved[key] = table.TryGetValue(key, out var value) ? value : null;
                }
            }
        }
    }

    private static void RestorePristine()
    {
        foreach (var (language, saved) in _pristine!)
        {
            var table = MiraLocaleManager.Locale[language];
            foreach (var (key, value) in saved)
            {
                if (value == null)
                {
                    table.Remove(key);
                }
                else
                {
                    table[key] = value;
                }
            }
        }
    }

    private static string? PristineEnglish(string key)
    {
        return _pristine != null && _pristine.TryGetValue(MiraLanguage.English, out var saved) &&
               saved.TryGetValue(key, out var value)
            ? value
            : null;
    }

    /// <summary>
    /// Role name keys: TOU's <c>TownOfUsMira.Role.X</c> and the extension's <c>ExtensionRoleX</c>, recognised
    /// by having a description next to them, plus the few that do not.
    /// </summary>
    private static HashSet<string> FindRoleNameKeys(Dictionary<string, string> english)
    {
        var englishTable = MiraLocaleManager.Locale.TryGetValue(MiraLanguage.English, out var table)
            ? table
            : [];
        var keys = new HashSet<string>(AdditionalRoleNameKeys);

        foreach (var key in englishTable.Keys)
        {
            if (key.StartsWith(TouRolePrefix, System.StringComparison.Ordinal) &&
                key.IndexOf('.', TouRolePrefix.Length) < 0 &&
                DescriptionSuffixes.Any(s => englishTable.ContainsKey($"{key}.{s}")))
            {
                keys.Add(key);
            }
        }

        foreach (var key in english.Keys)
        {
            if (key.StartsWith("ExtensionRole", System.StringComparison.Ordinal) &&
                (english.ContainsKey(key + "IntroBlurb") || english.ContainsKey(key + "TabDescription")))
            {
                keys.Add(key);
            }
            else if (key.StartsWith(TouRolePrefix, System.StringComparison.Ordinal) &&
                     key.IndexOf('.', TouRolePrefix.Length) < 0 && englishTable.ContainsKey(key + ".IntroBlurb"))
            {
                keys.Add(key);
            }
        }

        return keys;
    }

    /// <summary>
    /// Strings the extension overrides under TOU-Mira 1.7.1 key names are also stored under the 1.7.3 key that
    /// replaced them (<c>TouRoleDoomsayerMessageTitle</c> -> <c>TownOfUsMira.Role.DoomsayerMessageTitle</c>).
    /// </summary>
    private static Dictionary<string, string> WithTouAliases(Dictionary<string, string> strings)
    {
        var touTable = MiraLocaleManager.Locale.TryGetValue(MiraLanguage.English, out var table) ? table : [];
        var result = new Dictionary<string, string>(strings);
        foreach (var (key, value) in strings)
        {
            if (ManualAliases.TryGetValue(key, out var targets))
            {
                foreach (var target in targets)
                {
                    result[target] = value;
                }

                continue;
            }

            var match = MatchTouKey(key, touTable);
            if (match != null && !strings.ContainsKey(match))
            {
                result[match] = value;
            }
        }

        return result;
    }

    private static string? MatchTouKey(string oldKey, Dictionary<string, string> touTable)
    {
        var m = Regex.Match(oldKey, "^Tou(Role|Modifier)([A-Z][A-Za-z]*)$");
        if (!m.Success)
        {
            return null;
        }

        var prefix = m.Groups[1].Value == "Role" ? TouRolePrefix : "TownOfUsMira.Modifier.";
        var rest = m.Groups[2].Value;
        for (var nameLength = rest.Length; nameLength > 0; nameLength--)
        {
            var name = rest[..nameLength];
            var suffix = rest[nameLength..];
            var candidates = new List<string>();
            if (suffix.Length == 0)
            {
                candidates.Add(prefix + name);
            }
            else if (DescriptionSuffixes.Contains(suffix))
            {
                candidates.Add($"{prefix}{name}.{suffix}");
            }
            else
            {
                candidates.Add(prefix + name + suffix);
                candidates.AddRange(DescriptionSuffixes.Where(suffix.EndsWith)
                    .Select(s => $"{prefix}{name}{suffix[..^s.Length]}.{s}"));
            }

            var found = candidates.FirstOrDefault(touTable.ContainsKey);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Dictionary<string, string> ReadResource(string resourceName)
    {
        var strings = new Dictionary<string, string>();
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            Warning($"Extension locale resource {resourceName} not found");
            return strings;
        }

        using var reader = new StreamReader(stream);
        var document = new XmlDocument();
        document.LoadXml(reader.ReadToEnd());
        var nodes = document.SelectNodes("//string");
        if (nodes == null || nodes.Count == 0)
        {
            nodes = document.SelectNodes("//entry");
        }

        if (nodes == null)
        {
            return strings;
        }

        foreach (XmlNode node in nodes)
        {
            var key = node.Attributes?["name"]?.Value ?? node.Attributes?["key"]?.Value;
            if (!string.IsNullOrEmpty(key))
            {
                strings[key] = Unescape(node.InnerText);
            }
        }

        return strings;
    }

    /// <summary>
    /// TouLocale.GetParsed in 1.7.1 turned <c>\%tag\%</c> into <c>&lt;tag&gt;</c> and <c>&lt;nl&gt;</c> into a new
    /// line on every read; MiraLocaleManager does neither, so the conversion happens once here.
    /// </summary>
    private static string Unescape(string value)
    {
        value = EscapedTag.Replace(value, "<$1>");
        return value.Replace("<nl>", "\n").Replace("<and>", "&");
    }
}
