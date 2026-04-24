using HarmonyLib;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Roles.Neutral;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.SerialKiller;

[HarmonyPatch]
public static class SerialKillerVentButtonPatch
{
    [HarmonyPatch(typeof(VentButton), nameof(VentButton.SetTarget))]
    [HarmonyPostfix]
    public static void SetTargetPostfix(VentButton __instance)
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data?.Role is not SerialKillerRole)
            return;

        var sprite = TouExtensionNeuAssets.SerialKillerVentButtonSprite.LoadAsset();
        if (sprite != null && __instance.graphic != null)
        {
            __instance.graphic.sprite = sprite;
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
[HarmonyPostfix]
public static void HudUpdatePostfix(HudManager __instance)
{
    var player = PlayerControl.LocalPlayer;
    if (player == null || player.Data?.Role is not SerialKillerRole)
        return;

    var ventButton = __instance.ImpostorVentButton;
    if (ventButton == null || ventButton.buttonLabelText == null)
        return;

    var isActive = ventButton.currentTarget != null || player.inVent;
    if (!isActive)
        return;

    var color = TouExtensionColors.SerialKiller;
    ventButton.buttonLabelText.color = Color.white;
    ventButton.buttonLabelText.outlineColor = new Color32(
        (byte)(color.r * 255),
        (byte)(color.g * 255),
        (byte)(color.b * 255),
        255
    );
    ventButton.buttonLabelText.fontMaterial.SetColor("_OutlineColor", color);
}
}
