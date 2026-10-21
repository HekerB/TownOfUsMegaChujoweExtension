using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Game;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Game;

/// <summary>
/// Inverted controls for a number of meetings. Formerly "Drunk"; TOU-Mira 1.7.3 ships its own Drunk with the
/// same effect, so this one has its own name, icon and options and both can be enabled side by side.
/// </summary>
public sealed class WastedModifier : UniversalGameModifier, IUnguessable, IWikiDiscoverable
{
    public override string IdPart => "Wasted";
    public override string ModifierName => TouLocale.Get($"ExtensionModifier{IdPart}");
    public override string IntroInfo => TouLocale.GetParsed($"ExtensionModifier{IdPart}IntroBlurb");
    public override LoadableAsset<Sprite>? ModifierIcon => TouExtensionModifierIcons.WastedModifierIcon;

    private int _meetingsRemaining;

    public bool IsGuessable => false;
    public RoleBehaviour AppearAs => null!;

    public override string GetDescription()
    {
        return TouLocale.GetParsed($"ExtensionModifier{IdPart}TabDescription")
            .Replace("{meetings}", _meetingsRemaining.ToString());
    }

    public string GetAdvancedDescription()
    {
        var options = OptionGroupSingleton<WastedModifierOptions>.Instance;
        var description = TouLocale.GetParsed($"ExtensionModifier{IdPart}WikiDescription")
            .Replace("{meetings}", ((int)options.WastedDuration.Value).ToString());

        return description + MiscUtils.AppendOptionsText(GetType());
    }

    public override Color FreeplayFileColor => new Color32(180, 180, 180, 255);
    public override ModifierFaction FactionType => ModifierFaction.UniversalPassive;
    public List<CustomButtonWikiDescription> Abilities { get; } = [];

    public override int GetAssignmentChance()
    {
        return (int)OptionGroupSingleton<UniversalModifierOptions>.Instance.WastedChance.Value;
    }

    public override int GetAmountPerGame()
    {
        return (int)OptionGroupSingleton<UniversalModifierOptions>.Instance.WastedAmount;
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
        _meetingsRemaining = (int)OptionGroupSingleton<WastedModifierOptions>.Instance.WastedDuration.Value;
        base.OnActivate();
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














