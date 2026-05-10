using HarmonyLib;
using MiraAPI.GameOptions;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using System.Linq;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Charlatan;

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

    // Patch DeadBody.OnClick to check concealed distance
    [HarmonyPatch(typeof(DeadBody), nameof(DeadBody.OnClick))]
    [HarmonyPrefix]
    public static bool DeadBodyOnClickPrefix(DeadBody __instance)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || localPlayer.Data.IsDead)
        {
            return true;
        }

        if (__instance.Reported || !GameManager.Instance.CanReportBodies())
        {
            return false;
        }

        if (Minigame.Instance)
        {
            return false;
        }

        var localPosition = localPlayer.GetTruePosition();
        var offset = new Vector2(-0.2f, -0.25f);
        var bodyPosition = __instance.TruePosition + offset;
        var distance = Vector2.Distance(bodyPosition, localPosition);
        var reportable = false;

        var concealed = CharlatanConcealSystem.IsBodyConcealed(__instance.ParentId);
        var concealedRangeMultiplier = CharlatanConcealSystem.GetConcealedReportRange(__instance.ParentId);
        var concealRange = concealedRangeMultiplier > 0f ? localPlayer.MaxReportDistance * concealedRangeMultiplier : localPlayer.MaxReportDistance;
        var blocked = PhysicsHelpers.AnythingBetween(localPosition, __instance.TruePosition, Constants.ShipAndObjectsMask, false);

        if (distance < 0.5f)
        {
            reportable = true;
        }
        else if (!blocked)
        {
            if (concealed && distance < concealRange)
            {
                reportable = true;
            }

            if (!concealed && distance < localPlayer.MaxReportDistance)
            {
                reportable = true;
            }
        }

        if (reportable)
        {
            __instance.Reported = true;
            var bodyPlayer = MiscUtils.PlayerById(__instance.ParentId);
            if (bodyPlayer != null)
            {
                localPlayer.CmdReportDeadBody(bodyPlayer.Data);
            }
        }

        return false; // Skip original method
    }

    // Patch HudManager.Update to update report button visibility
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    public static void HudManagerUpdatePostfix(HudManager __instance)
    {
        CharlatanConcealSystem.UpdateBodyTransparency();
        UpdateReportButton(__instance);
    }

    private static float _lastReportUpdateTime = 0f;
    private static bool _lastReportableState = false;

    private static void UpdateReportButton(HudManager __instance)
    {
        if (Time.time - _lastReportUpdateTime < 0.1f)
        {
            __instance.ReportButton.SetActive(_lastReportableState);
            return;
        }
        _lastReportUpdateTime = Time.time;

        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || localPlayer.Data.IsDead)
        {
            return;
        }

        var truePosition = localPlayer.GetTruePosition();
        var offset = new Vector2(-0.2f, -0.25f);

        if (Minigame.Instance)
        {
            __instance.ReportButton.SetActive(false);
            return;
        }

        var reportable = false;

        foreach (var collider2D in Physics2D.OverlapCircleAll(truePosition, localPlayer.MaxReportDistance, Constants.PlayersOnlyMask))
        {
            if (collider2D.tag != "DeadBody")
            {
                continue;
            }

            if (!reportable)
            {
                var component = collider2D.GetComponent<DeadBody>();
                if (component == null || component.Reported)
                {
                    continue;
                }

                var pos = component.TruePosition + offset;
                var distance = Vector2.Distance(pos, truePosition);
                var concealed = CharlatanConcealSystem.IsBodyConcealed(component.ParentId);
                var concealedRangeMultiplier = CharlatanConcealSystem.GetConcealedReportRange(component.ParentId);
                var bodyConcealRange = concealedRangeMultiplier > 0f ? localPlayer.MaxReportDistance * concealedRangeMultiplier : localPlayer.MaxReportDistance;
                var blocked = PhysicsHelpers.AnythingBetween(truePosition, component.TruePosition, Constants.ShipAndObjectsMask, false);

                if (distance < 0.5f)
                {
                    reportable = true;
                }
                else if (!blocked)
                {
                    if (concealed && distance < bodyConcealRange)
                    {
                        reportable = true;
                    }

                    if (!concealed && distance < localPlayer.MaxReportDistance)
                    {
                        reportable = true;
                    }
                }
            }
        }

        _lastReportableState = reportable;
        __instance.ReportButton.SetActive(reportable);
    }
}

















