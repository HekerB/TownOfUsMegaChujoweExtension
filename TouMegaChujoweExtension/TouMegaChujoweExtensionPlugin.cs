using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using MiraAPI;
using MiraAPI.PluginLoading;
using Reactor;
using Reactor.Networking;
using Reactor.Networking.Attributes;
using Reactor.Utilities;
using System.Globalization;
using System.Reflection;
using TouMegaChujoweExtension.Patches;
using TouMegaChujoweExtension.Patches.WinConditions;
using TouMegaChujoweExtension.Utilities;
using TouMegaChujoweExtension.Modules;
using TownOfUs;
using TownOfUs.Patches;

namespace TouMegaChujoweExtension;

[BepInAutoPlugin("toumegachujowe.tou.extension", "Tou Mega Chujowe Extension")]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
[BepInDependency(MiraApiPlugin.Id)]
[BepInDependency(TownOfUsPlugin.Id)]
[ReactorModFlags(ModFlags.RequireOnAllClients)]
public partial class TouMegaChujoweExtensionPlugin : BasePlugin, IMiraPlugin
{
	/// <summary>
    ///     Gets the specified Culture for string manipulations.
    /// </summary> 
    public static CultureInfo Culture => TownOfUsPlugin.Culture;
	    /// <inheritdoc />
    public string OptionsTitleText => "TOU Mega Chujowe Extension";

    /// <inheritdoc />
    public string CustomOptionMenuNameOne => TownOfUs.Modules.Localization.TouLocale.Get("TOUMCETabOptionBetterRoles");

    /// <inheritdoc />
    public string CustomOptionMenuOneDescription => TownOfUs.Modules.Localization.TouLocale.Get("TOUMCETabOptionBetterRolesDesc");
    /// <summary>
    ///     Determines if the current build is a dev build or not.
    /// </summary>
    public static bool IsDevBuild => false;
    /// <inheritdoc />
    public ConfigFile GetConfigFile() => Config;
	
    // public static Harmony
	public static Harmony Harmony { get; private set; } = null!;

	public override void Load()
	{
		DuplicateChecker.Check();
		Harmony = new Harmony(Id);

		ReactorCredits.Register("Tou Mega Chujowe Extension", Version, IsDevBuild, ReactorCredits.AlwaysShow);
		IL2CPPChainloader.Instance.Finished += Modules.ExtensionLocale.SearchInternalLocale;
		IL2CPPChainloader.Instance.Finished += LawyerTeamChatRegistration.Register;
		IL2CPPChainloader.Instance.Finished += Patches.Lovers.LoverMeetingChatRegistration.Register;
		IL2CPPChainloader.Instance.Finished += () => ExtensionModNewsFetcher.CheckForNews();
	
		PatchAllWithErrorHandling();

		WinConditionRegistry.Register(new NeutralExtensionWinCondition());
	}

    private void PatchAllWithErrorHandling()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var patchTypes = SafeReflection.GetTypesSafe(assembly)
            .Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0)
            .ToList();

        int successCount = 0;
        int failCount = 0;
        List<string> failedTypes = new();

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

        // Apply manual patches for Town of Us role buttons that need special handling
        PatchToURoleButtons();
    }

    private void PatchToURoleButtons()
    {
        try
        {
            var assembly = typeof(TownOfUsPlugin).Assembly;
            var types = assembly.GetTypes();
            var prefix = new HarmonyMethod(typeof(ShieldUtils).GetMethod(nameof(ShieldUtils.HandleToURoleButtonPrefix)));

            foreach (var type in types)
            {
                // Specifically target roles that have hardcoded shield checks in their OnClick
                if (type.Name == "SheriffShootButton" || 
                    type.Name == "OfficerShootButton" || 
                    type.Name == "HunterKillButton")
                {
                    var method = type.GetMethod("OnClick", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (method != null)
                    {
                        Info($"[ManualPatch] Patching {type.Name}.OnClick");
                        Harmony.Patch(method, prefix: prefix);
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Error($"[ManualPatch] Critical failure during manual patching: {ex.Message}");
        }
    }
}
