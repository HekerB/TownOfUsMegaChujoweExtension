using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;

namespace TouMegaChujoweExtension.Options;

public sealed class UniversalModifierOptions : AbstractOptionGroup
{
    public override string GroupName => "Universal Modifiers";
    public override MenuCategory ParentMenu => MenuCategory.Modifiers;
    public override uint GroupPriority => 1;

    [ModdedNumberOption("ExtensionModifierSpitefulAmount", 0, 15)]
    public float SpitefulAmount { get; set; } = 0;

    public ModdedNumberOption SpitefulChance { get; } =
        new("ExtensionModifierSpitefulChance", 50f, 0, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<UniversalModifierOptions>.Instance.SpitefulAmount > 0
        };

    [ModdedNumberOption("ExtensionModifierWastedAmount", 0, 15)]
    public float WastedAmount { get; set; } = 0;

    public ModdedNumberOption WastedChance { get; } =
        new("ExtensionModifierWastedChance", 50f, 0, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<UniversalModifierOptions>.Instance.WastedAmount > 0
        };

    [ModdedNumberOption("ExtensionModifierChildAmount", 0, 15)]
    public float ChildAmount { get; set; } = 0;

    public ModdedNumberOption ChildChance { get; } =
        new("ExtensionModifierChildChance", 50f, 0, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<UniversalModifierOptions>.Instance.ChildAmount > 0
        };
}