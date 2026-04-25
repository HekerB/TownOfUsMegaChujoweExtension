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
                var wolfVent = TouNeutAssets.WerewolfVentSprite.LoadAsset();
                if (ventButton.graphic.sprite != wolfVent)
                {
                    ventButton.graphic.sprite = wolfVent;
                }
            }

            // 2. Change Text Color and Outline
            if (ventButton.buttonLabelText != null)
            {
                ventButton.buttonLabelText.color = Color.white;
                ventButton.buttonLabelText.SetOutlineColor(TouExtensionColors.Vulture);
            }

        }
    }
}
