using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate
{
    public enum AgentAppearance
    {
        Impostor,
        Crewpostor
    }

    public sealed class AgentOptions : AbstractOptionGroup<AgentRole>
    {
        public override string GroupName => TouLocale.Get("ExtensionRoleAgent", "Agent");

        private static readonly string[] AgentAppearanceNames =
        [
            "Impostor",
            "Crewpostor"
        ];

        public ModdedEnumOption<AgentAppearance> AppearsAs { get; } =
            new("ExtensionOptionAgentAppearsAs", AgentAppearance.Crewpostor, AgentAppearanceNames);

        [ModdedToggleOption("ExtensionOptionAgentCanVent")]
        public bool CanVent { get; set; } = false;

        [ModdedToggleOption("ExtensionOptionAgentCanUseImpostorChat")]
        public bool CanUseImpostorChat { get; set; } = true;

        [ModdedToggleOption("ExtensionOptionAgentImpostorsCanKillEachOther")]
        public bool ImpostorsCanKillEachOther { get; set; } = false;
    }
}
