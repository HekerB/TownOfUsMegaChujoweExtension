using AmongUs.GameOptions;
using HarmonyLib;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Roles.Loner;

[HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
public static class LonerRoleSelectionPatch
{
    [HarmonyPrefix]
    public static void SelectRolesPrefix()
    {
        LonerRole.ResetState();
    }

    [HarmonyPostfix]
    public static void SelectRolesPostfix()
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            return;
        }

        if (DraftSystem.LonerReducedImpostorSlot)
        {
            return;
        }

        var loner = PlayerControl.AllPlayerControls
            .ToArray()
            .FirstOrDefault(player => player?.Data?.Role is LonerRole && !player.Data.Disconnected);
        if (loner == null)
        {
            return;
        }

        var impostors = PlayerControl.AllPlayerControls
            .ToArray()
            .Where(player => player != null &&
                             player != loner &&
                             player.Data != null &&
                             !player.Data.Disconnected &&
                             player.IsImpostor())
            .ToList();

        if (impostors.Count == 0)
        {
            return;
        }

        var demoted = impostors[UnityEngine.Random.Range(0, impostors.Count)];
        demoted.RpcSetRole(RoleTypes.Crewmate);
    }
}
