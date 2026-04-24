using System;
using MiraAPI.GameOptions;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Modifiers;
using TownOfUs;
using TownOfUs.Assets;
using TownOfUs.Modifiers.Game;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Universal;

public sealed class SpitefulModifier : UniversalGameModifier, IWikiDiscoverable
{
    public override string LocaleKey => "Spiteful";
    public override string ModifierName => TouLocale.Get($"ExtensionModifier{LocaleKey}");
    public override string IntroInfo => TouLocale.GetParsed($"ExtensionModifier{LocaleKey}IntroBlurb");
    public override LoadableAsset<Sprite>? ModifierIcon => TouExtensionModifierIcons.SpitefulModifierIcon;

    public override string GetDescription()
    {
        return TouLocale.GetParsed($"ExtensionModifier{LocaleKey}TabDescription");
    }

    public string GetAdvancedDescription()
    {
        var options = OptionGroupSingleton<SpitefulModifierOptions>.Instance;
        var description = TouLocale.GetParsed($"ExtensionModifier{LocaleKey}WikiDescription");

        var effectTypeName = TouLocale.Get("ExtensionModifierSpitefulEffectType");
        var effectTypeValue = options.SpitefulEffectType.Value switch
        {
            SpitefulEffectType.LowerVision => TouLocale.Get("ExtensionModifierSpitefulEffectTypeEnumLowerVision"),
            SpitefulEffectType.Slowness => TouLocale.Get("ExtensionModifierSpitefulEffectTypeEnumSlowness"),
            SpitefulEffectType.IncreasedCooldowns => TouLocale.Get("ExtensionModifierSpitefulEffectTypeEnumIncreasedCooldowns"),
            _ => options.SpitefulEffectType.Value.ToString()
        };

        var optionsText = MiscUtils.AppendOptionsText(GetType());
        var punishmentEffectLine = $"{effectTypeName}: {effectTypeValue}";
        var alreadyIncluded = !string.IsNullOrWhiteSpace(optionsText) &&
                              optionsText.Contains(punishmentEffectLine, StringComparison.Ordinal);

        if (string.IsNullOrWhiteSpace(optionsText))
        {
            description += $"\n\n<size=50%> \n</size><b>{TownOfUsColors.Vigilante.ToTextColor()}{TouLocale.Get("Options")}</color></b>";
            description += $"\n{punishmentEffectLine}";
        }
        else if (!alreadyIncluded)
        {
            var headerText = $"\n<size=50%> \n</size><b>{TownOfUsColors.Vigilante.ToTextColor()}{TouLocale.Get("Options")}</color></b>";
            var headerIndex = optionsText.IndexOf(headerText, StringComparison.Ordinal);

            if (headerIndex >= 0)
            {
                var afterHeader = headerIndex + headerText.Length;
                description += optionsText.Substring(0, afterHeader);
                description += $"\n{punishmentEffectLine}";
                description += optionsText.Substring(afterHeader);
            }
            else
            {
                description += $"\n\n<size=50%> \n</size><b>{TownOfUsColors.Vigilante.ToTextColor()}{TouLocale.Get("Options")}</color></b>";
                description += $"\n{punishmentEffectLine}";
                description += optionsText;
            }
        }
        else
        {
            description += optionsText;
        }

        return description;
    }

    public override Color FreeplayFileColor => new Color32(255, 100, 0, 255);
    public override ModifierFaction FactionType => ModifierFaction.UniversalPassive;
    public List<CustomButtonWikiDescription> Abilities { get; } = [];

    public override int GetAssignmentChance()
    {
        return (int)OptionGroupSingleton<UniversalModifierOptions>.Instance.SpitefulChance;
    }

    public override int GetAmountPerGame()
    {
        return (int)OptionGroupSingleton<UniversalModifierOptions>.Instance.SpitefulAmount;
    }

    public override bool IsModifierValidOn(RoleBehaviour role)
    {
        if (!base.IsModifierValidOn(role))
        {
            return false;
        }

        var player = role.Player;
        if (player == null || player.Data == null || player.Data.IsDead)
        {
            return false;
        }

        return true;
    }
}
