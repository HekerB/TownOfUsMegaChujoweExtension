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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using TownOfUs.Patches;
using TownOfUs;

namespace TouMegaChujoweExtension;

[BepInPlugin(Id, Name, Version)]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
[BepInDependency(MiraApiPlugin.Id)]
[BepInDependency(TownOfUsPlugin.Id)]
[ReactorModFlags(ModFlags.RequireOnAllClients)]
public partial class TouMegaChujoweExtensionPlugin : BasePlugin, IMiraPlugin
{
    public const string Id = "toumegachujowe.tou.extension";
    public const string Name = "Tou Mega Ch**owe Extension";
    public const string Version = "1.4.0";
    public const string UncensoredDisplayName = "Tou Mega Chujowe Extension";
    public const string CensoredDisplayName = "Tou Mega Ch**owe Extension";

    public static CultureInfo Culture => TownOfUsPlugin.Culture;

    public string OptionsTitleText => ShouldCensorModName
        ? "TOU Mega Ch**owe Extension"
        : "TOU Mega Chujowe Extension";

    public string CustomOptionMenuNameOne => TownOfUs.Modules.Localization.TouLocale.Get("TOUMCETabOptionBetterRoles");

    public string CustomOptionMenuOneDescription => TownOfUs.Modules.Localization.TouLocale.Get("TOUMCETabOptionBetterRolesDesc");

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
        {
            return text;
        }

