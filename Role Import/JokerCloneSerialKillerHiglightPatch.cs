using HarmonyLib;
using TouMegaChujoweExtension.Buttons.Neutral;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Joker;

[HarmonyPatch(typeof(SerialKillerKillButton), nameof(SerialKillerKillButton.FixedUpdateHandler))]
public static class JokerCloneSerialKillerHighlightPatch
{
    [HarmonyPostfix]
    public static void Postfix(SerialKillerKillButton __instance)
    {
        if (__instance?.Button == null) return;

        var local = PlayerControl.LocalPlayer;
        if (local == null || local.HasDied()) return;
        if (MeetingHud.Instance) return;
        if (!local.CanMove) return;

        var dist = JokerCloneInteractionPatches.GetKillDistanceStatic();
        if (!JokerCloneSystem.TryGetClosestClone(local.GetTruePosition(), dist, out var idx, out _)) return;
        if (idx < 0 || idx >= JokerCloneSystem.Clones.Count) return;
        if (JokerCloneSystem.Clones[idx].IsPreview) return;

        try { __instance.Button.SetEnabled(); } catch { }
    }
}