using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace TouMegaChujoweExtension.Assets
{
    public static class TouExtensionCrewAssets
    {
        private const string ShortPath = "TouMegaChujoweExtension.Resources.Buttons";
        public static LoadableAsset<Sprite> DecoyButtonSprite { get; } = new LoadableResourceAsset($"{ShortPath}.Decoy_Button.png", 100f);
        public static LoadableAsset<Sprite> EvokerBlindButtonSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Buttons.BlindButton_Icon.png", 100f);
        public static LoadableAsset<Sprite> EvokerVerifyButtonSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Buttons.VerifyButton_Icon.png", 100f);
        public static LoadableAsset<Sprite> VanishButtonSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Buttons.Vanish_Button.png", 100f);
        public static LoadableAsset<Sprite> UnvanishButtonSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Buttons.Unvanish_Button.png", 100f);
        public static LoadableAsset<Sprite> GuardButtonSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Buttons.Guard_Icon.png", 200f);
        public static LoadableAsset<Sprite> BacklashButtonSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Buttons.Backlash_Icon.png", 80f);
        public static LoadableAsset<Sprite> GuardShieldSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Anims.Guard_Anim_Front.png", 150f);
        public static LoadableAsset<Sprite> ZoomOutButtonSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Buttons.Zoom_Out_Icon.png", 400f);
        public static LoadableAsset<Sprite> ZoomInButtonSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Buttons.Zoom_Out_Icon.png", 400f);
        public static LoadableAsset<Sprite> StakeButtonIcon { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Buttons.VH_Stake_Button.png", 350f);
        public static LoadableAsset<Sprite> DoctorInjectButtonSprite { get; } = new LoadableResourceAsset($"{ShortPath}.Doctor_Inject_Button.png", 200f);
        public static LoadableAsset<Sprite> PortalSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.portal.png", 100f);
        public static LoadableAsset<Sprite> PortalPlaceButtonSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.PortalMaker.png", 100f);
        public static LoadableAsset<Sprite> GardenerButtonSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Buttons.Gardener_Button.png", 200f);
    }
}