using HarmonyLib;
using MiraAPI.Modifiers;
using Reactor.Utilities;
using System.Linq;
using TownOfUs.Extensions;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Utilities;
using TownOfUs;
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
            // Use 0.5f alpha for better visibility
            Coroutines.Start(MiscUtils.CoFlash(TownOfUsColors.Oracle, alpha: 0.5f));
        }
    }

    [HarmonyPatch(typeof(OracleRole), nameof(OracleRole.RpcOracleBlessNotify))]
    [HarmonyPostfix]
    public static void NotifyPostfix(PlayerControl oracle, PlayerControl source, PlayerControl target)
    {
        // Ensure EVERYONE involved sees a bright flash (alpha 0.5f)
        // Base mod uses 0.3f which might be too faint, so we overwrite/trigger it here
        if (target.AmOwner || oracle.AmOwner || source.AmOwner)
        {
            Coroutines.Start(MiscUtils.CoFlash(TownOfUsColors.Oracle, alpha: 0.5f));
        }
    }
}










