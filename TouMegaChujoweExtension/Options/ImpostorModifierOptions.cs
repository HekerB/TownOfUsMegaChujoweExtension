using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Options;

public sealed class ImpostorModifierOptions : AbstractOptionGroup
{
    public override string GroupName => "Impostor Modifiers";
    public override Color GroupColor => Palette.ImpostorRoleHeaderRed;
    public override MenuCategory ParentMenu => MenuCategory.Modifiers;
    public override uint GroupPriority => 3;

    [ModdedNumberOption("ExtensionModifierLuckyAmount", 0, 15)]
    public float LuckyAmount { get; set; } = 0;

    public ModdedNumberOption LuckyChance { get; } =
        new("ExtensionModifierLuckyChance", 50f, 0, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<ImpostorModifierOptions>.Instance.LuckyAmount > 0
        };
}