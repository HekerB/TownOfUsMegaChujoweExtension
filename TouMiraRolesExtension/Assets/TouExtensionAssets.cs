using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace TouMiraRolesExtension.Assets
{
    public static class TouExtensionAssets
    {
        public static LoadableAsset<Sprite> HexedSprite { get; } = new LoadableResourceAsset("TouMiraRolesExtension.Resources.Hexed.png");
        public static LoadableAsset<Sprite> ObjectionButtonSprite { get; } = new LoadableResourceAsset("TouMiraRolesExtension.Resources.Buttons.Object.png");
        public static LoadableAsset<Sprite> ObjectionAnimationSprite { get; } = new LoadableResourceAsset("TouMiraRolesExtension.Resources.Objection!.png");
        public static LoadableAsset<Sprite> LanternSprite { get; } = new LoadableResourceAsset("TouMiraRolesExtension.Resources.Lantern.png");
        public static LoadableAsset<Sprite> BrokenLanternSprite { get; } = new LoadableResourceAsset("TouMiraRolesExtension.Resources.BrokenLantern.png");
        public static LoadableAsset<Sprite> MirageRoleIcon { get; } = new LoadableResourceAsset("TouMiraRolesExtension.Resources.Mirage_Role_Icon.png", 200f);
        public static LoadableAsset<Sprite> TrapperRoleIcon { get; } = new LoadableResourceAsset("TouMiraRolesExtension.Resources.Trapper_Role_Icon.png", 200f);
        public static LoadableAsset<Sprite> ForestallerRoleIcon { get; } = new LoadableResourceAsset("TouMiraRolesExtension.Resources.Forestaller_Role_Icon.png", 200f);
        public static LoadableAsset<Sprite> CluelessModifierIcon { get; } = new LoadableResourceAsset("TouMiraRolesExtension.Resources.Clueless_Modifier_Icon.png", 200f);
        public static LoadableAsset<Sprite> SpitefulModifierIcon { get; } = new LoadableResourceAsset("TouMiraRolesExtension.Resources.Spiteful_Modifier_Icon.png", 200f);
        public static LoadableAsset<Sprite> ScavengerRoleIcon { get; } = new LoadableResourceAsset("TouMiraRolesExtension.Resources.Scavenger_Icon.png", 200f);
        public static LoadableAsset<Sprite> ScavengerEatButtonSprite { get; } = new LoadableResourceAsset("TouMiraRolesExtension.Resources.Buttons.Scavenger_Eat_Button.png");
        public static LoadableAsset<Sprite> ScavengerScavengeButtonSprite { get; } = new LoadableResourceAsset("TouMiraRolesExtension.Resources.Buttons.Scavenger_FindBody_Button.png");
    }
}