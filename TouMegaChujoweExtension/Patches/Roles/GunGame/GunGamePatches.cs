using HarmonyLib;
using MiraAPI.Hud;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Roles.GunGame;

[HarmonyPatch]
public static class GunGamePatches
{
    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    [HarmonyPrefix]
    public static void RoleManagerSelectRolesPrefix()
    {
        GunGameRole.ResetState();
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPostfix]
    public static void MeetingHudStartPostfix()
    {
        if (OptionGroupSingleton<GunGameOptions>.Instance.KeepRoleAfterMeeting)
        {
            return;
        }

        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc == null || pc.Data == null || pc.Data.IsDead)
            {
                continue;
            }

            if (!pc.HasModifier<GunGameModifier>() || pc.Data.Role is GunGameRole)
            {
                continue;
            }

            pc.ChangeRole(RoleId.Get<GunGameRole>(), recordRole: false);
        }
    }
}

[HarmonyPatch(typeof(TownOfUs.Utilities.Extensions), nameof(TownOfUs.Utilities.Extensions.ChangeRole), typeof(PlayerControl), typeof(ushort), typeof(bool))]
public static class GunGameChangeRoleButtonPatch
{
    [HarmonyPostfix]
    public static void Postfix(PlayerControl player)
    {
        if (player == null || !player.AmOwner || !HudManager.InstanceExists)
        {
            return;
        }

        var role = player.Data?.Role;
        if (role == null)
        {
            return;
        }

        foreach (var button in CustomButtonManager.Buttons)
        {
            if (button == null)
            {
                continue;
            }

            if (button.Enabled(role))
            {
                if (button.Button == null)
                {
                    button.CreateButton(HudManager.Instance.transform);
                }

                button.SetActive(true, role);
            }
            else
            {
                button.SetActive(false, role);
            }
        }
    }
}
