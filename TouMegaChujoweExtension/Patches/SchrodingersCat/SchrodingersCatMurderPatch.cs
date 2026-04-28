using HarmonyLib;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Utilities;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Utilities;

namespace TouMegaChujoweExtension.Patches.SchrodingersCat;

/// <summary>
/// Intercepts kill attempts on SchrodingersCat.
/// First kill = adoption (blocked). Subsequent kills = go through.
/// </summary>
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
public static class SchrodingersCatMurderPatch
{
    [HarmonyPriority(Priority.First)]
    public static bool Prefix(PlayerControl __instance, PlayerControl target)
    {
        if (target == null || __instance == null)
            return true;

        if (target.Data?.Role is not SchrodingersCatRole catRole)
            return true;

        // If already adopted, check if the attacker is the adopter
        if (catRole.IsAdopted)
        {
            if (catRole.TeammateId == __instance.PlayerId)
            {
                // Cannot kill your own cat
                return false;
            }
            return true; // Someone else can kill the cat
        }

        // Check if NK can adopt
        if (__instance.Is(TownOfUs.Roles.RoleAlignment.NeutralKilling) &&
            !OptionGroupSingleton<SchrodingersCatOptions>.Instance.CanBeAdoptedByNeutralKillers)
            return true; // NK can't adopt, kill goes through

        // First kill attempt - adopt the cat
        if (!AmongUsClient.Instance.AmHost)
            return false;

        // Shield flash animation for both
        // Use ShieldUtils trigger for the killer to see the flash
        ShieldUtils.TriggerShieldFlash(__instance, ShieldType.SchrodingersCat);
        
        // Show shield on cat
        target.RpcGuardAndKill();

        // Set teammate via RPC
        SchrodingersCatRole.RpcSetTeammate(target, __instance.PlayerId);

        // Reset killer's cooldown
        __instance.SetKillCooldown();

        return false; // Block the kill
    }
}