        return text
            .Replace("TOU Mega Chujowe Extension", "TOU Mega Ch**owe Extension")
            .Replace("Tou Mega Chujowe Extension", CensoredDisplayName)
            .Replace("Mega Chujowe Perfect Comms", "Mega Ch**owe Perfect Comms")
            .Replace("ToU: Chujowe", "ToU: Ch**owe");
    }

    public ConfigFile GetConfigFile() => Config;

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
        IL2CPPChainloader.Instance.Finished += Patches.Roles.Apocalypse.ApocalypseTeamChatRegistration.Register;
        IL2CPPChainloader.Instance.Finished += Patches.Roles.Agent.AgentTeamChatRegistration.Register;
        IL2CPPChainloader.Instance.Finished += Patches.Roles.Pelican.PelicanTargetBlockPatches.Init;
        IL2CPPChainloader.Instance.Finished += () => ExtensionModNewsFetcher.CheckForNews();

        PatchAllWithErrorHandling();

        WinConditionRegistry.Register(new NeutralExtensionWinCondition());
    }

    private static void PatchAllWithErrorHandling()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var debugPath = GetPatchDebugPath();

        WritePatchDebugHeader(debugPath);

        var patchTypes = SafeReflection.GetTypesSafe(assembly)
            .Where(HasHarmonyPatch)
            .OrderBy(type => type.FullName)
            .ToList();

        var successCount = 0;
        var failCount = 0;
        var skipCount = 0;

        var failedTypes = new List<string>();
        var skippedTypes = new List<string>();

        AppendPatchDebug(debugPath, $"Found patch classes: {patchTypes.Count}");
        AppendPatchDebug(debugPath, "");

        foreach (var type in patchTypes)
        {
            var typeName = type.FullName ?? type.Name;

            if (ShouldSkipPatchType(type))
            {
                skipCount++;
                skippedTypes.Add(typeName);

                AppendPatchDebug(debugPath, $"SKIP: {typeName}");
                Warning($"Skipped Harmony patch class: {typeName}");
                continue;
            }

            AppendPatchDebug(debugPath, $"PATCHING: {typeName}");

            try
            {
                Harmony.PatchAll(type);

                successCount++;
                AppendPatchDebug(debugPath, $"OK: {typeName}");
            }
            catch (Exception ex)
            {
                failCount++;
                failedTypes.Add(typeName);

                AppendPatchDebug(debugPath,
                    $"FAILED: {typeName}\n" +
                    $"{ex.GetType().FullName}: {ex.Message}\n" +
                    $"{ex.StackTrace}\n");

                Error($"Failed to patch class: {typeName}");
                Error($"Error type: {ex.GetType().FullName}");
                Error($"Error message: {ex.Message}");

                if (ex.InnerException != null)
                {
                    Error($"Inner exception: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");
                    AppendPatchDebug(debugPath,
                        $"INNER: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}\n" +
                        $"{ex.InnerException.StackTrace}\n");
                }

                Debug($"Stack trace: {ex.StackTrace}");
            }
        }

        AppendPatchDebug(debugPath,
            "\n==== TouMega patch debug finished ====\n" +
            $"Patched: {successCount}\n" +
            $"Failed: {failCount}\n" +
            $"Skipped: {skipCount}\n");

        Info($"Harmony patching completed: {successCount} classes patched successfully, {failCount} classes had errors, {skipCount} classes skipped");

        if (failCount > 0)
        {
            Warning($"Failed to patch the following classes: {string.Join(", ", failedTypes)}");
            Warning("The mod may function partially. If you experience issues, please report which patch classes failed.");
        }

        if (skipCount > 0)
        {
            Warning($"Skipped the following patch classes: {string.Join(", ", skippedTypes)}");
        }

        if (successCount == 0 && failCount > 0)
        {
            Error("All Harmony patches failed! The mod cannot function without patches.");
            throw new InvalidOperationException($"Failed to apply any Harmony patches. {failCount} patch classes failed. See log for details.");
        }
    }

    private static bool HasHarmonyPatch(Type type)
    {
        try
        {
            return type.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool ShouldSkipPatchType(Type type)
    {
        _ = type;
        return false;
    }

    private static string GetPatchDebugPath()
    {
        try
        {
            var pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (!string.IsNullOrWhiteSpace(pluginFolder))
            {
                return Path.Combine(pluginFolder, "TouMegaPatchDebug.log");
            }
        }
        catch
        {
            // fallback
        }

        return Path.Combine(Environment.CurrentDirectory, "TouMegaPatchDebug.log");
    }

    private static bool IsPluginLoaded(string guid)
    {
        try
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            if (guid == "com.divani.mods")
            {
                return assemblies.Any(assembly =>
                    assembly.GetName().Name?.Contains("Divani", StringComparison.OrdinalIgnoreCase) == true);
            }

            if (guid == TownOfUsPlugin.Id)
            {
                return assemblies.Any(assembly =>
                    assembly.GetName().Name?.Contains("TownOfUs", StringComparison.OrdinalIgnoreCase) == true);
            }

            if (guid == MiraApiPlugin.Id)
            {
                return assemblies.Any(assembly =>
                    assembly.GetName().Name?.Contains("MiraAPI", StringComparison.OrdinalIgnoreCase) == true);
            }

            return assemblies.Any(assembly =>
                assembly.FullName?.Contains(guid, StringComparison.OrdinalIgnoreCase) == true);
        }
        catch
        {
            return false;
        }
    }

    private static void WritePatchDebugHeader(string debugPath)
    {
        try
        {
            File.WriteAllText(debugPath,
                "==== TouMega patch debug start ====\n" +
                $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                $"Plugin Id: {Id}\n" +
                $"Plugin Version: {Version}\n" +
                $"Debug Path: {debugPath}\n" +
                $"Divani loaded: {IsPluginLoaded("com.divani.mods")}\n" +
                $"TownOfUs loaded: {IsPluginLoaded(TownOfUsPlugin.Id)}\n" +
                $"MiraAPI loaded: {IsPluginLoaded(MiraApiPlugin.Id)}\n" +
                "\n");
        }
        catch (Exception ex)
        {
            Error($"Could not write patch debug header: {ex}");
        }
    }

    private static void AppendPatchDebug(string debugPath, string text)
    {
        try
        {
            File.AppendAllText(debugPath, text + "\n");
        }
        catch (Exception ex)
        {
            Error($"Could not append patch debug log: {ex}");
        }
    }
}