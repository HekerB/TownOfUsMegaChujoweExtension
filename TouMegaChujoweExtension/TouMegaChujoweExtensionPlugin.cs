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
    public static bool IsDevBuild => true;
    /// <inheritdoc />
    public ConfigFile GetConfigFile() => Config;
	
    // public static Harmony
	public static Harmony Harmony { get; private set; } = null!;

	public override void Load()
	{
		DuplicateChecker.Check();
		Modules.ModUpdater.CleanOldVersions();
		Harmony = new Harmony(Id);

        Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<Modules.TomahawkAxe>();
        Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<Modules.DeathNotePickupBehaviour>();
        Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<Modules.DeathNoteUIController>();
        Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<Modifiers.IcenbergOverlayAnimator>();
        Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<Modules.DecoyBodyComponent>();
        Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<Modules.InverterCameraBehaviour>();
        Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<Modules.SoulAnimator>();
        Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<Modules.JokerCloneControlComponent>();

		ReactorCredits.Register("Tou Mega Chujowe Extension", Version, IsDevBuild, ReactorCredits.AlwaysShow);
		IL2CPPChainloader.Instance.Finished += Modules.ExtensionLocale.SearchInternalLocale;
		IL2CPPChainloader.Instance.Finished += LawyerTeamChatRegistration.Register;
		IL2CPPChainloader.Instance.Finished += Patches.Roles.Lovers.LoverMeetingChatRegistration.Register;
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

    }
}














