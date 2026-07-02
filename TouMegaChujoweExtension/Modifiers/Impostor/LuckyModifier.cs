using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Game;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class LuckyModifier : TouGameModifier, IWikiDiscoverable
{
    public override string LocaleKey => "Lucky";
    public override string ModifierName => TouLocale.Get($"ExtensionModifier{LocaleKey}");
    public override string IntroInfo => TouLocale.GetParsed($"ExtensionModifier{LocaleKey}IntroBlurb");
    public override LoadableAsset<Sprite>? ModifierIcon => TouExtensionModifierIcons.LuckyModifierIcon;
    public override Color FreeplayFileColor => new Color32(255, 200, 0, 255);
    public override ModifierFaction FactionType => ModifierFaction.ImpostorPassive;

    public List<CustomButtonWikiDescription> Abilities { get; } = [];

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

    public override int GetAssignmentChance()
    {
        return (int)OptionGroupSingleton<ImpostorModifierOptions>.Instance.LuckyChance;
    }

    public override int GetAmountPerGame()
    {
        return (int)OptionGroupSingleton<ImpostorModifierOptions>.Instance.LuckyAmount;
    }

    public override bool IsModifierValidOn(RoleBehaviour role)
    {
        if (!base.IsModifierValidOn(role))
            return false;

        if (!role.IsImpostor())
            return false;

        var roleName = role.GetType().Name;
        return roleName is not ("WarlockRole" or "OutlawRole" or "ScavengerRole");
    }

    public static float GetRandomKillCooldown()
    {
        var opts = OptionGroupSingleton<LuckyModifierOptions>.Instance;
        var min = opts.LuckyMinCooldown;
        var max = opts.LuckyMaxCooldown;

        if (min > max)
            (min, max) = (max, min);

        return UnityEngine.Random.Range(min, max);
    }
}














