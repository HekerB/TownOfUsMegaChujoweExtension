using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;

namespace TouMegaChujoweExtension.Options;

public sealed class ModCompatibilityOptions : AbstractOptionGroup
{
    public override string GroupName => "Mod Compatibility";
    public override uint GroupPriority => 99; // Put it at the bottom

    [ModdedToggleOption("ExtensionOptionCompatibilityDuplicateWarnings")]
    public bool DuplicateWarnings { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionCompatibilityDuplicateAutoKick")]
    public bool DuplicateAutoKick { get; set; } = true;
}
