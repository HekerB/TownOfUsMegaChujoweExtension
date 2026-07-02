using CluelessCensorTypeEnum = TouMegaChujoweExtension.Options.Modifiers.Crewmate.CluelessCensorType;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using UnityEngine;
using TouMegaChujoweExtension.Modifiers.Game;

namespace TouMegaChujoweExtension.Options.Modifiers.Crewmate;

public enum CluelessCensorType
{
    WhiteBars,
    Asterisks,
    QuestionMarks,
    Remove
}

public sealed class CluelessModifierOptions : AbstractOptionGroup<CluelessModifier>
{
    public override string GroupName => "Clueless";
    public override Color GroupColor => new Color32(20, 45, 120, 255);
    public override MenuCategory ParentMenu => MenuCategory.Modifiers;
    public override uint GroupPriority => 50;

    private static readonly string[] CluelessCensorTypeValues =
    [
        "ExtensionModifierCluelessCensorTypeEnumWhiteBars",
        "ExtensionModifierCluelessCensorTypeEnumAsterisks",
        "ExtensionModifierCluelessCensorTypeEnumQuestionMarks",
        "ExtensionModifierCluelessCensorTypeEnumRemove"
    ];

    public ModdedEnumOption<CluelessCensorType> CluelessCensorType { get; } =
        new("ExtensionModifierCluelessCensorType", CluelessCensorTypeEnum.Asterisks, CluelessCensorTypeValues);
}
