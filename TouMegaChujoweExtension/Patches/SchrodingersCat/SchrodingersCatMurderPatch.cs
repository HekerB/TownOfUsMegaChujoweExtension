using HarmonyLib;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Utilities;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Utilities;
using TownOfUs.Extensions;
using MiraAPI.Networking;

namespace TouMegaChujoweExtension.Patches.SchrodingersCat;

/// <summary>
/// Intercepts kill attempts on SchrodingersCat.
/// First kill = adoption (blocked). Subsequent kills = go through only for owner.
/// </summary>
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
public static class SchrodingersCatMurderPatch
{
    [HarmonyPriority(Priority.First)]
    public static bool Prefix(PlayerControl __instance, PlayerControl target)
    {
        if (target == null || __instance == null || target.Data == null)
            return true;

        if (target.Data.Role is not SchrodingersCatRole catRole)
            return true;

        var logPrefix = $"[CatMurderPatch] killer={__instance.Data.PlayerName} target={target.Data.PlayerName}";
        
        if (catRole.IsAdopted)
        {
            if (catRole.TeammateId == __instance.PlayerId)
            {
                // Owner CANNOT kill his cat
                if (__instance.AmOwner)
                {
                    ShieldUtils.TriggerShieldFlash(__instance, ShieldType.SchrodingersCat);
                    __instance.SetKillTimer(__instance.GetKillCooldown());
                }
                return false;
            }
            
            // Everyone else CAN kill the cat after adoption
            return true;
        }

        // UNADOPTED CASE -> ADOPTION TRIGGER
        if (__instance.AmOwner)
        {
            var isNk = __instance.Is(TownOfUs.Roles.RoleAlignment.NeutralKilling);
            var canAdoptNk = OptionGroupSingleton<SchrodingersCatOptions>.Instance.CanBeAdoptedByNeutralKillers;
            
            if (isNk && !canAdoptNk)
            {
                ShieldUtils.TriggerShieldFlash(__instance, ShieldType.SchrodingersCat);
                __instance.SetKillTimer(__instance.GetKillCooldown());
                return false;
            }

            ShieldUtils.TriggerShieldFlash(__instance, ShieldType.SchrodingersCat);
            SchrodingersCatRole.RpcSetTeammate(target, __instance.PlayerId);
            __instance.SetKillTimer(__instance.GetKillCooldown());
        }

        return false;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckMurder))]
public static class SchrodingersCatCmdMurderPatch
{
    [HarmonyPriority(Priority.First)]
    public static bool Prefix(PlayerControl __instance, PlayerControl target)
    {
        return SchrodingersCatMurderPatch.Prefix(__instance, target);
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
public static class SchrodingersCatDeathPreventionPatch
{
    [HarmonyPriority(Priority.First)]
    public static bool Prefix(PlayerControl __instance, PlayerControl target)
    {
        if (target == null || target.Data == null) return true;
        if (target.Data.Role is not SchrodingersCatRole catRole) return true;

        if (catRole.IsAdopted)
        {
            if (catRole.TeammateId == __instance.PlayerId)
            {
                // Owner cannot kill
                if (__instance.AmOwner) ShieldUtils.TriggerShieldFlash(__instance, ShieldType.SchrodingersCat);
                return false;
            }
            // Others can kill
            return true;
        }

        if (__instance.AmOwner)
        {
            ShieldUtils.TriggerShieldFlash(__instance, ShieldType.SchrodingersCat);
            
            var isNk = __instance.Is(TownOfUs.Roles.RoleAlignment.NeutralKilling);
            var canAdoptNk = OptionGroupSingleton<SchrodingersCatOptions>.Instance.CanBeAdoptedByNeutralKillers;

            if (!isNk || canAdoptNk)
            {
                SchrodingersCatRole.RpcSetTeammate(target, __instance.PlayerId);
            }
        }
        
        return false;
    }
}

[HarmonyPatch(typeof(PlayerControl))]
public static class SchrodingersCatSpecialMurderPatch
{
    [HarmonyPatch("RpcMurderPlayer")]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool RpcMurderPlayerPrefix(PlayerControl __instance, PlayerControl target)
    {
        return SchrodingersCatDeathPreventionPatch.Prefix(__instance, target);
    }

    [HarmonyPatch("RpcSpecialMurder")]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool RpcSpecialMurderPrefix(PlayerControl __instance, PlayerControl target)
    {
        return SchrodingersCatDeathPreventionPatch.Prefix(__instance, target);
    }
    
    // Some custom roles might use this
    [HarmonyPatch("CmdReportDeadBody")]
    [HarmonyPrefix]
    public static bool CmdReportDeadBodyPrefix(PlayerControl __instance, PlayerControl target)
    {
        // Prevent reporting cat as dead if it's not dead? 
        // Not needed for now.
        return true;
    }
}

