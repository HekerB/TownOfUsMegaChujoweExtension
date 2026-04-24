using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Modifiers;
using TownOfUs;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Game;
using TownOfUs.Modules.Wiki;
using TownOfUs.Utilities;
using TownOfUs.Modules.Localization;
using TownOfUs.Options.Maps;
using TownOfUs.Utilities.Appearances;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Universal;

public sealed class ChildModifier : UniversalGameModifier, IWikiDiscoverable
{
    public override string LocaleKey => "Child";
    public override string ModifierName => TouLocale.Get($"ExtensionModifier{LocaleKey}");
    public override string IntroInfo => TouLocale.GetParsed($"ExtensionModifier{LocaleKey}IntroBlurb");
    public override LoadableAsset<Sprite>? ModifierIcon => TouExtensionModifierIcons.ChildModifierIcon;

    public override ModifierFaction FactionType => ModifierFaction.UniversalPassive;
    public override Color FreeplayFileColor => new Color32(255, 220, 100, 255);

    public float CurrentAge { get; set; }
    public float TimeSinceLastGrowth { get; set; }
    public bool IsAdult => CurrentAge >= (float)(int)Options.AdultAge;
    public bool WasAdult { get; set; }

    private ChildModifierOptions Options => OptionGroupSingleton<ChildModifierOptions>.Instance;

    private float GetCurrentSize()
    {
        const float minSize = 0.3f;
        const float maxSize = 0.7f;

        var startAge = (float)(int)Options.StartingAge;
        var adultAge = (float)(int)Options.AdultAge;

        if (adultAge <= startAge)
            return maxSize;

        var progress = Mathf.Clamp01((CurrentAge - startAge) / (adultAge - startAge));
        return Mathf.Lerp(minSize, maxSize, progress);
    }

    public override string GetDescription()
    {
        return TouLocale.GetParsed($"ExtensionModifier{LocaleKey}TabDescription")
            .Replace("<age>", $"{CurrentAge:F0}")
            .Replace("<adultAge>", $"{(int)Options.AdultAge}");
    }

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionModifier{LocaleKey}WikiDescription")
            .Replace("<startAge>", $"{(int)Options.StartingAge}")
            .Replace("<adultAge>", $"{(int)Options.AdultAge}")
            .Replace("<growthInterval>", $"{(int)Options.GrowthInterval}")
            + MiscUtils.AppendOptionsText(GetType());
    }

    public List<CustomButtonWikiDescription> Abilities { get; } = [];

    public override int GetAssignmentChance()
    {
        return (int)OptionGroupSingleton<UniversalModifierOptions>.Instance.ChildChance;
    }

    public override int GetAmountPerGame()
    {
        return (int)OptionGroupSingleton<UniversalModifierOptions>.Instance.ChildAmount;
    }

    public override void OnActivate()
    {
        CurrentAge = (float)(int)Options.StartingAge;
        TimeSinceLastGrowth = 0f;
        WasAdult = false;

        if (!IsAdult && !Player.HasModifier<InvulnerabilityModifier>())
        {
            Player.AddModifier<InvulnerabilityModifier>(false, false, false);
        }

        UpdateVisual();
    }

    public override void OnDeactivate()
    {
        if (Player != null && Player.HasModifier<InvulnerabilityModifier>())
        {
            Player.RemoveModifier<InvulnerabilityModifier>();
        }

        if (Player != null)
        {
            Player.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (Player == null || Player.HasDied())
            return;

        var growthInterval = (float)(int)Options.GrowthInterval;

        TimeSinceLastGrowth += Time.fixedDeltaTime;

        if (TimeSinceLastGrowth >= growthInterval)
        {
            TimeSinceLastGrowth -= growthInterval;
            CurrentAge += 1f;

            if (IsAdult && !WasAdult)
            {
                WasAdult = true;
                if (Player.HasModifier<InvulnerabilityModifier>())
                {
                    Player.RemoveModifier<InvulnerabilityModifier>();
                }
            }
        }

        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (Player == null || Player.HasDied())
            return;

        if (Player.GetAppearanceType() == TownOfUsAppearances.Camouflage &&
            OptionGroupSingleton<AdvancedSabotageOptions>.Instance.HidePlayerSizeInCamo)
            return;

        var size = GetCurrentSize();
        Player.transform.localScale = new Vector3(size, size, 1f);
    }
}
