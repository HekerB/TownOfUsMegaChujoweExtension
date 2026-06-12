using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions;

namespace TouMegaChujoweExtension.Options;

public sealed class ExtensionGeneralOptions : AbstractOptionGroup
{
    public override string GroupName => "General";
    public override uint GroupPriority => 1;

    [ModdedToggleOption("ExtensionOptionGeneralApocalypseMeetingChat")]
    public bool ApocalypseMeetingChat { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionGeneralJackalChat")]
    public bool JackalChat { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionGeneralLawyerChat")]
    public bool LawyerChat { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionGeneralLoversChat")]
    public bool LoversChat { get; set; } = false;

    [ModdedToggleOption("ExtensionOptionGeneralRecruitsKnowEachOther")]
    public bool RecruitsKnowEachOther { get; set; } = false;

    [ModdedToggleOption("ExtensionOptionGeneralLoversKnowEachOthersRoles")]
    public bool LoversKnowEachOthersRoles { get; set; } = true;
}
