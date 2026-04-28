using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class HackerOptions : AbstractOptionGroup<HackerRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleHacker", "Hacker");

    [ModdedNumberOption("ExtensionOptionHackerJamCooldown", 10f, 35f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float JamCooldownSeconds { get; set; } = 25f;

    [ModdedNumberOption("ExtensionOptionHackerJamDuration", 5f, 20f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float JamDurationSeconds { get; set; } = 15f;

    [ModdedNumberOption("ExtensionOptionHackerInitialJamCharges", 0f, 10f, 1f, MiraNumberSuffixes.None)]
    public float InitialJamCharges { get; set; } = 3f;

    [ModdedNumberOption("ExtensionOptionHackerJamChargesPerKill", 0f, 5f, 1f, MiraNumberSuffixes.None)]
    public float JamChargesPerKill { get; set; } = 1f;

    [ModdedNumberOption("ExtensionOptionHackerJamMaxCharges", 1f, 10f, 1f, MiraNumberSuffixes.None)]
    public float JamMaxCharges { get; set; } = 6f;

    [ModdedToggleOption("ExtensionOptionHackerSimpleModeJamOnly")]
    public bool SimpleModeJamOnly { get; set; } = false;

    public ModdedToggleOption MoveWithDeviceOption { get; } = new("ExtensionOptionHackerMoveWithDevice", true)
    {
        Visible = () => !OptionGroupSingleton<HackerOptions>.Instance.SimpleModeJamOnly
    };

    public ModdedNumberOption MaxBatterySecondsOption { get; } = new("ExtensionOptionHackerMaxBatterySeconds", 10f, 3f, 15f, 1f, MiraNumberSuffixes.Seconds)
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

    public bool JamEnabled =>
        JamMaxCharges > 0f && (SimpleModeJamOnly || JamChargesPerKill > 0f || InitialJamCharges > 0f);
}
