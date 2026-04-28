using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace TouMegaChujoweExtension.Assets
{
    public static class TouExtensionIcons
    {
        //crewmates
        public static LoadableAsset<Sprite> MirageRoleIcon { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Mirage_Role_Icon.png", 200f);
        public static LoadableAsset<Sprite> TrapperRoleIcon { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Trapper_Role_Icon.png", 200f);
        public static LoadableAsset<Sprite> ForestallerRoleIcon { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Forestaller_Role_Icon.png", 200f);
        public static LoadableAsset<Sprite> PresidentRoleIcon { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.President_Role_Icon.png", 200f);
        public static LoadableAsset<Sprite> EvokerRoleIcon { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Evoker_Role_Icon.png", 200f);
        public static LoadableAsset<Sprite> VanisherRoleIcon { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Vanisher_Role_Icon.png", 200f);
        public static LoadableAsset<Sprite> BodyguardRoleIcon { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Bodyguard_Icon.png");
        public static LoadableAsset<Sprite> FalconRoleIcon { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Falcon_Role_Icon.png", 350f);
        public static LoadableAsset<Sprite> SageRoleIcon { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Sage_Icon.png", 70f);
        public static LoadableAsset<Sprite> VampireHunterRoleIcon { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.VampireHunter_Role_Icon.png", 100f);
        // TODO: Replace with custom Doctor icons when available
        public static LoadableAsset<Sprite> DoctorRoleIcon => InjectorRole; // Placeholder
        public static LoadableAsset<Sprite> DoctorHealButtonIcon => InjectorRole; // Placeholder
        //neutrals
        public static LoadableAsset<Sprite> VultureRoleIcon { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Vulture_Icon.png", 200f);
        public static LoadableAsset<Sprite> PelicanRoleIcon { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Pelican_Role_Icon.png", 250f);
        public static LoadableAsset<Sprite> PirateRoleIcon { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Pirate_Role_Icon.png", 250f);
        public static LoadableAsset<Sprite> JokerRoleIcon { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Joker_Role_Icon.png", 250f);
        public static LoadableAsset<Sprite> PopeRoleIcon { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Pope_Role_Icon.png", 300f);
        public static LoadableAsset<Sprite> BountyHunterRoleIcon { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.BountyHunter_Role_Icon.png", 300f);
        public static LoadableAsset<Sprite> ShroudRoleIcon { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Shroud_Role_Icon.png", 100f);
        public static LoadableAsset<Sprite> DoppelgangerRoleIcon { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Doppelganger_Icon.png", 100f);
        //impostors
        public static LoadableAsset<Sprite> OutlawRoleIcon { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Outlaw_Role_Icon.png", 800f);
        public static LoadableAsset<Sprite> HackerRole { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Hacker.Hacker_Role.png");
        public static LoadableAsset<Sprite> InjectorRole { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Injector_Role_Icon.png", 200f);
        public static LoadableAsset<Sprite> CharlatanRole { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Charlatan_Role_Icon.png", 200f);
        public static LoadableAsset<Sprite> OutlawRole { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Outlaw_Role_Icon.png", 200f);
        public static LoadableAsset<Sprite> RcXdRoleIcon { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.RC-XD_Icon.png", 60f);
        public static LoadableAsset<Sprite> PoisonerRole { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Poisoner_Icon.png");
        public static LoadableAsset<Sprite> KamikazeRole { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Kamikaze_Role_Icon.png", 135f);
    }
}
