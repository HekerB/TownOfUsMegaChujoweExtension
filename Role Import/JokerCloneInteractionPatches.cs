using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Joker;

public static class JokerCloneInteractionPatches
{
    public static bool IsKillingRole(PlayerControl player)
    {
        if (player == null || player.Data == null || player.Data.Role == null) return false;
        if (player.Data.Role.IsImpostor) return true;
        if (player.Is(RoleAlignment.NeutralKilling)) return true;
        if (player.Data.Role is ITownOfUsRole touRole &&
            touRole.RoleAlignment is RoleAlignment.CrewmateKilling or RoleAlignment.NeutralKilling)
            return true;
        return false;
    }

    public static Color GetKillColor(PlayerControl player)
    {
        if (player == null || player.Data?.Role == null) return Palette.ImpostorRed;
        if (player.Data.Role.IsImpostor) return Palette.ImpostorRed;
        if (player.Data.Role is ITownOfUsRole touRole) return touRole.RoleColor;
        return Palette.ImpostorRed;
    }

    public static float GetKillDistanceStatic()
    {
        var opts = GameOptionsManager.Instance?.currentNormalGameOptions;
        if (opts == null) return 1.0f;
        var killDistances = opts.GetFloatArray(FloatArrayOptionNames.KillDistances);
        var idx = Math.Clamp(opts.KillDistance, 0, killDistances.Length - 1);
        return killDistances[idx];
    }
}