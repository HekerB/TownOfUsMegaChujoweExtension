using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using System.Linq;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modules;

public static class BountyHunterSystem
{
    // // private static readonly BepInEx.Logging.ManualLogSource Log =
        // // BepInEx.Logging.Logger.CreateLogSource("BH");

    public static bool HasWon { get; set; }
    public static bool GameEndedByBH { get; set; }

    public static void Reset()
    {
        HasWon = false;
        GameEndedByBH = false;
    }
}



















