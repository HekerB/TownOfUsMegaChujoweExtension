using HarmonyLib;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs;
using TownOfUs.Utilities;
using TownOfUs.Extensions;
using MiraAPI.Modifiers;
using System.Linq;
using Reactor.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Crewmate;

[HarmonyPatch(typeof(OracleRole), nameof(OracleRole.RpcOracleBless))]
public static class OracleFlashPatch
{
    public static void Postfix(PlayerControl exiled)
    {
        var mod = exiled.GetModifiers<OracleBlessedModifier>().FirstOrDefault();
        if (mod != null)
        {
            // Trigger the Oracle flash for everyone when a player is saved from exile
            Coroutines.Start(MiscUtils.CoFlash(TownOfUsColors.Oracle));
        }
    }

    [HarmonyPatch(typeof(OracleRole), nameof(OracleRole.RpcOracleBlessNotify))]
    [HarmonyPostfix]
    public static void NotifyPostfix(PlayerControl oracle, PlayerControl source, PlayerControl target)
    {
        // Ensure the target also sees the flash when they are saved from a guess/kill
        if (target.AmOwner && !oracle.AmOwner && !source.AmOwner)
        {
            Coroutines.Start(MiscUtils.CoFlash(TownOfUsColors.Oracle));
        }
    }
}
