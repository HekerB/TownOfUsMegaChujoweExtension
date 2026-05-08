using MiraAPI.GameOptions;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Modifiers.Universal;
using UnityEngine;

namespace TouMegaChujoweExtension.Options.Modifiers;

public sealed class ChildModifierOptions : AbstractOptionGroup<ChildModifier>
{
    public override string GroupName => "Child";
    public override Color GroupColor => new Color32(255, 140, 0, 255);
    public override bool ShowInModifiersMenu => true;
    public override uint GroupPriority => 53;

    public ModdedNumberOption StartingAge { get; } =
        new("ExtensionModifierChildStartAge", 5f, 0, 17f, 1f, MiraNumberSuffixes.None);

    public ModdedNumberOption AdultAge { get; } =
        new("ExtensionModifierChildAdultAge", 18f, 6f, 25f, 1f, MiraNumberSuffixes.None);

    public ModdedNumberOption GrowthInterval { get; } =
        new("ExtensionModifierChildGrowthInterval", 30f, 10f, 120f, 5f, MiraNumberSuffixes.Seconds);
}
