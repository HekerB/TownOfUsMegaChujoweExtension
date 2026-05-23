using MiraAPI.Utilities;
using Reactor.Networking.Attributes;

namespace TouMegaChujoweExtension.Modules;

public static class PresidentVoteSystem
{
    /// <summary>
    /// RPC sent when president abstains. Ensures all clients know the president
    /// has finished voting (VotesRemaining = 0) and host checks for end voting.
    /// </summary>
    [MethodRpc((uint)ExtensionRpc.PresidentAbstain)]
    public static void RpcPresidentAbstain(PlayerControl source, byte presidentPlayerId)
    {
        var president = GameData.Instance.GetPlayerById(presidentPlayerId)?.Object;
        if (president == null)
        {
            return;
        }

        var voteData = president.GetVoteData();
        if (voteData != null)
        {
            voteData.SetRemainingVotes(0);
        }

        // Mark as abstained on all clients
        if (president.Data.Role is PresidentRole presidentRole)
        {
            presidentRole.HasAbstained = true;
        }

        if (AmongUsClient.Instance.AmHost && MeetingHud.Instance != null)
        {
            MeetingHud.Instance.SetDirtyBit(1U);
            MeetingHud.Instance.CheckForEndVoting();
        }
    }
}