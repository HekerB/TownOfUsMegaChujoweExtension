using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public class KamikazeOptions : AbstractOptionGroup<KamikazeRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleKamikaze");

    [ModdedNumberOption("ExtensionOptionKamikazeDetonateRadius", 0.05f, 1f, 0.05f, MiraNumberSuffixes.Multiplier, "0.00")]
    public float DetonateRadius { get; set; } = 0.2f;

    [ModdedNumberOption("ExtensionOptionKamikazeMaxKills", min: 1, max: 14, increment: 1)]
    public float MaxKills { get; set; } = 3;

    [ModdedToggleOption("ExtensionOptionKamikazeShowRadiusIndicator")]
    public bool ShowRadiusIndicator { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionKamikazeCanSuicideFirstRound")]
    public bool CanSuicideFirstRound { get; set; } = false;

    [ModdedToggleOption("ExtensionOptionKamikazeCanVent")]
    public bool CanVent { get; set; } = true;

}















