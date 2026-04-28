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

    [ModdedToggleOption("ExtensionOptionSerialKillerCanReportBodies")]
    public bool CanReportBodies { get; set; } = false;

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
