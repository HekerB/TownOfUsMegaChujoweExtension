using HarmonyLib;
using TouMegaChujoweExtension.Buttons.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Neutral;

/// <summary>
/// Catches external SetKillTimer calls (e.g. after meeting) and syncs both buttons.
/// </summary>
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetKillTimer))]
public static class ShroudKillTimerSyncPatch
{
    private static float _lastKillTimer;

    [HarmonyPostfix]
    public static void Postfix(PlayerControl __instance, float time)
    {
        if (__instance != PlayerControl.LocalPlayer) return;
        if (!__instance.IsRole<ShroudRole>()) return;

        if (time > 5f && time > _lastKillTimer + 1f)
        {
            ShroudKillButton.SetOwnCooldown();
            ShroudAbilityButton.SetOwnCooldown();
        }

        _lastKillTimer = time;
    }
}
