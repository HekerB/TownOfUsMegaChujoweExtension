using HarmonyLib;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Assets;
using TownOfUs.Utilities;
using UnityEngine;
using TouMegaChujoweExtension.Assets;
using System.Linq;

namespace TouMegaChujoweExtension.Patches.Neutral;

[HarmonyPatch]
public static class VultureVentPatch
{

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    public static void UpdatePostfix(HudManager __instance)
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data?.Role == null) return;

        var isVulture = player.Data.Role is VultureRole;
        var ventButton = __instance.ImpostorVentButton;

        if (ventButton == null) return;

        if (isVulture)
        {
            // 1. Change Sprite
            if (ventButton.graphic != null)
            {
                var ventSprite = TouNeutAssets.WerewolfVentSprite.LoadAsset();
                if (ventButton.graphic.sprite != ventSprite)
                {
                    ventButton.graphic.sprite = ventSprite;
                }
            }

            // 2. Fix Highlighting (Alpha)
            var hasTarget = ventButton.currentTarget != null || player.inVent;
            var alpha = hasTarget ? 1f : 0.3f;

            if (ventButton.graphic != null)
            {
                var color = ventButton.graphic.color;
                color.a = alpha;
                ventButton.graphic.color = color;
            }

            // 3. Change Text Color and Outline
            if (ventButton.buttonLabelText != null)
            {
                ventButton.buttonLabelText.color = new Color(1f, 1f, 1f, alpha);
                ventButton.buttonLabelText.SetOutlineColor(TouExtensionColors.Vulture.SetAlpha(alpha));
            }

        }
    }
}
