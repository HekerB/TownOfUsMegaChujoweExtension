using MiraAPI.GameOptions;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TownOfUs;
using UnityEngine;

namespace TouMegaChujoweExtension.Options.Modifiers;

public sealed class DeathNoteModifierOptions : AbstractOptionGroup<DeathNoteModifier>
{
    public override string GroupName => "Death Note";
    public override Color GroupColor => new Color32(40, 0, 80, 255);
    public override bool ShowInModifiersMenu => true;
    public override uint GroupPriority => 41;

    public ModdedNumberOption DeathNoteTimer { get; } =
        new("ExtensionModifierDeathNoteTimer", 40f, 10f, 120f, 5f, MiraNumberSuffixes.Seconds);

    public ModdedNumberOption DeathNoteMaxUses { get; } =
        new("ExtensionModifierDeathNoteMaxUses", 1f, 1f, 1f, 1f, MiraNumberSuffixes.None);
}
