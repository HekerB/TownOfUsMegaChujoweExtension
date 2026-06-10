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
    public static Color Vanisher => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(88, 88, 88, 255);
    public static Color Bodyguard => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(0, 51, 128, 255);
    public static Color Sage => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(200, 162, 255, 255);
    public static Color VampireHunter => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(150, 100, 200, 255);
    public static Color Falcon => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(255, 255, 255, 255);
    public static Color Doctor => Trapper;
    public static Color SpiritMaster => TownOfUsColors.SoulCollector;
    public static Color Arcanist => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(110, 15, 110, 255);
    public static Color Portalmaker => SerialKiller;
    public static Color Gardener => Trapper;
    //neutrals
    public static Color SerialKiller => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(40, 80, 160, 255);
    public static Color Vulture => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(139, 69, 19, 255);
    public static Color Pelican => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(106, 21, 171, 255);
    public static Color Pirate => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(235, 193, 62, 255);
    public static Color Joker => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(138, 43, 226, 255);
    public static Color Shifter => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(186, 186, 186, 255);
    public static Color Pope => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(255, 215, 0, 255);
    public static Color Shroud => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(77, 153, 230, 255);
    public static Color Doppelganger => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(212, 176, 56, 255);
    public static Color BountyHunter => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(0, 18, 97, 255);
    public static Color Jackal => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(83, 80, 100, 255);
    public static Color Baker => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(244, 167, 82, 255);
    public static Color Berserker => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(136, 8, 20, 255);
    public static Color War => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(94, 57, 41, 255);
    public static Color Famine => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(2, 48, 32, 255);
    public static Color SoulCollector => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(25, 25, 47, 255);
    public static Color Death => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(32, 32, 32, 255);
    public static Color Innocent => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(255, 141, 168, 255);
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
    public static Color Burrower => Palette.ImpostorRed;
    // Modifiers
    public static Color Venomous => new Color32(0, 200, 90, 255);
    public static Color Publicity => new Color32(51, 179, 179, 255);
    public static Color DeathNote => new Color32(42, 10, 42, 255);
    public static Color Sidekick => new Color32(167, 155, 255, 255);
}




