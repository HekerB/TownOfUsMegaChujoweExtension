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

    [ModdedToggleOption("ExtensionOptionGeneralJackalChat")]
    public bool JackalChat { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionGeneralJackalSeesRecruitChat")]
    public bool JackalSeesRecruitChat { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionGeneralRecruitsKnowEachOther")]
    public bool RecruitsKnowEachOther { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionGeneralRecruitsHaveArrow")]
    public bool RecruitsHaveArrow { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionGeneralPreventVampiresWithJackal")]
    public bool PreventVampiresWithJackal { get; set; } = true;

}