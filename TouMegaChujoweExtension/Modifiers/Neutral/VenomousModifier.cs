using MiraAPI.GameOptions;
using MiraAPI.Utilities.Assets;
using TownOfUs.Interfaces;
using TownOfUs.Modifiers.Game;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

public sealed class VenomousModifier : UniversalGameModifier, IWikiDiscoverable, IColoredModifier, IGuessable
{
    public Color ModifierColor => TouExtensionColors.Venomous;
    public override string LocaleKey => "Venomous";
    public override string ModifierName => TouLocale.Get($"ExtensionModifier{LocaleKey}");
    public override string IntroInfo => TouLocale.GetParsed($"ExtensionModifier{LocaleKey}IntroBlurb");
    public override LoadableAsset<Sprite>? ModifierIcon => TouExtensionModifierIcons.VenomousModifierIcon;
    public string GuesserName => ModifierName;
    public Color GuesserColor => ModifierColor;
    public Sprite? GuesserIcon => ModifierIcon?.LoadAsset();
    public bool CanBeGuessed => GetAmountPerGame() > 0;

    public override string GetDescription()
    {
        return TouLocale.GetParsed($"ExtensionModifier{LocaleKey}TabDescription");
    }

    public string GetAdvancedDescription()
    {
        var description = TouLocale.GetParsed($"ExtensionModifier{LocaleKey}WikiDescription");
        description += MiscUtils.AppendOptionsText(GetType());
        return description;
    }

    public override Color FreeplayFileColor => new Color32(0, 200, 90, 255);
    public override ModifierFaction FactionType => ModifierFaction.NeutralPassive;
    public List<CustomButtonWikiDescription> Abilities { get; } = [];

    public override int GetAssignmentChance()
    {
        return (int)OptionGroupSingleton<NeutralModifierOptions>.Instance.VenomousChance;
    }

    public override int GetAmountPerGame()
    {
        return (int)OptionGroupSingleton<NeutralModifierOptions>.Instance.VenomousAmount;
    }

    public override bool IsModifierValidOn(RoleBehaviour role)
    {
        if (!base.IsModifierValidOn(role))
            return false;

        return role.GetRoleAlignment() == RoleAlignment.NeutralKilling;
    }
}














