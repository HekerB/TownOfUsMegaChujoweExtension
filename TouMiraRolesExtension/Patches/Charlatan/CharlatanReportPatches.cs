using HarmonyLib;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Roles.Impostor;
using TownOfUs.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using System.Linq;

namespace TouMiraRolesExtension.Patches.Charlatan;

[HarmonyPatch]
public static class CharlatanReportPatches
{
    [HarmonyPatch(typeof(ReportButton), nameof(ReportButton.DoClick))]
    [HarmonyPrefix]
    public static bool ReportButtonDoClickPrefix(ReportButton __instance)
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data?.Role is not CharlatanRole)
        {
            return true;
        }

        var allBodies = Object.FindObjectsOfType<DeadBody>();
        DeadBody? targetBody = null;
        foreach (var body in allBodies)
        {
            if (CharlatanDeceiveSystem.CanDeceiveReport(player.PlayerId, body.ParentId))
            {
                targetBody = body;
                break;
            }
        }

        if (targetBody != null)
        {
            var bodyPlayer = MiscUtils.PlayerById(targetBody.ParentId);
            if (bodyPlayer != null)
            {
                player.CmdReportDeadBody(bodyPlayer.Data);
            }
            return false;
        }

        return true;
    }

    // Patch MaxReportDistance to reduce it when there are concealed bodies nearby
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MaxReportDistance), MethodType.Getter)]
    [HarmonyPostfix]
    public static void MaxReportDistanceGetterPostfix(PlayerControl __instance, ref float __result)
    {
        // Check all nearby bodies to see if any are concealed
        var allBodies = Object.FindObjectsOfType<DeadBody>();
        var playerPos = (Vector2)__instance.transform.position;
        var originalDistance = __result;
        var minConcealedDistance = originalDistance;
        var foundConcealed = false;

        foreach (var body in allBodies)
        {
            if (body == null || body.Reported)
            {
                continue;
            }

            if (!CharlatanConcealSystem.IsBodyConcealed(body.ParentId))
            {
                continue;
            }

            var bodyPos = (Vector2)body.transform.position;
            var distance = Vector2.Distance(playerPos, bodyPos);

            // If this concealed body is within normal report range, we need to reduce MaxReportDistance
            // so that only bodies within the reduced distance can be reported
            if (distance <= originalDistance)
            {
                foundConcealed = true;
                var concealedRange = CharlatanConcealSystem.GetConcealedReportRange(body.ParentId);
                var effectiveDistance = originalDistance * concealedRange;
                
                // Track the minimum effective distance - this ensures concealed bodies require closer proximity
                if (effectiveDistance < minConcealedDistance)
                {
                    minConcealedDistance = effectiveDistance;
                }
            }
        }

        // If there are concealed bodies in range, reduce MaxReportDistance
        // This makes it so you need to be closer to report any body when concealed bodies are nearby
        if (foundConcealed)
        {
            __result = minConcealedDistance;
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    public static void HudManagerUpdatePostfix()
    {
        CharlatanConcealSystem.UpdateBodyTransparency();
    }
}

