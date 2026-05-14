using HarmonyLib;
using MiraAPI.Events.Vanilla.Meeting.Voting;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using TownOfUs.Events.Misc;
using TownOfUs.Extensions;

namespace TouMegaChujoweExtension.Patches.Roles.President;

[HarmonyPatch(typeof(KnightedEvents), nameof(KnightedEvents.HandleVoteEvent))]
public static class PresidentKnightedVoteFixPatch
{
    [HarmonyPrefix]
    public static bool Prefix(HandleVoteEvent @event)
    {
        // If the voter is a President
        if (@event.VoteData.Owner.Data.Role is PresidentRole president)
        {
            // Robust check for the Knighted modifier
            bool isKnight = @event.VoteData.Owner.GetModifiers<BaseModifier>().Any(mod => mod.GetType().Name.Contains("KnightedModifier"));

            // If the President is a Knight AND hasn't used their bonus yet this meeting
            if (isKnight && !president.HasCastKnightedVote)
            {
                president.HasCastKnightedVote = true;
                
                // Retrieve the Monarch's bonus configuration
                var monarchOpts = OptionGroupSingleton<TownOfUs.Options.Roles.Crewmate.MonarchOptions>.Instance;
                if (monarchOpts != null)
                {
                    int bonus = (int)monarchOpts.VotesPerKnight;
                    // Manually cast the bonus votes for the target
                    for (int i = 0; i < bonus; i++)
                    {
                        @event.VoteData.VoteForPlayer(@event.TargetId);
                    }
                }
            }
            
            // ALWAYS return false for President.
            // This prevents KnightedEvents from calling SetRemainingVotes(0), 
            // which would destroy the President's remaining bank votes.
            return false; 
        }
        return true;
    }
}














