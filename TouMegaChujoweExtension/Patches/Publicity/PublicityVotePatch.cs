using HarmonyLib;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Crewmate;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Publicity;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.BloopAVoteIcon))]
public static class PublicityVotePatch
{
    [HarmonyPostfix]
    public static void Postfix([HarmonyArgument(0)] NetworkedPlayerInfo voterPlayer,
        [HarmonyArgument(2)] Transform parent)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || localPlayer.Data.IsDead)
            return;

        if (!localPlayer.HasModifier<PublicityModifier>())
            return;

        if (!GameOptionsManager.Instance.currentNormalGameOptions.AnonymousVotes)
            return;

        var voteSpreader = parent.GetComponent<VoteSpreader>();
        if (voteSpreader == null)
            return;

        var votes = voteSpreader.Votes;
        if (votes == null || votes.Count == 0)
            return;

        var lastVote = votes[votes.Count - 1];
        if (lastVote == null)
            return;

        PlayerMaterial.SetColors(voterPlayer.DefaultOutfit.ColorId, lastVote);
    }
}
