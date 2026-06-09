/*using HarmonyLib;
using TouMegaChujoweExtension.Buttons.Crewmate;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Roles.Neutral;

namespace TouMegaChujoweExtension.Patches.Joker;

[HarmonyPatch(typeof(StakeButton), nameof(StakeButton.ClickHandler))]
public static class VampireHunterStakeClonePatch
{
    [HarmonyPrefix]
    public static bool Prefix(StakeButton __instance)
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data == null || player.Data.IsDead) return true;
        if (!__instance.CanClick()) return false;

        const float stakeDist = 1.5f;

        if (!JokerCloneSystem.TryGetClosestClone(player.GetTruePosition(), stakeDist, out var idx, out _))
            return true;

        if (idx < 0 || idx >= JokerCloneSystem.Clones.Count) return true;
        var clone = JokerCloneSystem.Clones[idx];
        if (clone == null || clone.IsPreview) return true;

        JokerRole.RpcJokerCloneKilled(player, clone.JokerId, idx);

        try { __instance.Timer = __instance.Cooldown; } catch { }

        return false;
    }
}*/