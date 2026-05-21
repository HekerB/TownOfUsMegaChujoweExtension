using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace TouMegaChujoweExtension.Assets
{
    public static class TouExtensionCrewAssets
    {
        private const string ShortPath = "TouMegaChujoweExtension.Resources.Buttons";
        public static LoadableAsset<Sprite> DecoyButtonSprite { get; } = new LoadableResourceAsset($"{ShortPath}.Decoy_Button.png");
        public static LoadableAsset<Sprite> EvokerBlindButtonSprite { get; } = new LoadableResourceAsset($"{ShortPath}.BlindButton_Icon.png", 100f);
        public static LoadableAsset<Sprite> EvokerVerifyButtonSprite { get; } = new LoadableResourceAsset($"{ShortPath}.VerifyButton_Icon.png", 100f);
        public static LoadableAsset<Sprite> VanishButtonSprite { get; } = new LoadableResourceAsset($"{ShortPath}.Vanish_Button.png", 100f);
        public static LoadableAsset<Sprite> UnvanishButtonSprite { get; } = new LoadableResourceAsset($"{ShortPath}.Unvanish_Button.png", 100f);
        public static LoadableAsset<Sprite> GuardButtonSprite { get; } = new LoadableResourceAsset($"{ShortPath}.Guard_Icon.png", 200f);
        public static LoadableAsset<Sprite> BacklashButtonSprite { get; } = new LoadableResourceAsset($"{ShortPath}.Backlash_Icon.png", 80f);
        public static LoadableAsset<Sprite> GuardShieldSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Anims.Guard_Anim_Front.png", 150f);
        public static LoadableAsset<Sprite> ZoomOutButtonSprite { get; } = new LoadableResourceAsset($"{ShortPath}.Zoom_Out_Icon.png", 400f);
        public static LoadableAsset<Sprite> ZoomInButtonSprite { get; } = new LoadableResourceAsset($"{ShortPath}.Zoom_Out_Icon.png", 400f);
        public static LoadableAsset<Sprite> StakeButtonIcon { get; } = new LoadableResourceAsset($"{ShortPath}.VH_Stake_Button.png", 350f);
        public static LoadableAsset<Sprite> DoctorInjectButtonSprite { get; } = new LoadableResourceAsset($"{ShortPath}.Doctor_Inject_Button.png", 100f);
        public static LoadableAsset<Sprite> SentinelPatrolSprite { get; } = new LoadableResourceAsset($"{ShortPath}.Patrol_Button.png", 100f);
        public static LoadableAsset<Sprite> ArcanistDrawButtonSprite { get; } = new LoadableResourceAsset($"{ShortPath}.Arcanist_Draw_Button.png", 100f);
        public static LoadableAsset<Sprite> BuilderButtonSprite { get; } = new LoadableResourceAsset($"{ShortPath}.VH_Stake_Button.png", 350f);
    }
}