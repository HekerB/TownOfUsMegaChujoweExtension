using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Options.Modifiers.Neutral;

public sealed class VenomousModifierOptions : AbstractOptionGroup<VenomousModifier>
{
    public override string GroupName => "Venomous";
    public override Color GroupColor => new Color32(0, 200, 90, 255);
    public override MenuCategory ParentMenu => MenuCategory.Modifiers;
    public override uint GroupPriority => 40;

    public ModdedNumberOption VenomousRotDelay { get; } =
        new("ExtensionModifierVenomousRotDelay", 5f, 5f, 40f, 1f, MiraNumberSuffixes.Seconds);
}













