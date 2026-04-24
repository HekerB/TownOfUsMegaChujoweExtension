using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;
using TouMegaChujoweExtension.Roles.Neutral;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public sealed class PopeOptions : AbstractOptionGroup<PopeRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRolePope", "Pope");

    [ModdedNumberOption("ExtensionOptionPopeCanonizeCooldown", 5f, 120f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float CanonizeCooldown { get; set; } = 25f;

    [ModdedNumberOption("ExtensionOptionPopeMaxCanonizations", 3f, 15f, 1f, MiraNumberSuffixes.None, "0")]
    public float MaxCanonizations { get; set; } = 15f;

    [ModdedNumberOption("ExtensionOptionPopeJudgementDuration", 30f, 180f, 5f, MiraNumberSuffixes.Seconds)]
    public float JudgementDuration { get; set; } = 120f;

    [ModdedToggleOption("ExtensionOptionPopeCanonizeInteractions")]
    public bool CanonizeInteractions { get; set; } = true;
}