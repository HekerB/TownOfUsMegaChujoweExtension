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
        RenameDoomsayerToForeteller = config.Bind("Visuals", "RenameDoomsayerToForeteller", true);
        UsePolishLanguage = config.Bind("Localization", "UsePolishLanguage", false);

        UsePolishLanguage.SettingChanged += (s, e) => Modules.ExtensionLocale.SearchInternalLocale();

        JokerPiPLocation = config.Bind("Joker", "JokerPiPLocation", TouMegaChujoweExtension.JokerPiPLocation.BottomRight);
        JokerPiPSize = config.Bind("Joker", "JokerPiPSize", TouMegaChujoweExtension.JokerPiPSize.Normal);
    }

    public override string TabName => "ToU: Chujowe";
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
	
    [LocalizedLocalToggleSetting("ExtensionLocalSettingRenameDoomsayerToForeteller")]
    public ConfigEntry<bool> RenameDoomsayerToForeteller { get; private set; }

    [LocalizedLocalToggleSetting("ExtensionLocalSettingUsePolishLanguage")]
    public ConfigEntry<bool> UsePolishLanguage { get; private set; }

    [LocalizedLocalEnumSetting(names: new[]
    {
        "PiPLocationTopLeft", "PiPLocationMiddleLeft", "PiPLocationBottomLeft",
        "PiPLocationTopRight", "PiPLocationMiddleRight", "PiPLocationBottomRight",
        "PiPLocationDynamic"
    })]
    public ConfigEntry<JokerPiPLocation> JokerPiPLocation { get; private set; }

    [LocalizedLocalEnumSetting(names: new[]
    {
        "PiPSizeNormal", "PiPSizeSmall", "PiPSizeLarge"
    })]
    public ConfigEntry<JokerPiPSize> JokerPiPSize { get; private set; }

    public override void OnOptionChanged(ConfigEntryBase configEntry)
    {
        base.OnOptionChanged(configEntry);
    }
}

public enum JokerPiPLocation
{
    TopLeft,
    MiddleLeft,
    BottomLeft,
    TopRight,
    MiddleRight,
    BottomRight,
    Dynamic
}

public enum JokerPiPSize
{
    Small,
    Normal,
    Large
}










