using AmongUs.GameOptions;
using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Roles;
using System.Linq;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Roles;

[HarmonyPatch(typeof(TownOfUs.Utilities.Extensions), "ChangeRole")]
public static class VampireHunterFallbackPatch
{
    private static bool _isReentry = false;

    [HarmonyPostfix]
    public static void Postfix(PlayerControl player, ushort newRoleType)
    {
        if (_isReentry) return;
        if (!AmongUsClient.Instance || !AmongUsClient.Instance.AmHost) return;
        if (player == null) return;

        // Check if new role is VampireHunterRole
        var role = RoleManager.Instance.GetRole((RoleTypes)newRoleType);
        if (role is not VampireHunterRole) return;

        // Check if there are living vampires left
        var livingVampires = PlayerControl.AllPlayerControls.ToArray()
            .Count(x => x != null && !x.HasDied() && x.Data.Role is VampireRole);

        if (livingVampires == 0)
        {
            var become = OptionGroupSingleton<VampireHunterOptions>.Instance.BecomeOnVampireDeath;

            ushort newRoleId = become switch
            {
                VampireHunterBecomes.Sheriff => RoleId.Get<TownOfUs.Roles.Crewmate.SheriffRole>(),
                VampireHunterBecomes.Veteran => RoleId.Get<TownOfUs.Roles.Crewmate.VeteranRole>(),
                VampireHunterBecomes.Vigilante => RoleId.Get<TownOfUs.Roles.Crewmate.VigilanteRole>(),
                VampireHunterBecomes.Hunter => RoleId.Get<TownOfUs.Roles.Crewmate.HunterRole>(),
                VampireHunterBecomes.Officer => RoleId.Get<TownOfUs.Roles.Crewmate.OfficerRole>(),
                _ => (ushort)AmongUs.GameOptions.RoleTypes.Crewmate
            };

            _isReentry = true;
            try
            {
                player.RpcChangeRole(newRoleId, false);
            }
            finally
            {
                _isReentry = false;
            }
        }
    }
}














