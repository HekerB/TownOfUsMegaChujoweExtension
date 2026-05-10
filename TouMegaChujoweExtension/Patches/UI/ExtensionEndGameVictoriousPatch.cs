using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Patches;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.UI;

[HarmonyPatch(typeof(EndGamePatches), nameof(EndGamePatches.HandlePlayerNames))]
public static class ExtensionEndGameVictoriousPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        var allPoolablePlayers = UnityEngine.Object.FindObjectsOfType<PoolablePlayer>();
        var winners = EndGameResult.CachedWinners.ToArray();
        string victoriousText = TouLocale.Get("DiedToWinning", "Victorious");
        
        var extensionRoles = new List<string>
        {
            TouLocale.Get("ExtensionRolePirate", "Pirate"),
            TouLocale.Get("ExtensionRoleBountyHunter", "Bounty Hunter"),
            TouLocale.Get("ExtensionRolePelican", "Pelican"),
            TouLocale.Get("ExtensionRoleVulture", "Vulture"),
            TouLocale.Get("ExtensionRoleJoker", "Joker"),
            TouLocale.Get("ExtensionRolePope", "Pope"),
            TouLocale.Get("ExtensionRoleSerialKiller", "Serial Killer"),
            TouLocale.Get("ExtensionRoleShroud", "Shroud"),
            TouLocale.Get("ExtensionRoleLawyer", "Lawyer"),
            TouLocale.Get("ExtensionRoleVampire", "Vampire"),
            TouLocale.Get("TouRoleChef", "Chef")
        };

        // 1. Update PoolablePlayer displays (above heads)
        if (allPoolablePlayers != null)
        {
            foreach (var player in allPoolablePlayers)
            {
                var nameTxt = player.cosmetics?.nameText;
                if (nameTxt == null || string.IsNullOrEmpty(nameTxt.text)) continue;

                bool isExtensionRole = extensionRoles.Any(role => nameTxt.text.Contains(role));
                if (!isExtensionRole) continue;
                if (nameTxt.text.Contains(victoriousText)) continue;

                string currentText = nameTxt.text;
                var winner = winners.FirstOrDefault(w => currentText.Contains(w.PlayerName));
                
                if (winner != null)
                {
                    var role = RoleManager.Instance.GetRole(winner.RoleWhenAlive);
                    string roleName = role is ITownOfUsRole touRole ? touRole.RoleName : (role?.Role.ToString() ?? "");
                    string roleColor = role?.TeamColor.ToTextColor() ?? "";
                    
                    // Rebuild the name display: Victorious (Role Color) | Name (Gold) | Role (Role Color)
                    nameTxt.text = $"<size=70%>{roleColor}{victoriousText}</color></size>\n<size=85%><color=#EFBF04>{winner.PlayerName}</color></size>\n<size=65%>{roleColor}{roleName}</color></size>";
                }
            }
        }

        // 2. Update EndGameData records (for the Summary list)
        if (EndGamePatches.EndGameData.PlayerRecords != null)
        {
            foreach (var record in EndGamePatches.EndGameData.PlayerRecords)
            {
                if (!record.Winner) continue;
                
                // If it's one of our roles, add Victorious to the RoleStringShort
                bool matchesRole = extensionRoles.Any(r => record.RoleStringShort != null && record.RoleStringShort.Contains(r));
                if (matchesRole && record.RoleStringShort != null && !record.RoleStringShort.Contains(victoriousText))
                {
                    var role = RoleManager.Instance.GetRole(record.LastRole);
                    string roleColor = role?.TeamColor.ToTextColor() ?? "";
                    
                    record.RoleStringShort = $"{roleColor}{victoriousText}</color> | {record.RoleStringShort}";
                    record.RoleString = $"{roleColor}{victoriousText}</color> | {record.RoleString}";
                }
            }
        }
    }
}













