using BepInEx.Configuration;
using MiraAPI.LocalSettings;
using MiraAPI.LocalSettings.Attributes;
using UnityEngine;

namespace TouMegaChujoweExtension;

public class TouExtensionDraftLocalSettings(ConfigFile config) : LocalSettingsTab(config)
{
    public override string TabName => "ToU: Draft";
    protected override bool ShouldCreateLabels => true;

    public override LocalSettingTabAppearance TabAppearance => new()
    {
        TabIcon = TouRoleIcons.Traitor,
        TabButtonHoverColor = Color.red
    };

    [LocalEnumSetting("ExtensionLocalSettingDraftAlert", names: ["DraftAlertStartOnly", "DraftAlertStartAndEnd", "DraftAlertEndOnly", "DraftAlertNever"])]
    public ConfigEntry<DraftAlertTiming> DraftAlertTiming { get; private set; } =
        config.Bind("Draft", "DraftAlertTiming", TouMegaChujoweExtension.DraftAlertTiming.StartAndEnd);

    [LocalToggleSetting("ExtensionLocalSettingStartDraftMusicMuted")]
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
