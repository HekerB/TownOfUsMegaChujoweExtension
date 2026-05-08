using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;

namespace TouMegaChujoweExtension.Options;

public sealed class GeneralOptions : AbstractOptionGroup
{
    public override string GroupName => "General";
    public override uint GroupPriority => 1;

    [ModdedToggleOption("ExtensionOptionGeneralLawyerChat")]
    public bool LawyerChat { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionGeneralLoversChat")]
    public bool LoversChat { get; set; } = true;
}
