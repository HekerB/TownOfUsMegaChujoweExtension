using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace TouMegaChujoweExtension.Assets
{
    public static class TouExtensionAssets
    {
        //logo
        public static LoadableAsset<Sprite> ExtensionLogo { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Extension_Logo_Icon.png", 600f);
        //draft
        public static LoadableAsset<Sprite> DraftBackground { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Draft_Background.png");
        public static LoadableAsset<Sprite> DraftRandomIcon { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Buttons.Draft_Random_Icon.png");
        //meeting 
        public static LoadableAsset<Sprite> HexedSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Hexed.png");
        public static LoadableAsset<Sprite> CycleBack { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.MeetingButtons.CycleBack.png");
        public static LoadableAsset<Sprite> CycleForward { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.MeetingButtons.CycleForward.png");
        public static LoadableAsset<Sprite> GuessButton { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.MeetingButtons.GuessButton.png");
        public static LoadableAsset<Sprite> ObjectionButtonSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Buttons.Object.png");
        public static LoadableAsset<Sprite> PirateDuelAttack1 { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.MeetingButtons.Pirate_Duel_Attack1.png");
        public static LoadableAsset<Sprite> PirateDuelAttack2 { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.MeetingButtons.Pirate_Duel_Attack2.png");
        public static LoadableAsset<Sprite> PirateDuelAttack3 { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.MeetingButtons.Pirate_Duel_Attack3.png");
        public static LoadableAsset<Sprite> PirateDuelDefend1 { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.MeetingButtons.Pirate_Duel_Defend1.png");
        public static LoadableAsset<Sprite> PirateDuelDefend2 { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.MeetingButtons.Pirate_Duel_Defend2.png");
        public static LoadableAsset<Sprite> PirateDuelDefend3 { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.MeetingButtons.Pirate_Duel_Defend3.png");
        //game
        public static LoadableAsset<Sprite> DeathNoteUISprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Death_Note_UI.png", 650f);
        public static LoadableAsset<Sprite> LanternSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Lantern.png");
        public static LoadableAsset<Sprite> BrokenLanternSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.BrokenLantern.png");
        public static LoadableAsset<Sprite> AxeSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Axe_Sprite.png");
    }
}
