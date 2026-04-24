using MiraAPI.GameOptions;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Modifiers.Impostor;
using UnityEngine;

namespace TouMegaChujoweExtension.Options.Modifiers;

public sealed class LuckyModifierOptions : AbstractOptionGroup<LuckyModifier>
{
    public override string GroupName => "Lucky";
    public override Color GroupColor => new Color32(214, 64, 66, 255);
    public override bool ShowInModifiersMenu => true;
    public override uint GroupPriority => 30;

    public ModdedNumberOption LuckyMinCooldown { get; } =
        new("ExtensionModifierLuckyMinCooldown", 10f, 0f, 60f, 1f, MiraNumberSuffixes.Seconds);

    public ModdedNumberOption LuckyMaxCooldown { get; } =
        new("ExtensionModifierLuckyMaxCooldown", 50f, 0f, 60f, 1f, MiraNumberSuffixes.Seconds);
}