using HarmonyLib;
using TouMegaChujoweExtension.Buttons.Neutral;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Buttons;
using TownOfUs.Utilities;
using TownOfUs.Buttons.Impostor;

namespace TouMegaChujoweExtension.Patches.Joker;

[HarmonyPatch(typeof(TownOfUsButton), nameof(TownOfUsButton.FixedUpdateHandler))]
public static class JokerCloneButtonHighlightPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(TownOfUsButton __instance)
    {
        if (__instance is JokerPlaceCloneButton) return;
		if (__instance is TownOfUsTargetButton<PlayerControl>) return;
		if (__instance is WarlockKillButton) return;
		if (__instance is not IKillButton) return;
        if (__instance.Button == null) return;

        var local = PlayerControl.LocalPlayer;
        if (local == null || local.HasDied()) return;
        if (MeetingHud.Instance) return;
        if (HudManager.Instance?.Chat != null && HudManager.Instance.Chat.IsOpenOrOpening) return;
        if (!local.CanMove) return;

        var dist = JokerCloneInteractionPatches.GetKillDistanceStatic();
        if (!JokerCloneSystem.TryGetClosestClone(local.GetTruePosition(), dist, out var idx, out _)) return;
        if (idx < 0 || idx >= JokerCloneSystem.Clones.Count) return;
        if (JokerCloneSystem.Clones[idx].IsPreview) return;

        try { __instance.Button.SetEnabled(); } catch { }
    }
}