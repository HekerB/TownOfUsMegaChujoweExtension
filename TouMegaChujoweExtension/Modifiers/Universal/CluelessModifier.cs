using MiraAPI.GameOptions;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Modifiers;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Assets;
using TownOfUs.Modifiers.Game;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Universal;

public sealed class CluelessModifier : UniversalGameModifier, IWikiDiscoverable
{
    public override string LocaleKey => "Clueless";
    public override string ModifierName => TouLocale.Get($"ExtensionModifier{LocaleKey}");
    public override string IntroInfo => TouLocale.GetParsed($"ExtensionModifier{LocaleKey}IntroBlurb");
    public override LoadableAsset<Sprite> ModifierIcon => TouExtensionModifierIcons.CluelessModifierIcon;

    public override string GetDescription()
    {
        return TouLocale.GetParsed($"ExtensionModifier{LocaleKey}TabDescription");
    }

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionModifier{LocaleKey}WikiDescription")
               + MiscUtils.AppendOptionsText(GetType());
    }

    public override Color FreeplayFileColor => new Color32(180, 180, 180, 255);
    public override ModifierFaction FactionType => ModifierFaction.UniversalPassive;
    public List<CustomButtonWikiDescription> Abilities { get; } = [];

    public override int GetAssignmentChance()
    {
        return (int)OptionGroupSingleton<UniversalModifierOptions>.Instance.CluelessChance;
    }

    public override int GetAmountPerGame()
    {
        return (int)OptionGroupSingleton<UniversalModifierOptions>.Instance.CluelessAmount;
    }

    public override bool IsModifierValidOn(RoleBehaviour role)
    {
        if (!base.IsModifierValidOn(role) || role is SnitchRole || role is ForestallerRole)
        {
            return false;
        }

        var player = role.Player;
        if (player == null || player.Data == null)
        {
            return false;
        }

        if (player.Data.IsDead)
        {
            return false;
        }

        if (role is HaunterRole || role is SpectreRole)
        {
            return false;
        }

        return true;
    }

    public override void OnActivate()
    {
        base.OnActivate();

        if (!Player.AmOwner)
        {
            return;
        }

        try
        {
            if (HudManager.Instance != null && HudManager.Instance.TaskPanel != null &&
                HudManager.Instance.TaskPanel.taskText != null)
            {
                HudManager.Instance.TaskPanel.taskText.text = string.Empty;
            }

            if (MapBehaviour.Instance != null)
            {
                MapBehaviour.Instance.taskOverlay?.Hide();
            }
        }
        catch (System.Exception ex)
        {
            Reactor.Utilities.Logger<TouMegaChujoweExtensionPlugin>.Error($"[CluelessModifier] Failed to hide tasks: {ex.Message}");
        }
    }
}
