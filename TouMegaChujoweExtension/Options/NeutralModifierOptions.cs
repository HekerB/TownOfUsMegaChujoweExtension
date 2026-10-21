using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs;
using UnityEngine;

namespace TouMegaChujoweExtension.Options;

public sealed class NeutralModifierOptions : AbstractOptionGroup
{
    public override string GroupName => "Neutral Modifiers";
    public override Color GroupColor => TownOfUsColors.Neutral;
    public override MenuCategory ParentMenu => MenuCategory.Modifiers;
    public override uint GroupPriority => 4;

    [ModdedNumberOption("ExtensionModifierVenomousAmount", 0, 15)]
    public float VenomousAmount { get; set; } = 0;

    public ModdedNumberOption VenomousChance { get; } =
        new("ExtensionModifierVenomousChance", 50f, 0, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<NeutralModifierOptions>.Instance.VenomousAmount > 0
        };
        [ModdedNumberOption("ExtensionModifierDeathNoteAmount", 0, 15)]
    public float DeathNoteAmount { get; set; } = 0;

    public ModdedNumberOption DeathNoteChance { get; } =    
    new("ExtensionModifierDeathNoteChance", 100f, 0, 100f, 10f, MiraNumberSuffixes.Percent)
    {
        Visible = () => OptionGroupSingleton<NeutralModifierOptions>.Instance.DeathNoteAmount > 0
    };
}