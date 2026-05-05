using HarmonyLib;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Roles;
using TownOfUs.Patches;
using System.Linq;
using UnityEngine;
using TMPro;

namespace TouMegaChujoweExtension.Patches.Pirate;

[HarmonyPatch(typeof(EndGamePatches), nameof(EndGamePatches.HandlePlayerNames))]
public static class PirateEndGamePatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        // End game players are typically PoolablePlayer instances in the end game scene
        var allPoolablePlayers = UnityEngine.Object.FindObjectsOfType<PoolablePlayer>();
        if (allPoolablePlayers == null || allPoolablePlayers.Length == 0) return;

        // Get the "Victorious" text from localization
        string victoriousText = TouLocale.Get("DiedToWinning", "Victorious");
        string pirateRoleName = TouLocale.Get("ExtensionRolePirate", "Pirate");
        
        // Apply the fix to all winning Pirates
        foreach (var player in allPoolablePlayers)
        {
            var nameTxt = player.cosmetics?.nameText;
            if (nameTxt == null || string.IsNullOrEmpty(nameTxt.text)) continue;

            // Check if this player display is a Pirate
            if (nameTxt.text.Contains(pirateRoleName))
            {
                // Rebuild the text to include "Victorious" at the top.
                // The base mod sets it as: "\n<size=85%>{PlayerName}</size>\n<size=65%>{RoleName}</size>"
                // We want to ensure it says "Victorious" at the very top.
                
                // We trim the leading newline and prepend our text.
                string currentText = nameTxt.text.TrimStart('\n');
                
                // If it already contains the victorious text, skip
                if (currentText.Contains(victoriousText)) continue;

                nameTxt.text = $"<size=70%>{victoriousText}</size>\n{currentText}";
            }
        }
    }
}
