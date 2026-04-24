using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class OutlawOptions : AbstractOptionGroup<OutlawRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleOutlaw", "Outlaw");

    [ModdedNumberOption("ExtensionOptionOutlawKillCooldown", 5f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float KillCooldown { get; set; } = 25f;

    [ModdedNumberOption("ExtensionOptionOutlawDoubleKillWindow", 1f, 10f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float DoubleKillWindow { get; set; } = 3f;

    [ModdedNumberOption("ExtensionOptionOutlawBonusKills", 1f, 5f, 1f, MiraNumberSuffixes.None)]
    public float BonusKills { get; set; } = 1f;
}