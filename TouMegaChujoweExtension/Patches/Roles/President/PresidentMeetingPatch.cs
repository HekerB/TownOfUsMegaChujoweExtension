using HarmonyLib;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using TownOfUs.Modifiers.Impostor;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Roles.President;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
public static class PresidentMeetingPatch
{
    private static float _lastUpdate;

    [HarmonyPostfix]
    public static void Postfix(MeetingHud __instance)
    {
        if (UnityEngine.Time.time - _lastUpdate < 0.2f) return;
        _lastUpdate = UnityEngine.Time.time;
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || localPlayer.Data.Role is not PresidentRole presidentRole)
        {
            return;
        }

        if (presidentRole.AbstainButton == null)
        {
            return;
        }

        var voteData = localPlayer.GetVoteData();
        if (voteData == null)
        {
            return;
        }
        if (presidentRole.AbstainButton == null || presidentRole.AbstainButton.gameObject == null)
        {
            return;
        }
        if (__instance.state is MeetingHud.VoteStates.NotVoted or MeetingHud.VoteStates.Voted)
        {
            var tmp = presidentRole.AbstainButton.gameObject.GetComponentInChildren<TMPro.TextMeshPro>();
            if (tmp != null)
            {
                tmp.text = $"ABSTAIN ({voteData.VotesRemaining})";
            }
        }

        // Hide Skip and Abstain if blackmailed
        if (presidentRole.IsBlackmailActive())
        {
            __instance.SkipVoteButton.gameObject.SetActive(false);
            presidentRole.AbstainButton.gameObject.SetActive(false);
        }
    }
}














