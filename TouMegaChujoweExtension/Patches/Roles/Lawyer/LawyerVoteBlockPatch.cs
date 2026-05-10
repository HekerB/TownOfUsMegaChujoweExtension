using BepInEx.Logging;
using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Lawyer;

[HarmonyPatch(typeof(PlayerVoteArea))]
public static class LawyerVoteBlockPatch
{


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
        
        if (isObjected)
        {
            if (LawyerEvents.TryGetOriginalVote(localPlayerId, out var originalVote))
            {
                
                if (originalVote == targetPlayerId)
                {
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

        }

        CurrentVotes[localPlayerId] = targetPlayerId;

        return true;
    }
}














