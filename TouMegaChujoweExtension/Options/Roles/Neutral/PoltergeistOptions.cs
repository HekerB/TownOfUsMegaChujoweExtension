using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;
using TouMegaChujoweExtension.Roles.Classic.Neutral;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public enum PoltergeistWinOptions
{
    EndsGame,
    WinsWithOthers
}

public sealed class PoltergeistOptions : AbstractOptionGroup<PoltergeistRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRolePoltergeist", "Poltergeist");

    [ModdedNumberOption("Decoys Triggered to Win", 1f, 10f, 1f, MiraNumberSuffixes.None)]
    public float RequiredDecoysReported { get; set; } = 3f;

    [ModdedEnumOption("Poltergeist Win Condition", typeof(PoltergeistWinOptions))]
    public PoltergeistWinOptions PoltergeistWin { get; set; } = PoltergeistWinOptions.EndsGame;

    [ModdedNumberOption("Decoy Cooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float DecoyCooldown { get; set; } = 30f;

    [ModdedNumberOption("Decoys Triggered Before Clickable", 0f, 10f, 1f, MiraNumberSuffixes.None)]
    public float DecoysReportedBeforeClickable { get; set; } = 1f;
}
