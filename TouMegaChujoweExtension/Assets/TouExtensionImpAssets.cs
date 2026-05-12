using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace TouMegaChujoweExtension.Assets;

public static class TouExtensionImpAssets
{

    private const string ShortPath = "TouMegaChujoweExtension.Resources.Buttons";
    private const string HackerPath = "TouMegaChujoweExtension.Resources.Hacker";
    public static LoadableAsset<Sprite> SpellButtonSprite { get; } = new LoadableResourceAsset($"{ShortPath}.SpellButton.png");
    public static LoadableAsset<Sprite> LanternButtonSprite { get; } = new LoadableResourceAsset($"{ShortPath}.LanternButton.png");
    public static LoadableAsset<Sprite> HackerDownloadButtonSprite { get; } = new LoadableResourceAsset($"{HackerPath}.hackerdownload.png");
    public static LoadableAsset<Sprite> HackerJamButtonSprite { get; } = new LoadableResourceAsset($"{HackerPath}.HackerJam.png");
    public static LoadableAsset<Sprite> HackerDeviceGenericSprite { get; } = new LoadableResourceAsset($"{HackerPath}.evilcamera.png");
    public static LoadableAsset<Sprite> HackerAdminSkeldSprite { get; } = new LoadableResourceAsset($"{HackerPath}.eviladminskeld.png");
    public static LoadableAsset<Sprite> HackerAdminMiraSprite { get; } = new LoadableResourceAsset($"{HackerPath}.eviladminmira.png");
    public static LoadableAsset<Sprite> HackerAdminPolusSprite { get; } = new LoadableResourceAsset($"{HackerPath}.eviladminpolus.png");
    public static LoadableAsset<Sprite> HackerAdminAirshipSprite { get; } = new LoadableResourceAsset($"{HackerPath}.eviladminairship.png");
    public static LoadableAsset<Sprite> HackerAdminSubmergedSprite { get; } = new LoadableResourceAsset($"{HackerPath}.eviladminsubmerged.png");
    public static LoadableAsset<Sprite> HackerCamerasSprite { get; } = new LoadableResourceAsset($"{HackerPath}.evilcamera.png");
    public static LoadableAsset<Sprite> HackerDoorLogSprite { get; } = new LoadableResourceAsset($"{HackerPath}.evildoorlog.png");
    public static LoadableAsset<Sprite> HackerVitalsSprite { get; } = new LoadableResourceAsset($"{HackerPath}.evilvitals.png");
    public static LoadableAsset<Sprite> InjectorInjectButtonSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Buttons.Inject_Button.png");
    public static LoadableAsset<Sprite> DeceiveButtonSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Buttons.DecieveButton.png");
    public static LoadableAsset<Sprite> ConcealButtonSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Buttons.ConcealButton.png");
    public static LoadableAsset<Sprite> RcCarSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.RC_Icon.png", 50f);
    public static LoadableAsset<Sprite> RcXdDeployButton { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Buttons.Deploy_Button.png", 370f);
    public static LoadableAsset<Sprite> RcXdDetonateButton { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Buttons.Detonate_Button.png", 250f);
    public static LoadableAsset<Sprite> PoisonButtonSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Buttons.Poison_Button.png", 100f);
    public static LoadableAsset<Sprite> VineButtonSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Buttons.Vine_Button.png", 100f);
    public static LoadableAsset<Sprite> PoisonedButtonSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Buttons.Poisoned_Button.png", 100f);
    public static LoadableAsset<Sprite> KamikazeSuicideButtonSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Buttons.Suicide_Button.png", 170f);
    public static LoadableAsset<Sprite> SpeedyAbilitySprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Buttons.Speedy_Ability.png");
}










