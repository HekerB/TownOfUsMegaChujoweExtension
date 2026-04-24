using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;

namespace TouMegaChujoweExtension.Options;

public sealed class GeneralOptions : AbstractOptionGroup
{
    public override string GroupName => "General";
    public override uint GroupPriority => 1;

    [ModdedToggleOption("Lawyer/Client Gets A Private Chat")]
    public bool LawyerChat { get; set; } = true;

    [ModdedToggleOption("Lovers Get A Private Chat In Meetings")]
    public bool LoversChat { get; set; } = true;
}