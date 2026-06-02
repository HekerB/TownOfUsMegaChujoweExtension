using System;
using MiraAPI.GameOptions;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Options;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Modules.Localization;
using TownOfUs.Roles.Neutral;
using UnityEngine;
using ExtensionSoulCollectorRole = TouMegaChujoweExtension.Roles.Classic.Neutral.SoulCollectorRole;

namespace TouMegaChujoweExtension.Modules;

public static class ApocalypseUtils
{
    public static bool WinsTogetherEnabled =>
        OptionGroupSingleton<ExtensionGameMechanicOptions>.Instance.ApocalypseWinsTogether;

    public static bool RolesKnowEachOther =>
        OptionGroupSingleton<ExtensionGameMechanicOptions>.Instance.ApocalypseRolesKnowEachOther;

    public static bool MeetingChatEnabled =>
        OptionGroupSingleton<ExtensionGameMechanicOptions>.Instance.ApocalypseMeetingChat;

    public static bool IsApocalypsePlayer(PlayerControl? player)
    {
        return player?.Data?.Role != null && IsApocalypseRole(player.Data.Role);
    }

    public static bool IsApocalypseRole(RoleBehaviour? role)
    {
        if (role == null)
        {
            return false;
        }

        if (role is PlaguebearerRole or PestilenceRole or BakerRole or FamineRole or ExtensionSoulCollectorRole or DeathRole or BerserkerRole)
        {
            return true;
        }

        return string.Equals(role.GetType().Name, "WarRole", StringComparison.Ordinal);
    }

    public static bool AreAllied(PlayerControl? playerA, PlayerControl? playerB)
    {
        return WinsTogetherEnabled &&
               playerA != null &&
               playerB != null &&
               playerA.PlayerId != playerB.PlayerId &&
               IsApocalypsePlayer(playerA) &&
               IsApocalypsePlayer(playerB);
    }

    public static string GetDisplayRoleName(PlayerControl player)
    {
        var role = player.Data?.Role;
        if (role == null)
        {
            return string.Empty;
        }

        return role switch
        {
            BakerRole => TouLocale.Get("ExtensionRoleBaker", "Baker"),
            FamineRole => TouLocale.Get("ExtensionRoleFamine", "Famine"),
            ExtensionSoulCollectorRole => TouLocale.Get("ExtensionRoleSoulCollector", "Soul Collector"),
            DeathRole => TouLocale.Get("ExtensionRoleDeath", "Death"),
            BerserkerRole berserker => berserker.IsWar
                ? TouLocale.Get("ExtensionRoleWar", "War")
                : TouLocale.Get("ExtensionRoleBerserker", "Berserker"),
            _ => role is ICustomRole customRole
                ? customRole.RoleName
                : role.name
        };
    }

    public static Color GetRoleColor(PlayerControl player)
    {
        var role = player.Data?.Role;
        if (role == null)
        {
            return Color.white;
        }

        return role switch
        {
            BakerRole => TouExtensionColors.Baker,
            FamineRole => TouExtensionColors.Famine,
            ExtensionSoulCollectorRole => TouExtensionColors.SoulCollector,
            DeathRole => TouExtensionColors.Death,
            BerserkerRole berserker => berserker.IsWar ? TouExtensionColors.War : TouExtensionColors.Berserker,
            _ => role.TeamColor
        };
    }
}
