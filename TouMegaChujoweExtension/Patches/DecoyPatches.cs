using HarmonyLib;
using TouMegaChujoweExtension.Modules;
using UnityEngine;
using MiraAPI.Utilities;
using TownOfUs.Extensions;

namespace TouMegaChujoweExtension.Patches;

[HarmonyPatch]
public static class DecoyPatches
{
    [HarmonyPatch(typeof(ReportButton), nameof(ReportButton.DoClick))]
    [HarmonyPriority(Priority.First)]
    [HarmonyPrefix]
    public static bool ReportButtonPrefix(ReportButton __instance)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || localPlayer.Data.IsDead) return true;

        // Find the closest active decoy body
        var closestDecoy = DecoySystem.GetClosestDecoy(localPlayer.transform.position, out var distance);
        if (closestDecoy != null && distance < localPlayer.MaxReportDistance)
        {
            var decoyComp = closestDecoy.GetComponent<DecoyBodyComponent>();
            if (decoyComp != null)
            {
                if (decoyComp.SwapperPlayerId == localPlayer.PlayerId)
                {
                    return false; // Creator cannot report or trigger their own decoy!
                }

                DecoySystem.RpcSpringDecoy(
                    localPlayer,
                    decoyComp.SwapperPlayerId,
                    decoyComp.IsPoltergeist,
                    localPlayer.transform.position
                );
                return false; // Block standard report button action!
            }
        }

        return true;
    }

    [HarmonyPatch(typeof(DeadBody), nameof(DeadBody.OnClick))]
    [HarmonyPriority(Priority.First)]
    [HarmonyPrefix]
    public static bool DeadBodyOnClickPrefix(DeadBody __instance)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || localPlayer.Data.IsDead) return true;

        var decoyComp = __instance.GetComponent<DecoyBodyComponent>();
        if (decoyComp != null)
        {
            if (decoyComp.SwapperPlayerId == localPlayer.PlayerId)
            {
                return false; // Creator cannot report or trigger their own decoy!
            }

            var localPosition = localPlayer.GetTruePosition();
            var offset = new Vector2(-0.2f, -0.25f);
            var bodyPosition = __instance.TruePosition + offset;
            var distance = Vector2.Distance(bodyPosition, localPosition);

            if (distance < localPlayer.MaxReportDistance)
            {
                DecoySystem.RpcSpringDecoy(
                    localPlayer,
                    decoyComp.SwapperPlayerId,
                    decoyComp.IsPoltergeist,
                    localPlayer.transform.position
                );
                return false; // Block standard direct report!
            }
        }

        return true;
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPrefix]
    public static void MeetingHudStartPrefix()
    {
        DecoySystem.ClearDecoys();
    }

    /// <summary>
    /// Visually disables the report button when the closest reportable body
    /// is the player's own decoy (Body Swapper or Poltergeist).
    /// </summary>
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    public static void HudManagerUpdatePostfix(HudManager __instance)
    {
        if (__instance == null || __instance.ReportButton == null) return;
        
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || localPlayer.Data == null || localPlayer.Data.IsDead) return;

        // Poltergeist cannot report any bodies ever
        if (localPlayer.Data?.Role is TouMegaChujoweExtension.Roles.Classic.Neutral.PoltergeistRole)
        {
            __instance.ReportButton.SetDisabled();
            return;
        }

        // Body Swapper cannot report their own decoy
        if (localPlayer.Data?.Role is TouMegaChujoweExtension.Roles.Classic.Impostor.BodySwapperRole)
        {
            // Check if the closest body is the player's own decoy
            var closestDecoy = DecoySystem.GetClosestDecoy(localPlayer.transform.position, out var distance);
            if (closestDecoy != null && distance < localPlayer.MaxReportDistance)
            {
                var decoyComp = closestDecoy.GetComponent<DecoyBodyComponent>();
                if (decoyComp != null && decoyComp.SwapperPlayerId == localPlayer.PlayerId)
                {
                    // Also check there's no real dead body closer
                    var closestRealBody = localPlayer.GetNearestDeadBody(localPlayer.MaxReportDistance);
                    bool hasCloserRealBody = false;
                    if (closestRealBody != null)
                    {
                        var realComp = closestRealBody.GetComponent<DecoyBodyComponent>();
                        if (realComp == null)
                        {
                            float realDist = Vector2.Distance(closestRealBody.transform.position, localPlayer.transform.position);
                            hasCloserRealBody = realDist <= distance;
                        }
                    }

                    if (!hasCloserRealBody)
                    {
                        __instance.ReportButton.SetDisabled();
                    }
                }
            }
        }
    }
}
