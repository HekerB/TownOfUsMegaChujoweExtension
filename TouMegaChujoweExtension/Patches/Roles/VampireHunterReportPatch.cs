using HarmonyLib;
using MiraAPI.GameOptions;
using System.Linq;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles;

[HarmonyPatch]
public static class VampireHunterReportPatch
{
    private static readonly Vector2 BodyOffset = new(-0.2f, -0.25f);

    private static DeadBody GetClosestReportableBody(PlayerControl player)
    {
        if (player == null || player.Data.IsDead || !GameManager.Instance.CanReportBodies() || Minigame.Instance) return null;
        
        DeadBody closestBody = null;
        float closestDistance = player.MaxReportDistance;
        Vector2 playerPos = player.GetTruePosition();
        
        foreach (var body in UnityEngine.Object.FindObjectsOfType<DeadBody>())
        {
            if (body == null || body.Reported) continue;
            
            Vector2 bodyPos = body.TruePosition + BodyOffset;
            float distance = Vector2.Distance(playerPos, bodyPos);
            
            if (distance < closestDistance)
            {
                // Wall check - 0.5f threshold where walls don't matter (standard Among Us logic)
                if (distance < 0.5f || !PhysicsHelpers.AnythingBetween(playerPos, body.TruePosition, Constants.ShipAndObjectsMask, false))
                {
                    closestDistance = distance;
                    closestBody = body;
                }
            }
        }
        return closestBody;
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    public static void HudManagerUpdatePostfix(HudManager __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data == null || local.Data.Role is not VampireHunterRole) return;
        
        // If the option is ON, we don't need to hide anything
        var reportButton = __instance.ReportButton;
        if (reportButton == null) return;
        if (OptionGroupSingleton<VampireHunterOptions>.Instance.CanSelfReport) return;

        var body = GetClosestReportableBody(local);
        if (body != null)
        {
            if (StakeButton.HunterKilledVictims.Contains(body.ParentId))
            {
                reportButton.SetDisabled();
            }
            else
            {
                reportButton.SetEnabled();
            }
        }
    }


    [HarmonyPatch(typeof(ReportButton), nameof(ReportButton.DoClick))]
    [HarmonyPrefix]
    public static bool DoClickPrefix()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data == null || local.Data.Role is not VampireHunterRole) return true;

        // If the option is ON, reporting is allowed
        if (OptionGroupSingleton<VampireHunterOptions>.Instance.CanSelfReport) return true;

        var body = GetClosestReportableBody(local);
        if (body != null && StakeButton.HunterKilledVictims.Contains(body.ParentId))
        {
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(DeadBody), nameof(DeadBody.OnClick))]
    [HarmonyPrefix]
    public static bool DeadBodyOnClickPrefix(DeadBody __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data == null || local.Data.Role is not VampireHunterRole) return true;

        // If the option is ON, reporting is allowed
        if (OptionGroupSingleton<VampireHunterOptions>.Instance.CanSelfReport) return true;

        if (StakeButton.HunterKilledVictims.Contains(__instance.ParentId))
        {
            return false;
        }
        return true;
    }
}


















