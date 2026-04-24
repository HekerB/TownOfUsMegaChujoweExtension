using MiraAPI.GameOptions;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Modifiers.Neutral;
using UnityEngine;

namespace TouMegaChujoweExtension.Options.Modifiers;

public sealed class VenomousModifierOptions : AbstractOptionGroup<VenomousModifier>
{
    public override string GroupName => "Venomous";
    public override Color GroupColor => new Color32(0, 200, 90, 255);
    public override bool ShowInModifiersMenu => true;
    public override uint GroupPriority => 40;

    public ModdedNumberOption VenomousRotDelay { get; } =
        new("ExtensionModifierVenomousRotDelay", 5f, 5f, 40f, 1f, MiraNumberSuffixes.Seconds);

    public ModdedToggleOption VenomousGuessable { get; } =
        new("ExtensionModifierVenomousGuessable", true);
}