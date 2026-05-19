using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Roles;
using TownOfUs.Roles.Neutral;
using TouMegaChujoweExtension.Options;
using TownOfUs.Patches;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Jackal;

public static class JackalVampireExclusionState
{
    public static bool VampireBlocked { get; set; }
    public static bool JackalBlocked { get; set; }
    public static bool Decided { get; set; }

    public static void Decide()
    {
        if (Decided) return;
        Decided = true;

        // Roll 50/50 to block either Jackal or Vampire
        if (UnityEngine.Random.value < 0.5f)
        {
            VampireBlocked = true;
            JackalBlocked = false;
        }
        else
        {
            VampireBlocked = false;
            JackalBlocked = true;
        }
        
        UnityEngine.Debug.Log($"[TOUMCE] Jackal/Vampire exclusion decided: VampireBlocked={VampireBlocked}, JackalBlocked={JackalBlocked}");
    }

    public static void Reset()
    {
        Decided = false;
        VampireBlocked = false;
        JackalBlocked = false;
    }
}

[HarmonyPatch(typeof(VampireRole), nameof(VampireRole.Configuration), MethodType.Getter)]
public static class JackalVampirePatch
{
    public static void Postfix(ref CustomRoleConfiguration __result)
    {
        var generalOptions = OptionGroupSingleton<ExtensionGameMechanicOptions>.Instance;

        if (generalOptions != null && generalOptions.PreventVampiresWithJackal)
        {
            if (JackalVampireExclusionState.Decided && JackalVampireExclusionState.VampireBlocked)
            {
                __result.MaxRoleCount = 0;
            }
        }
    }
}

[HarmonyPatch(typeof(TouMegaChujoweExtension.Roles.Classic.Neutral.JackalRole), nameof(TouMegaChujoweExtension.Roles.Classic.Neutral.JackalRole.Configuration), MethodType.Getter)]
public static class JackalRoleConfigPatch
{
    public static void Postfix(ref CustomRoleConfiguration __result)
    {
        var generalOptions = OptionGroupSingleton<ExtensionGameMechanicOptions>.Instance;

        if (generalOptions != null && generalOptions.PreventVampiresWithJackal)
        {
            if (JackalVampireExclusionState.Decided && JackalVampireExclusionState.JackalBlocked)
            {
                __result.MaxRoleCount = 0;
            }
        }
    }
}

[HarmonyPatch(typeof(TouRoleManagerPatches), "AssignRoles")]
[HarmonyPatch(typeof(TouRoleManagerPatches), "AssignRolesFromRoleList")]
public static class JackalVampireExclusionPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            JackalVampireExclusionState.Decide();
        }
    }
}
