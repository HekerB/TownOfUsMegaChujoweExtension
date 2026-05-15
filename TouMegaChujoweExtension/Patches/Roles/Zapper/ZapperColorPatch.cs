using HarmonyLib;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Utilities;
using UnityEngine;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Impostor;

namespace TouMegaChujoweExtension.Patches.Zapper;

[HarmonyPatch]
public static class ZapperColorPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static void UpdateZapperTargetColor(HudManager __instance)
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.Data == null || player.Data.IsDead) continue;

            if (player.HasModifier<ZapperZapModifier>())
            {
                if (player.cosmetics != null && player.cosmetics.nameText != null)
                {
                    player.cosmetics.nameText.color = new Color(0.1f, 0.1f, 0.7f, 1f); // Dark Blue
                }
                
                if (MeetingHud.Instance != null)
                {
                    foreach (var pva in MeetingHud.Instance.playerStates)
                    {
                        if (pva == null || pva.TargetPlayerId != player.PlayerId) continue;
                        if (pva.NameText == null) continue;
                        pva.NameText.color = new Color(0.1f, 0.1f, 0.7f, 1f);
                    }
                }
            }
        }
    }
}
