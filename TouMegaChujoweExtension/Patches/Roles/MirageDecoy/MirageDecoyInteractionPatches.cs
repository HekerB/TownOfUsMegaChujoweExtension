using AmongUs.GameOptions;
using HarmonyLib;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Roles.MirageDecoy;


[HarmonyPatch]
public static class MirageDecoyInteractionPatches
{
    private static bool TryTriggerFromLocalPlayer(float maxDistance)
    {
        return MirageDecoySystem.TryTriggerFromLocalPlayer(maxDistance);
    }
    private static float GetKillDistance()
    {
        var opts = GameOptionsManager.Instance?.currentNormalGameOptions;
        if (opts == null)
        {
            return 1.0f;
        }

        var killDistances = opts.GetFloatArray(FloatArrayOptionNames.KillDistances);
        var idx = Math.Clamp(opts.KillDistance, 0, killDistances.Length - 1);
        return killDistances[idx];
    }

    [HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    public static bool KillButtonDoClickPrefix()
    {
        if (!TryTriggerFromLocalPlayer(GetKillDistance()))
        {
            return true;
        }

        try
        {
            var local = PlayerControl.LocalPlayer;
            if (local != null)
            {
                local.SetKillTimer(local.GetKillCooldown());
            }
        }
        catch
        {
            // ignore
        }

        return false;
    }
}















