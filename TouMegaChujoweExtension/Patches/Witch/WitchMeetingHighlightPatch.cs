using HarmonyLib;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
public static class WitchMeetingHighlightPatch
{
    private static float _lastUpdate;

    [HarmonyPostfix]
    public static void UpdatePostfix(MeetingHud __instance)
    {
        if (UnityEngine.Time.time - _lastUpdate < 0.2f) return;
        _lastUpdate = UnityEngine.Time.time;
        if (__instance == null || __instance.playerStates == null)
        {
            return;
        }

        foreach (var voteArea in __instance.playerStates)
        {
            if (voteArea == null)
            {
                continue;
            }

            var player = MiscUtils.PlayerById(voteArea.TargetPlayerId);
            if (player == null || !player.HasModifier<WitchSpellboundModifier>())
            {
                continue;
            }

            voteArea.NameText.color = TouExtensionColors.Witch;
        }
    }
}
