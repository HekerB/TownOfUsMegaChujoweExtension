using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Modifiers;
using TownOfUs.Extensions;
using TownOfUs.Modifiers.Game;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Utilities;
using UnityEngine;
using TownOfUs.Assets;

namespace TouMegaChujoweExtension.Modifiers.Universal;

public sealed class DrunkModifier : UniversalGameModifier, IUnguessable, IWikiDiscoverable
{
    public override string LocaleKey => "Drunk";
    public override string ModifierName => TouLocale.Get($"ExtensionModifier{LocaleKey}");
    public override string IntroInfo => TouLocale.GetParsed($"ExtensionModifier{LocaleKey}IntroBlurb");
    public override LoadableAsset<Sprite>? ModifierIcon => TouModifierIcons.Drunk;

    private int _meetingsRemaining;

    public bool IsGuessable => false;
    public RoleBehaviour? AppearAs => null;

    public override string GetDescription()
    {
        return TouLocale.GetParsed($"ExtensionModifier{LocaleKey}TabDescription")
            .Replace("{meetings}", _meetingsRemaining.ToString());
    }

    public string GetAdvancedDescription()
    {
        var options = OptionGroupSingleton<DrunkModifierOptions>.Instance;
        var description = TouLocale.GetParsed($"ExtensionModifier{LocaleKey}WikiDescription")
            .Replace("{meetings}", ((int)options.DrunkDuration.Value).ToString());
        var optionsText = MiscUtils.AppendOptionsText(GetType());
        if (!string.IsNullOrWhiteSpace(optionsText))
        {
            description += optionsText;
        }

        return description;
    }

    public override Color FreeplayFileColor => new Color32(139, 69, 19, 255);
    public override ModifierFaction FactionType => ModifierFaction.UniversalPassive;
    public List<CustomButtonWikiDescription> Abilities { get; } = [];

    public override int GetAssignmentChance()
    {
        return (int)OptionGroupSingleton<UniversalModifierOptions>.Instance.DrunkChance.Value;
    }

    public override int GetAmountPerGame()
    {
        return (int)OptionGroupSingleton<UniversalModifierOptions>.Instance.DrunkAmount;
    }

    public override bool IsModifierValidOn(RoleBehaviour role)
    {
        if (!base.IsModifierValidOn(role))
            return false;

        var player = role.Player;
        if (player == null || player.Data == null || player.Data.IsDead)
            return false;

        return true;
    }

    public override void OnActivate()
    {
        _meetingsRemaining = (int)OptionGroupSingleton<DrunkModifierOptions>.Instance.DrunkDuration.Value;
    }

    public override void OnMeetingStart()
    {
        _meetingsRemaining--;
        if (_meetingsRemaining <= 0)
        {
            Player.RemoveModifier(this);
        }
    }
}
