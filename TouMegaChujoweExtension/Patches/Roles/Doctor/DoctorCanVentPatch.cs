using HarmonyLib;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Crewmate;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Doctor;

[HarmonyPatch]
public static class DoctorCanVentPatch
{
    [HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
    [HarmonyPostfix]
    public static void CanUseVentPostfix(
        Vent __instance,
        NetworkedPlayerInfo pc,
        ref bool canUse,
        ref bool couldUse,
        ref float __result)
    {
        if (__instance == null || pc == null) return;
        if (canUse) return; // already can use

        var player = pc.Object;
        if (player == null || pc.IsDead || pc.Disconnected) return;

        if (!player.HasModifier<DoctorCanVentModifier>()) return;

        if (player.inVent)
        {
            if (Vent.currentVent != null && __instance.Id == Vent.currentVent.Id)
            {
                canUse = true;
                couldUse = true;
                __result = 0f;
            }
            return;
        }

        Vector2 truePosition = player.GetTruePosition();
        Vector2 ventPosition = __instance.transform.position;
        float distance = Vector2.Distance(truePosition, ventPosition);

        bool inRange = distance <= __instance.UsableDistance;
        bool clearPath = !PhysicsHelpers.AnythingBetween(truePosition, ventPosition, Constants.ShipOnlyMask, false);

        couldUse = inRange && clearPath;
        canUse = couldUse;
        __result = distance;
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        if (__instance == null) return;

        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data == null || player.Data.IsDead) return;

        if (player.Data.Role != null && player.Data.Role.IsImpostor) return;

        var mod = player.GetModifier<DoctorCanVentModifier>();
        var ventButton = __instance.ImpostorVentButton;

        if (mod == null) return;

        if (ventButton == null || ventButton.gameObject == null) return;

        bool inMeeting = MeetingHud.Instance != null || ExileController.Instance != null;
        if (inMeeting)
        {
            if (ventButton.gameObject.activeSelf) ventButton.gameObject.SetActive(false);
            return;
        }

        if (!ventButton.gameObject.activeSelf) ventButton.gameObject.SetActive(true);

        if (ventButton.graphic != null)
        {
            if (!ventButton.graphic.gameObject.activeSelf) ventButton.graphic.gameObject.SetActive(true);
            ventButton.graphic.enabled = true;
            // ventButton.graphic.sprite = default vent sprite is usually fine
        }
        
        ventButton.SetCoolDown(0f, 1f);
    }
}
