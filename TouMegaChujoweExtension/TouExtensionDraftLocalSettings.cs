using BepInEx.Configuration;
using MiraAPI.LocalSettings;
using TownOfUs.LocalSettings.Attributes;
using TownOfUs.LocalSettings.SettingTypes;

namespace TouMegaChujoweExtension;

public class TouExtensionDraftLocalSettings(ConfigFile config) : LocalSettingsTab(config)
{
    public override string TabName => "ToU: Draft";
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
        TabIcon = TouRoleIcons.Traitor
    };

    [LocalizedLocalEnumSetting("ExtensionLocalSettingDraftAlert", names: ["DraftAlertStartOnly", "DraftAlertStartAndEnd", "DraftAlertEndOnly", "DraftAlertNever"])]
    public ConfigEntry<DraftAlertTiming> DraftAlertTiming { get; private set; } =
        config.Bind("Draft", "DraftAlertTiming", TouMegaChujoweExtension.DraftAlertTiming.StartAndEnd);

    [LocalizedLocalToggleSetting("ExtensionLocalSettingStartDraftMusicMuted")]
    public ConfigEntry<bool> StartDraftMusicMuted { get; private set; } =
        config.Bind("Draft", "StartDraftMusicMuted", false);
}

public enum DraftAlertTiming
{
    StartOnly,
    StartAndEnd,
    EndOnly,
    Never
}
