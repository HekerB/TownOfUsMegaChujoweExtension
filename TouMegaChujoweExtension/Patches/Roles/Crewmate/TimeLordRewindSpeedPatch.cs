using HarmonyLib;
using MiraAPI.GameOptions;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using TownOfUs.Modules.Localization;
using TownOfUs.Options.Roles.Crewmate;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Utilities;
using TownOfUs;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Crewmate;

[HarmonyPatch(typeof(TimeLordRole), nameof(TimeLordRole.RpcStartRewind))]
public static class TimeLordRewindSpeedPatch
{
    public static float GetAdjustedDuration()
    {
        var multiplier = OptionGroupSingleton<TimeLordExtensionOptions>.Instance.RewindSpeed;
        return 3.5f / Mathf.Max(0.1f, multiplier);
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var foundDuration = false;

        for (var i = 0; i < codes.Count; i++)
        {
            var code = codes[i];
            
            // Look for ldc.r4 3.5
            if (code.opcode == OpCodes.Ldc_R4 && code.operand is float f && Mathf.Approximately(f, 3.5f))
            {
                code.opcode = OpCodes.Call;
                code.operand = AccessTools.Method(typeof(TimeLordRewindSpeedPatch), nameof(GetAdjustedDuration));
                foundDuration = true;
            }
        }

        if (!foundDuration)
        {
            UnityEngine.Debug.LogWarning("[TimeLordRewindSpeedPatch] Could not find duration constant 3.5f in RpcStartRewind");
        }

        return codes;
    }
}














