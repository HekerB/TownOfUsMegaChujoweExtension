using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class SpeedyOptions : AbstractOptionGroup<SpeedyRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleSpeedy", "Speedy");
    public override uint GroupPriority => 5;

    [ModdedNumberOption("ExtensionOptionSpeedyAccelerateCooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float AccelerateCooldown { get; set; } = 20f;

    [ModdedNumberOption("ExtensionOptionSpeedyAccelerateDuration", 5f, 30f, 1f, MiraNumberSuffixes.Seconds)]
    public float AccelerateDuration { get; set; } = 10f;

    [ModdedNumberOption("ExtensionOptionSpeedyAccelerationBuff", 1.25f, 3f, 0.25f, MiraNumberSuffixes.Multiplier)]
    public float AccelerationBuff { get; set; } = 2f;

    [ModdedToggleOption("ExtensionOptionSpeedyCanVent")]
    public bool CanVent { get; set; } = true;
}
