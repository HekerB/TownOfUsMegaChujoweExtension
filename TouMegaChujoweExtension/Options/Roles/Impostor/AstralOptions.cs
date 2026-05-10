using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class AstralOptions : AbstractOptionGroup<AstralRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleAstral", "Astral");
    public override uint GroupPriority => 5;



    [ModdedNumberOption("ExtensionOptionAstralPhaseCooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float PhaseCooldown { get; set; } = 30f;

    [ModdedNumberOption("ExtensionOptionAstralPhaseDuration", 5f, 30f, 1f, MiraNumberSuffixes.Seconds)]
    public float PhaseDuration { get; set; } = 15f;

    [ModdedToggleOption("ExtensionOptionAstralTeleportInvisibilityEnabled")]
    public bool InvisibilityAfterTeleport { get; set; } = true;

    public ModdedNumberOption InvisibilityDuration { get; } = new("ExtensionOptionAstralTeleportInvisibilityDuration", 5f, 1f, 15f, 0.5f, MiraNumberSuffixes.Seconds)
    {
        Visible = () => OptionGroupSingleton<AstralOptions>.Instance.InvisibilityAfterTeleport
    };

    [ModdedToggleOption("ExtensionOptionAstralDieIfNoKillDuringPhase")]
    public bool DieIfNoKillDuringPhase { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionAstralCanVent")]
    public bool CanVent { get; set; } = true;
}
