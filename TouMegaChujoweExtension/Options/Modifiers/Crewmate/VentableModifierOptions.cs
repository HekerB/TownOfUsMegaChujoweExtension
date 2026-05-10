using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Options.Modifiers.Crewmate;

public sealed class VentableModifierOptions : AbstractOptionGroup<VentableModifier>
{
    public override string GroupName => "Ventable";
    public override Color GroupColor => new Color32(120, 220, 255, 255);
    public override bool ShowInModifiersMenu => true;
    public override uint GroupPriority => 20;

    public ModdedNumberOption MaxVentUses { get; } =
        new("ExtensionModifierVentableMaxVentUses", 3f, 1f, 15f, 1f, MiraNumberSuffixes.None);

    public ModdedNumberOption VentCooldown { get; } =
        new("ExtensionModifierVentableVentCooldown", 15f, 0f, 60f, 2.5f, MiraNumberSuffixes.Seconds);

    public ModdedNumberOption VentDuration { get; } =
        new("ExtensionModifierVentableVentDuration", 10f, 1f, 30f, 1f, MiraNumberSuffixes.Seconds);
}













