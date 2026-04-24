using BepInEx.Logging;
using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Events.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Lawyer;

[HarmonyPatch(typeof(PlayerVoteArea))]
public static class LawyerVoteBlockPatch
{
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("LawyerEvents");

    private static readonly Dictionary<byte, byte> CurrentVotes = new();

    public static byte? GetCurrentVote(byte playerId)
    {
        return CurrentVotes.TryGetValue(playerId, out var vote) ? vote : null;
    }

    public static void ClearVotes()
    {
        CurrentVotes.Clear();
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(PlayerVoteArea.Select))]
    public static bool PlayerVoteAreaSelectPrefix(PlayerVoteArea __instance)
    {
        if (!PlayerControl.LocalPlayer.AmOwner)
        {
            return true;
        }

        var options = OptionGroupSingleton<LawyerOptions>.Instance;
        if (options == null || !options.ObjectionPreventsSameVote)
        {
            return true;
        }

        if (__instance == MeetingHud.Instance.SkipVoteButton)
        {
            return true;
        }

        var localPlayerId = PlayerControl.LocalPlayer.PlayerId;
        var targetPlayerId = __instance.TargetPlayerId;

        var isObjected = LawyerEvents.IsObjectedVoter(localPlayerId);
        Logger.LogWarning($"[Lawyer] IsObjectedVoter({localPlayerId}): {isObjected}");
        Logger.LogWarning($"[Lawyer] CurrentVotes count: {CurrentVotes.Count}, ObjectedVoterOriginalVotes count: {LawyerEvents.ObjectedVoterOriginalVotes.Count}");
        
        if (isObjected)
        {
            if (LawyerEvents.TryGetOriginalVote(localPlayerId, out var originalVote))
            {
                Logger.LogWarning($"[Lawyer] Objected player {localPlayerId} - Original vote: {originalVote}, Current target: {targetPlayerId}, Match: {originalVote == targetPlayerId}");
                
                if (originalVote == targetPlayerId)
                {
                    Logger.LogWarning($"[Lawyer] BLOCKING VOTE - Same person!");
                    var msg = TouLocale.Get("ExtensionLawyerCannotVoteSamePerson");

                    var notif = Helpers.CreateAndShowNotification(
                        $"<b>{Color.white.ToTextColor()}{msg}</color></b>",
                        Color.white,
                        new Vector3(0f, 1f, -20f),
                        spr: TownOfUs.Assets.TouRoleIcons.Lawyer.LoadAsset());
                    notif.AdjustNotification();

                    return false;
                }
            }
            else
            {
                Logger.LogWarning($"[Lawyer] IsObjectedVoter returned true but TryGetOriginalVote failed for player {localPlayerId}");
            }
        }

        CurrentVotes[localPlayerId] = targetPlayerId;
        Logger.LogWarning($"[Lawyer] Tracking vote - Player {localPlayerId} voting for {targetPlayerId}");

        return true;
    }
}