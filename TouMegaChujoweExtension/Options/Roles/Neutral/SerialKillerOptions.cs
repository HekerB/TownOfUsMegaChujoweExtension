using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public sealed class SerialKillerOptions : AbstractOptionGroup<SerialKillerRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleSerialKiller", "Serial Killer");

    [ModdedToggleOption("ExtensionOptionSerialKillerCanReportBodies")]
    public bool CanReportBodies { get; set; } = false;

    [ModdedToggleOption("ExtensionOptionSerialKillerManiacMode")]
    public bool ManiacMode { get; set; } = true;

    public ModdedNumberOption ManiacTimer { get; } = new("ExtensionOptionSerialKillerManiacTimer", 40f, 5f, 60f, 5f, MiraNumberSuffixes.Seconds, "0.0")
    {
        Visible = () => OptionGroupSingleton<SerialKillerOptions>.Instance.ManiacMode
    };

    public ModdedNumberOption ManiacCooldown { get; } = new("ExtensionOptionSerialKillerManiacCooldown", 19f, 0f, 30f, 0.5f, MiraNumberSuffixes.Seconds, "0.0")
    {
        Visible = () => OptionGroupSingleton<SerialKillerOptions>.Instance.ManiacMode
    };

    [ModdedToggleOption("ExtensionOptionSerialKillerKillCooldownReductionEnabled")]
    public bool KillCooldownReductionEnabled { get; set; } = false;

    public ModdedNumberOption KillCooldownReductionPerKill { get; } = new("ExtensionOptionSerialKillerKillCooldownReductionPerKill", 2.5f, 0f, 15f, 0.5f, MiraNumberSuffixes.Seconds)
    {
        Visible = () => OptionGroupSingleton<SerialKillerOptions>.Instance.KillCooldownReductionEnabled
    };

    public ModdedNumberOption MinimumKillCooldown { get; } = new("ExtensionOptionSerialKillerMinimumKillCooldown", 10f, 0f, 30f, 1f, MiraNumberSuffixes.Seconds)
    {
        Visible = () => OptionGroupSingleton<SerialKillerOptions>.Instance.KillCooldownReductionEnabled
    };

    [ModdedToggleOption("ExtensionOptionSerialKillerManiacTimerReductionEnabled")]
    public bool ManiacTimerReductionEnabled { get; set; } = false;

    public ModdedNumberOption ManiacTimerReductionPerKill { get; } = new("ExtensionOptionSerialKillerManiacTimerReductionPerKill", 2.5f, 0f, 10f, 0.5f, MiraNumberSuffixes.Seconds)
    {
        Visible = () => OptionGroupSingleton<SerialKillerOptions>.Instance.ManiacMode && OptionGroupSingleton<SerialKillerOptions>.Instance.ManiacTimerReductionEnabled
    };

    public ModdedNumberOption ManiacTimerLimit { get; } = new("ExtensionOptionSerialKillerManiacTimerLimit", 30f, 15f, 60f, 1f, MiraNumberSuffixes.Seconds)
    {
        Visible = () => OptionGroupSingleton<SerialKillerOptions>.Instance.ManiacMode && OptionGroupSingleton<SerialKillerOptions>.Instance.ManiacTimerReductionEnabled
    };

    [ModdedEnumOption("ExtensionOptionSerialKillerVentKillTargets", typeof(VentKillTargets),
        ["ExtensionOptionSerialKillerVentKillTargetsEnumImpostors",
         "ExtensionOptionSerialKillerVentKillTargetsEnumImpNK",
         "ExtensionOptionSerialKillerVentKillTargetsEnumImpNeutrals",
         "ExtensionOptionSerialKillerVentKillTargetsEnumAny"])]
    public VentKillTargets VentKillTargets { get; set; } = VentKillTargets.Any;
}

public enum VentKillTargets
{
    Impostors,
    ImpNK,
    ImpNeutrals,
    Any
}
