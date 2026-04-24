using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public class KamikazeOptions : AbstractOptionGroup<KamikazeRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleKamikaze");

    [ModdedNumberOption("ExtensionOptionKamikazeKillCooldown", min: 10f, max: 60f, increment: 2.5f, suffixType: MiraNumberSuffixes.Seconds)]
    public float KillCooldown { get; set; } = 25f;

    [ModdedNumberOption("ExtensionOptionKamikazeSuicideCooldown", min: 5f, max: 120f, increment: 2.5f, suffixType: MiraNumberSuffixes.Seconds)]
    public float SuicideCooldown { get; set; } = 30f;

    [ModdedNumberOption("ExtensionOptionKamikazeDetonateRadius", 0.05f, 1f, 0.05f, MiraNumberSuffixes.Multiplier, "0.00")]
    public float DetonateRadius { get; set; } = 0.3f;

    [ModdedNumberOption("ExtensionOptionKamikazeMaxKills", min: 1, max: 14, increment: 1)]
    public float MaxKills { get; set; } = 3;

    [ModdedToggleOption("ExtensionOptionKamikazeShowRadiusIndicator")]
    public bool ShowRadiusIndicator { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionKamikazeCanSuicideFirstRound")]
    public bool CanSuicideFirstRound { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionKamikazeCanVent")]
    public bool CanVent { get; set; } = false;
}