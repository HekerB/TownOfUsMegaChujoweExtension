using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public enum VampireHunterBecomes
{
    Crewmate = 0,
    Sheriff,
    Veteran,
    Vigilante,
    Hunter
}

public sealed class VampireHunterOptions : AbstractOptionGroup<VampireHunterRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleVampireHunter", "Vampire Hunter");

    [ModdedNumberOption("ExtensionOptionVHStakeCooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float StakeCooldown { get; set; } = 25f;

    [ModdedNumberOption("ExtensionOptionVHMaxFailedStakes", 0f, 15f, 1f, MiraNumberSuffixes.None, zeroInfinity: true)]
    public float MaxFailedStakes { get; set; } = 5f;

    [ModdedToggleOption("ExtensionOptionVHCanStakeRoundOne")]
    public bool CanStakeRoundOne { get; set; } = false;

    [ModdedToggleOption("ExtensionOptionVHSelfKillOnFailure")]
    public bool SelfKillOnFailure { get; set; } = false;

    [ModdedEnumOption("ExtensionOptionVHBecomesOnVampDeath", typeof(VampireHunterBecomes))]
    public VampireHunterBecomes BecomeOnVampireDeath { get; set; } = VampireHunterBecomes.Crewmate;

    [ModdedNumberOption("ExtensionOptionVHMinVampires", 1f, 5f, 1f, MiraNumberSuffixes.None)]
    public float MinVampiresForSpawn { get; set; } = 2f;
}
