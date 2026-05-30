using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using BepInEx;
using HarmonyLib;
using MiraAPI.PluginLoading;
using MiraAPI;
using Reactor.Networking.Attributes;
using Reactor.Networking;
using Reactor.Utilities;
using Reactor;
using System.Globalization;
using System.Reflection;
using TownOfUs.Patches;
using TownOfUs;

namespace TouMegaChujoweExtension;

[BepInAutoPlugin("toumegachujowe.tou.extension", "Tou Mega Ch**owe Extension")]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
[BepInDependency(MiraApiPlugin.Id)]
[BepInDependency(TownOfUsPlugin.Id)]
[ReactorModFlags(ModFlags.RequireOnAllClients)]
public partial class TouMegaChujoweExtensionPlugin : BasePlugin, IMiraPlugin
{
    public const string UncensoredDisplayName = "Tou Mega Chujowe Extension";
    public const string CensoredDisplayName = "Tou Mega Ch**owe Extension";

    /// <summary>
    ///     Gets the specified Culture for string manipulations.
    /// </summary> 
    public static CultureInfo Culture => TownOfUsPlugin.Culture;
    /// <inheritdoc />
    public string OptionsTitleText => ShouldCensorModName ? "TOU Mega Ch**owe Extension" : "TOU Mega Chujowe Extension";

    /// <inheritdoc />
    public string CustomOptionMenuNameOne => TownOfUs.Modules.Localization.TouLocale.Get("TOUMCETabOptionBetterRoles");

    /// <inheritdoc />
    public string CustomOptionMenuOneDescription => TownOfUs.Modules.Localization.TouLocale.Get("TOUMCETabOptionBetterRolesDesc");
    /// <summary>
    ///     Determines if the current build is a dev build or not.
    /// </summary>
    public static bool IsDevBuild => false;
    public static bool ShouldCensorModName
    {
        get
        {
            try
            {
                return LocalSettingsTabSingleton<TouExtensionLocalSettings>.Instance?.CensorModName.Value ?? true;
            }
            catch
            {
                return true;
            }
        }
    }

    public static string DisplayName => ShouldCensorModName ? CensoredDisplayName : UncensoredDisplayName;
    public static string CensorVisibleText(string text)
    {
        if (!ShouldCensorModName || string.IsNullOrEmpty(text))
            return text;

        return text
            .Replace("TOU Mega Chujowe Extension", "TOU Mega Ch**owe Extension")
            .Replace("Tou Mega Chujowe Extension", CensoredDisplayName)
            .Replace("Mega Chujowe Perfect Comms", "Mega Ch**owe Perfect Comms")
            .Replace("ToU: Chujowe", "ToU: Ch**owe");
    }

    /// <inheritdoc />
    public ConfigFile GetConfigFile() => Config;

    // public static Harmony
    public static Harmony Harmony { get; private set; } = null!;

    public override void Load()
    {
        DuplicateChecker.Check();
        Harmony = new Harmony(Id);

        ReactorCredits.Register(DisplayName, Version, IsDevBuild, ReactorCredits.AlwaysShow);
        IL2CPPChainloader.Instance.Finished += Modules.ExtensionLocale.SearchInternalLocale;
        IL2CPPChainloader.Instance.Finished += LawyerTeamChatRegistration.Register;
        IL2CPPChainloader.Instance.Finished += Patches.Roles.Lovers.LoverMeetingChatRegistration.Register;
        IL2CPPChainloader.Instance.Finished += Patches.Roles.Jackal.JackalTeamChatRegistration.Register;
        IL2CPPChainloader.Instance.Finished += Patches.Roles.Pelican.PelicanTargetBlockPatches.Init;
        IL2CPPChainloader.Instance.Finished += () => ExtensionModNewsFetcher.CheckForNews();

        PatchAllWithErrorHandling();

        WinConditionRegistry.Register(new NeutralExtensionWinCondition());
    }

    private static void PatchAllWithErrorHandling()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var patchTypes = SafeReflection.GetTypesSafe(assembly)
            .Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0)
            .ToList();

        int successCount = 0;
        int failCount = 0;
        List<string> failedTypes = [];

        foreach (var type in patchTypes)
        {
            try
            {
                Harmony.PatchAll(type);
                successCount++;
            }
            catch (System.Exception ex)
            {
                failCount++;
                failedTypes.Add(type.FullName ?? type.Name);
                Error($"Failed to patch class: {type.FullName}");
                Error($"Error type: {ex.GetType().FullName}");
                Error($"Error message: {ex.Message}");

                if (ex.InnerException != null)
                    Error($"Inner exception: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");

                Debug($"Stack trace: {ex.StackTrace}");
            }
        }

        Info($"Harmony patching completed: {successCount} classes patched successfully, {failCount} classes had errors");

        if (failCount > 0)
        {
            Warning($"Failed to patch the following classes: {string.Join(", ", failedTypes)}");
            Warning("The mod may function partially. If you experience issues, please report which patch classes failed.");
        }

        if (successCount == 0 && failCount > 0)
        {
            Error("All Harmony patches failed! The mod cannot function without patches.");
            throw new System.InvalidOperationException($"Failed to apply any Harmony patches. {failCount} patch classes failed. See log for details.");
        }

    }
}














