using TownOfUs.Utilities;
using HarmonyLib;
using TownOfUs.Extensions;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles;

[HarmonyPatch]
public static class NeutralVentButtonPatch
{
    private static bool? _lastHasTarget;

    [HarmonyPatch(typeof(VentButton), nameof(VentButton.SetTarget))]
    [HarmonyPostfix]
    public static void SetTargetPostfix(VentButton __instance)
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data == null) return;

        Sprite? customSprite = null;
        if (player.IsRole<SerialKillerRole>())
        {
            customSprite = TouExtensionNeuAssets.SerialKillerVentButtonSprite.LoadAsset();
        }
        else if (player.IsRole<ShroudRole>())
        {
            customSprite = TouExtensionNeuAssets.ShroudVentSprite.LoadAsset();
        }
        else if (player.IsRole<VultureRole>())
        {
            customSprite = TownOfUs.Assets.TouNeutAssets.WerewolfVentSprite.LoadAsset();
        }

        if (customSprite != null && __instance.graphic != null && __instance.graphic.sprite != customSprite)
        {
            __instance.graphic.sprite = customSprite;
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data?.Role == null) return;

        var ventButton = __instance.ImpostorVentButton;
        if (ventButton == null || ventButton.buttonLabelText == null) return;

        Color? roleColor = null;
        var role = player.Data.Role;

        if (role is SerialKillerRole) roleColor = TouExtensionColors.SerialKiller;
        else if (role is ShroudRole) roleColor = TouExtensionColors.Shroud;
        else if (role is VultureRole) roleColor = TouExtensionColors.Vulture;
        else if (role is PelicanRole) roleColor = TouExtensionColors.Pelican;
        else if (role is JackalRole) roleColor = TouExtensionColors.Jackal;

        if (roleColor.HasValue)
        {
            var hasTarget = ventButton.currentTarget != null || player.inVent;
            
            if (!_lastHasTarget.HasValue || _lastHasTarget.Value != hasTarget)
            {
                _lastHasTarget = hasTarget;
                var alpha = hasTarget ? 1f : 0.3f;
                var finalColor = roleColor.Value;
                finalColor.a = alpha;

                if (ventButton.graphic != null)
                {
                    var c = ventButton.graphic.color;
                    c.a = alpha;
                    ventButton.graphic.color = c;
                }

                // Always force the color for these roles to prevent flickering with default Impostor red
                ventButton.buttonLabelText.color = new Color(1f, 1f, 1f, alpha);
                ventButton.buttonLabelText.outlineColor = finalColor;
                
                ventButton.buttonLabelText.fontMaterial?.SetColor("_OutlineColor", finalColor);
            }
        }
    }
}















