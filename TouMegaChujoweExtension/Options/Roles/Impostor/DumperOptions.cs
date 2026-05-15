using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class DumperOptions : AbstractOptionGroup<DumperRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleDumper", "Dumper");

    [ModdedNumberOption("ExtensionOptionDumperMaxDragDuration", 5f, 60f, 1f, MiraNumberSuffixes.Seconds, "0")]
    public float MaxDragDuration { get; set; } = 10f;

    [ModdedNumberOption("ExtensionOptionDumperTakeCooldown", 5f, 60f, 1f, MiraNumberSuffixes.Seconds, "0")]
    public float TakeCooldown { get; set; } = 20f;
}
