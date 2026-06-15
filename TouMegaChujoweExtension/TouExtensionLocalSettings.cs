using BepInEx.Configuration;
using MiraAPI.LocalSettings;
using TownOfUs.LocalSettings.Attributes;
using TownOfUs.LocalSettings.SettingTypes;

namespace TouMegaChujoweExtension;

public class TouExtensionLocalSettings : LocalSettingsTab
{
    public TouExtensionLocalSettings(ConfigFile config) : base(config)
    {
        EnableNauseaCameraShake = config.Bind("Accessibility", "EnableNauseaCameraShake", true);
        UseLegacyGuessDeathAnimation = config.Bind("Visuals", "UseLegacyGuessDeathAnimation", false);
        UseClassicAssassinGuessing = config.Bind("Visuals", "UseClassicAssassinGuessing", false);
        MapColor = config.Bind("Visuals", "MapColor", MapColorType.PlayerColor);
        CensorModName = config.Bind("Visuals", "CensorModName", true);
        UsePolishLanguage = config.Bind("Localization", "UsePolishLanguage", false);

        UsePolishLanguage.SettingChanged += (s, e) => Modules.ExtensionLocale.SearchInternalLocale();
    }

    public override string TabName => TouMegaChujoweExtensionPlugin.CensorVisibleText("ToU: Chujowe");
    protected override bool ShouldCreateLabels => true;

    public override void Open()
    {
        base.Open();

        foreach (var entry in TownOfUs.Modules.Localization.TouLocale.LocalizedToggles)
        {
            var toggleObject = entry.Key;
            LocalizedLocalToggleSetting.UpdateToggleText(toggleObject.Text, entry.Value, toggleObject.onState);
        }

        foreach (var entry in TownOfUs.Modules.Localization.TouLocale.LocalizedSliders)
        {
            var sliderObject = entry.Key;
            sliderObject.SliderObject.Title.text =
                LocalizedLocalSliderSetting.GetLocalizedValueText(sliderObject, sliderObject.LocaleKey);
        }
    }

    public override LocalSettingTabAppearance TabAppearance => new()
    {
        TabIcon = TouExtensionAssets.ExtensionLogo
    };

    [LocalizedLocalToggleSetting("ExtensionLocalSettingEnableNauseaCameraShake")]
    public ConfigEntry<bool> EnableNauseaCameraShake { get; private set; }

    [LocalizedLocalToggleSetting("ExtensionLocalSettingUseLegacyGuessDeathAnimation")]
    public ConfigEntry<bool> UseLegacyGuessDeathAnimation { get; private set; }

    [LocalizedLocalToggleSetting("ExtensionLocalSettingUseClassicAssassinGuessing")]
    public ConfigEntry<bool> UseClassicAssassinGuessing { get; private set; }

    [LocalizedLocalEnumSetting("ExtensionLocalSettingMapColor", names: ["MapColorOff", "MapColorRole", "MapColorPlayer"])]
    public ConfigEntry<MapColorType> MapColor { get; private set; }

    [LocalizedLocalToggleSetting("ExtensionLocalSettingCensorModName")]
    public ConfigEntry<bool> CensorModName { get; private set; }

    [LocalizedLocalToggleSetting("ExtensionLocalSettingUsePolishLanguage")]
    public ConfigEntry<bool> UsePolishLanguage { get; private set; }
}

public enum MapColorType
{
    Off,
    Role,
    PlayerColor
}
