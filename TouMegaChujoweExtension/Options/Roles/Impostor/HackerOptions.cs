using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class HackerOptions : AbstractOptionGroup<HackerRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleHacker", "Hacker");

    [ModdedNumberOption("ExtensionOptionHackerJamCooldown", 10f, 35f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float JamCooldownSeconds { get; set; } = 25f;

    [ModdedNumberOption("ExtensionOptionHackerJamDuration", 5f, 30f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float JamDurationSeconds { get; set; } = 15f;

    [ModdedNumberOption("ExtensionOptionHackerInitialJamCharges", 0f, 11f, 1f, MiraNumberSuffixes.None, "0", true)]
    public float InitialJamCharges { get; set; } = 3f;

    [ModdedToggleOption("ExtensionOptionHackerSimpleModeJamOnly")]
    public bool SimpleModeJamOnly { get; set; } = false;

    public ModdedToggleOption MoveWithDeviceOption { get; } = new("ExtensionOptionHackerMoveWithDevice", true)
    {
        Visible = () => !OptionGroupSingleton<HackerOptions>.Instance.SimpleModeJamOnly
    };

    public ModdedNumberOption MaxBatterySecondsOption { get; } = new("ExtensionOptionHackerMaxBatterySeconds", 10f, 3f, 20f, 1f, MiraNumberSuffixes.Seconds)
    {
        Visible = () => !OptionGroupSingleton<HackerOptions>.Instance.SimpleModeJamOnly
    };

    public ModdedNumberOption BatteryPerDownloadSecondOption { get; } = new("ExtensionOptionHackerBatteryPerDownloadSecond", 2f, 1f, 4f, 1f, MiraNumberSuffixes.Seconds)
    {
        Visible = () => !OptionGroupSingleton<HackerOptions>.Instance.SimpleModeJamOnly
    };

    public ModdedNumberOption DownloadRangeOption { get; } = new("ExtensionOptionHackerDownloadRange", 1f, 0.5f, 2.5f, 0.25f, MiraNumberSuffixes.None)
    {
        Visible = () => !OptionGroupSingleton<HackerOptions>.Instance.SimpleModeJamOnly
    };

    // Backward compatibility for HackerRole
    public float MaxBatterySeconds => MaxBatterySecondsOption.Value;
    public bool MoveWithDevice => MoveWithDeviceOption.Value;
    public float BatteryPerDownloadSecond => BatteryPerDownloadSecondOption.Value;
    public float DownloadRange => DownloadRangeOption.Value;

    public bool JamEnabled => InitialJamCharges > 0f;
}















