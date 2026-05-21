using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class BootOptions : AbstractOptionGroup<BootRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleBoot", "Boot");

    [ModdedNumberOption("ExtensionOptionBootKillCooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds, "0.0")]
    public float KillCooldown { get; set; } = 30f;

    [ModdedNumberOption("ExtensionOptionBootCooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds, "0.0")]
    public float BootCooldown { get; set; } = 20f;

    [ModdedToggleOption("ExtensionOptionBootSyncCooldowns")]
    public bool SyncCooldowns { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionBootCanVent")]
    public bool CanVent { get; set; } = true;
}
