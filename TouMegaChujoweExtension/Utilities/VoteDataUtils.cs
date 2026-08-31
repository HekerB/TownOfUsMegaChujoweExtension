using MiraAPI.Utilities;
using MiraAPI.Voting;

namespace TouMegaChujoweExtension.Utilities;

public static class VoteDataUtils
{
    /// <summary>
    /// Reads a player's vote data without throwing when they have none.
    /// </summary>
    /// <remarks>
    /// Mira's <c>GetVoteData</c> throws an <see cref="InvalidOperationException"/> when the component is
    /// not attached, so the <c>if (voteData == null)</c> guards this mod was written with could never
    /// fire - the exception escaped the caller instead, which around a meeting means one player in a bad
    /// state breaks the vote for everyone.
    /// </remarks>
    public static bool TryGetVoteData(this PlayerControl? player, out PlayerVoteData voteData)
    {
        voteData = null!;

        if (player == null || player.Pointer == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            voteData = player.GetVoteData();
        }
        catch
        {
            return false;
        }

        return voteData != null;
    }
}
