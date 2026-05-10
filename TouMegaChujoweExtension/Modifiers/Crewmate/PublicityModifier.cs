using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TownOfUs.Interfaces;
using TownOfUs.Modifiers.Game;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Crewmate;

public sealed class PublicityModifier : TouGameModifier, IWikiDiscoverable, IColoredModifier, IGuessable
{
    public override string LocaleKey => "Publicity";
    public override string ModifierName => TouLocale.Get($"ExtensionModifier{LocaleKey}");
    public override string IntroInfo => TouLocale.GetParsed($"ExtensionModifier{LocaleKey}IntroBlurb");
    public override LoadableAsset<Sprite>? ModifierIcon => TouExtensionModifierIcons.PublicityVoteModifierIcon;

    public override ModifierFaction FactionType => ModifierFaction.CrewmatePassive;
    public override Color FreeplayFileColor => new Color32(140, 255, 255, 255);
    public Color ModifierColor => TouExtensionColors.Publicity;
    public string GuesserName => ModifierName;
    public Color GuesserColor => TouExtensionColors.Publicity;
    public Sprite? GuesserIcon => TouExtensionModifierIcons.PublicityVoteModifierIcon.LoadAsset();
    public bool CanBeGuessed => GetAmountPerGame() > 0;

    public override string GetDescription()
        => TouLocale.GetParsed($"ExtensionModifier{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
        => TouLocale.GetParsed($"ExtensionModifier{LocaleKey}WikiDescription");

    public List<CustomButtonWikiDescription> Abilities { get; } = [];

    public override int GetAssignmentChance()
        => (int)OptionGroupSingleton<CrewmateModifierOptions>.Instance.PublicityChance.Value;

    public override int GetAmountPerGame()
        => (int)OptionGroupSingleton<CrewmateModifierOptions>.Instance.PublicityAmount;

    public override bool IsModifierValidOn(RoleBehaviour role)
    {
        if (!base.IsModifierValidOn(role)) return false;
        if (!role.IsCrewmate()) return false;
        if (role is ProsecutorRole) return false;
        return true;
    }
}














