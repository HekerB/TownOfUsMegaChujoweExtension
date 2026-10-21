using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Options.Modifiers.Universal;

public sealed class WastedModifierOptions : AbstractOptionGroup<WastedModifier>
{
    public override string GroupName => TownOfUs.Modules.Localization.TouLocale.Get("ExtensionModifierWasted", "Wasted");
    public override Color GroupColor => new Color32(64, 168, 100, 255);
    public override MenuCategory ParentMenu => MenuCategory.Modifiers;
    public override uint GroupPriority => 52;

    public ModdedNumberOption WastedDuration { get; } =
        new("ExtensionModifierWastedDuration", 3f, 1f, 20f, 1f, MiraNumberSuffixes.None);
}













