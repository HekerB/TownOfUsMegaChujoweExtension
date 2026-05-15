using HarmonyLib;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.SchrodingersCat;

/// <summary>
/// Most cat protection logic is now in ShieldEvents.
/// These patches are minimal fallbacks.
/// </summary>
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
public static class SchrodingersCatDeathPreventionPatch
{
    [HarmonyPriority(Priority.First)]
    public static bool Prefix(PlayerControl __instance, PlayerControl target)
    {
        if (target == null || target.Data == null) return true;
        if (target.Data.Role is not SchrodingersCatRole catRole) return true;

        // Adoption and general protection is handled by BeforeMurderEvent in ShieldEvents.
        // We only block here if for some reason the event wasn't cancelled.
        
        if (!catRole.IsAdopted) return false; // Block unadopted (Adoption should happen in ShieldEvents)
        
        if (catRole.TeammateId == __instance.PlayerId) return false; // Block owner
        
        return true; // Others can kill
    }
}
