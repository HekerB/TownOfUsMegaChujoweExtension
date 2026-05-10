using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions;

namespace TouMegaChujoweExtension.Options;

public sealed class ExtensionGeneralOptions : AbstractOptionGroup
{
    public override string GroupName => "General";
    public override uint GroupPriority => 1;

    [ModdedToggleOption("ExtensionOptionGeneralLawyerChat")]
    public bool LawyerChat { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionGeneralLoversChat")]
    public bool LoversChat { get; set; } = true;
}











