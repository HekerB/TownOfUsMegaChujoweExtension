using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TouMegaChujoweExtension.Networking;
using TownOfUs.Roles.Neutral;
using TownOfUs.Extensions;

namespace TouMegaChujoweExtension.Patches.SchrodingersCat;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
public static class SchrodingersCatDeathPatches
{
    public static void Postfix(PlayerControl __instance, DeathReason reason)
    {
        if (__instance == null || !AmongUsClient.Instance.AmHost) return;

        // Check if the dying player is the owner of any adopted Cat
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.Data == null || player.Data.IsDead)
                continue;

            if (player.Data.Role is SchrodingersCatRole catRole && catRole.IsAdopted && catRole.TeammateId == __instance.PlayerId)
            {
                var options = OptionGroupSingleton<SchrodingersCatOptions>.Instance;
                if (options.ChangeRoleOnOwnerDeath)
                {
                    ushort becomeRoleId = options.OwnerDiedBecomes switch
                    {
                        CatOwnerDiedBecomesOption.Amnesiac => RoleId.Get<AmnesiacRole>(),
                        CatOwnerDiedBecomesOption.Survivor => RoleId.Get<SurvivorRole>(),
                        CatOwnerDiedBecomesOption.Jester => RoleId.Get<JesterRole>(),
                        _ => RoleId.Get<AmnesiacRole>()
                    };

                    // Use RpcSetRole which exists in MiraAPI
                    player.RpcSetRole((AmongUs.GameOptions.RoleTypes)becomeRoleId);
                }
            }
        }
    }
}
