using MiraAPI.GameOptions;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Modifiers.Universal;
using UnityEngine;

namespace TouMegaChujoweExtension.Options.Modifiers;

public sealed class DrunkModifierOptions : AbstractOptionGroup<DrunkModifier>
{
    public override string GroupName => "Drunk";
    public override Color GroupColor => new Color32(64, 168, 100, 255);
    public override bool ShowInModifiersMenu => true;
    public override uint GroupPriority => 52;

    public ModdedNumberOption DrunkDuration { get; } =
        new("ExtensionModifierDrunkDuration", 3f, 1f, 20f, 1f, MiraNumberSuffixes.None);
}
