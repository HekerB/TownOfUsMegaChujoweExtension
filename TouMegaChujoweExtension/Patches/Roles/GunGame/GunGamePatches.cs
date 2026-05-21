using System.Collections.Generic;
using HarmonyLib;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Extensions;
using TownOfUs.Networking;
using MiraAPI.Networking;
using TownOfUs.Utilities;
using MiraAPI.Roles;
using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Impostor;

namespace TouMegaChujoweExtension.Patches.Roles.GunGame;

[HarmonyPatch]
public static class GunGamePatches
{
    private static float _lastMutationTime;
    public static HashSet<byte> GunGamePlayers { get; } = new();

    /// <summary>
    /// Tracks the ushort role ID that the Gun Game player was changed into this round.
    /// Used to know which role they ARE after mutation so we can revert at meeting.
    /// </summary>
    public static Dictionary<byte, ushort> CurrentMutatedRoleId { get; } = new();

    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    [HarmonyPrefix]
    public static void RoleManagerSelectRolesPrefix()
    {
        GunGameRole.CurrentChainIndex.Clear();
        GunGamePlayers.Clear();
        CurrentMutatedRoleId.Clear();
    }



    /// <summary>
    /// On meeting start, revert all GunGame players back to GunGameRole.
    /// This preserves their chain index so the next kill continues from where they left off.
    /// </summary>
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPostfix]
    public static void MeetingHudStartPostfix()
    {
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc == null || pc.Data == null || pc.Data.IsDead) continue;
            if (!pc.HasModifier<TouMegaChujoweExtension.Modifiers.Impostor.GunGameModifier>()) continue;

            // Only revert if they are NOT already GunGameRole
            if (pc.Data.Role is GunGameRole) continue;

            // Revert to GunGameRole
            var gunGameRoleId = RoleId.Get<GunGameRole>();
            pc.ChangeRole(gunGameRoleId, recordRole: false);
        }
    }
}

[HarmonyPatch(typeof(TownOfUs.Utilities.Extensions), nameof(TownOfUs.Utilities.Extensions.ChangeRole), typeof(PlayerControl), typeof(ushort), typeof(bool))]
public static class ChangeRolePatch
{
    [HarmonyPostfix]
    public static void Postfix(PlayerControl player, ushort newRoleType)
    {
        if (player == null || !player.AmOwner || !HudManager.InstanceExists) return;

        var roleBehaviour = player.Data?.Role;
        if (roleBehaviour == null) return;

        foreach (var button in MiraAPI.Hud.CustomButtonManager.Buttons)
        {
            if (button == null) continue;
            if (button.Enabled(roleBehaviour))
            {
                if (button.Button == null)
                {
                    button.CreateButton(HudManager.Instance.transform);
                }
                button.SetActive(true, roleBehaviour);
            }
            else
            {
                button.SetActive(false, roleBehaviour);
            }
        }
    }
}
