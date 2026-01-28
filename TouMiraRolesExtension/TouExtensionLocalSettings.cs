using BepInEx.Configuration;
using MiraAPI.LocalSettings;
using TouMiraRolesExtension.Assets;
using TownOfUs.LocalSettings.Attributes;
using TownOfUs.LocalSettings.SettingTypes;

namespace TouMiraRolesExtension;

public class TouExtensionLocalSettings(ConfigFile config) : LocalSettingsTab(config)
{
    public override string TabName => "TOU Extension";
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
        TabIcon = TouExtensionImpAssets.InjectorRole
    };

    [LocalizedLocalToggleSetting("ExtensionLocalSettingEnableNauseaCameraShake")]
    public ConfigEntry<bool> EnableNauseaCameraShake { get; private set; } =
        config.Bind("Accessibility", "EnableNauseaCameraShake", true);
}