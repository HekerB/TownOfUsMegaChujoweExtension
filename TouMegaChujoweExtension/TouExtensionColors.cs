using TownOfUs;
using UnityEngine;

namespace TouMegaChujoweExtension;

public static class TouExtensionColors
{
    //crewmates
    public static Color Trapper => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(166, 209, 179, 255);
    public static Color Forestaller => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(241, 196, 15, 255);
    public static Color Mirage => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(222, 168, 94, 255);
    public static Color President => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(150, 100, 200, 255);
    public static Color Evoker => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(128, 179, 255, 255);
    public static Color Vanisher => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(145, 230, 184, 255);
    public static Color Bodyguard => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(0, 51, 128, 255);
    public static Color Sage => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(200, 162, 255, 255);
    public static Color VampireHunter => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(150, 100, 200, 255);
    public static Color Falcon => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(255, 255, 255, 255);
    public static Color Portalmaker => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(138, 43, 226, 255);
    public static Color Arcanist => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(120, 40, 140, 255);
    public static Color Gardener => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(50, 205, 50, 255);
    public static Color Doctor => Trapper;
    //neutrals
    public static Color SerialKiller => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(40, 80, 160, 255);
    public static Color Vulture => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(139, 69, 19, 255);
    public static Color Pelican => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(106, 21, 171, 255);
    public static Color Pirate => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(255, 247, 0, 255);
    public static Color Joker => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(138, 43, 226, 255);
    public static Color Shifter => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(186, 186, 186, 255);
    public static Color Pope => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(255, 215, 0, 255);
    public static Color Shroud => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(77, 153, 230, 255);

    public static Color Doppelganger => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(212, 176, 56, 255);
    public static Color BountyHunter => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(0, 18, 97, 255);
    //impostors
    public static Color Witch => Palette.ImpostorRed;
    public static Color Wraith => Palette.ImpostorRed;
    public static Color Hacker => Palette.ImpostorRed;
    public static Color Injector => Palette.ImpostorRed;
    public static Color Poisoner => Palette.ImpostorRed;
    public static Color Charlatan => Palette.ImpostorRed;
    public static Color Outlaw => Palette.ImpostorRed;
    public static Color RcXd => Palette.ImpostorRed;
    public static Color Astral => Palette.ImpostorRed;
    public static Color Speedy => Palette.ImpostorRed;
    public static Color Detonator => Palette.ImpostorRed;


    // Shield Flash Colors
    public static class ShieldFlashes
    {
        public static Color Medic => new Color32(0, 102, 0, 255);       // Dark Green
        public static Color Warden => new Color32(153, 0, 255, 255);    // Purple
        public static Color Cleric => new Color32(0, 255, 179, 255);    // Cyan/Teal
        public static Color Mirrorcaster => new Color32(144, 162, 195, 255); // Silver Blue
        public static Color Fairy => new Color32(102, 170, 243, 255);   // Light Blue
        public static Color Mercenary => new Color32(140, 102, 153, 255); // Mauve
        public static Color BodyguardFlash => new Color32(0, 35, 255, 255); // Vibrant Royal Blue (Distinct from Mirror)
        public static Color Oracle => new Color32(191, 0, 191, 255);    // Magenta
    }

    // Modifiers
    public static Color Ventable => new Color32(51, 179, 179, 255);
    public static Color Venomous => new Color32(0, 200, 90, 255);
    public static Color Publicity => new Color32(51, 179, 179, 255);
    public static Color DeathNote => new Color32(42, 10, 42, 255);
}