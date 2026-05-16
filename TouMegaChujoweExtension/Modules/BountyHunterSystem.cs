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
    public static bool HasWon { get; set; }
    public static bool GameEndedByBH { get; set; }

    public static void Reset()
    {
        HasWon = false;
        GameEndedByBH = false;
    }
}