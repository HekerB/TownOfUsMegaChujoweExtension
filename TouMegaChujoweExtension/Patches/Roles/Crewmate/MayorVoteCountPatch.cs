using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Events.Vanilla.Meeting.Voting;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Events.Crewmate;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using MiraAPI.Modifiers;
using TMPro;

namespace TouMegaChujoweExtension.Patches.Roles.Crewmate;

[HarmonyPatch]
public static class MayorVoteCountPatch
{
    // Part 1: Initial vote count setup (called at meeting start)
    [HarmonyPatch(typeof(MayorRole), nameof(MayorRole.OnMeetingStart))]
    [HarmonyPostfix]
    public static void PostfixOnMeetingStart(MayorRole __instance)
    {
        if (__instance.Player == null) return;
        
        var opts = OptionGroupSingleton<MayorExtensionOptions>.Instance;
        if (opts == null) return;

        var voteData = __instance.Player.GetVoteData();
        if (voteData != null)
        {
            var count = (int)opts.VoteCount;
            voteData.SetRemainingVotes(count);
            
            // Sync with any role fields (using reflection just in case)
            var names = new[] { "Votes", "VoteCount", "Amount", "ExtraVotes", "votes", "voteCount" };
            foreach (var name in names)
            {
                var field = AccessTools.Field(typeof(MayorRole), name);
                if (field != null) try { field.SetValue(__instance, count); } catch { }
                
                var prop = AccessTools.Property(typeof(MayorRole), name);
                if (prop != null && prop.CanWrite) try { prop.SetValue(__instance, count); } catch { }
            }
        }
    }

    // Part 2: Hijack the actual voting logic in the base mod's event system
    [HarmonyPatch(typeof(MayorEvents), nameof(MayorEvents.HandleVoteEvent))]
    [HarmonyPrefix]
    public static bool PrefixHandleVoteEvent(HandleVoteEvent @event)
    {
        // Check if the owner of the vote is a revealed Mayor
        if (@event.VoteData.Owner.Data.Role is not MayorRole mayor || !mayor.Revealed)
        {
            return true; // Let base mod handle non-mayors or unrevealed mayors
        }

        var opts = OptionGroupSingleton<MayorExtensionOptions>.Instance;
        var count = opts != null ? (int)opts.VoteCount : 3;

        // Clear existing votes and add our configured amount
        @event.VoteData.SetRemainingVotes(0);

        for (var i = 0; i < count; i++)
        {
            @event.VoteData.VoteForPlayer(@event.TargetId);
        }

        // Cancel the original event so the base mod doesn't add its hardcoded 3 votes
        @event.Cancel();
        return false;
    }

    // Part 3: Fix the UI text (Priority -1 to run after TownOfUs patch)
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.UpdateTimerText))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Low)]
    public static void PostfixUpdateTimerText(MeetingHud __instance)
    {
        var opts = OptionGroupSingleton<MayorExtensionOptions>.Instance;
        if (opts == null) return;
        var count = (int)opts.VoteCount;

        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return;
        
        var timerText = __instance.TimerText;
        if (timerText == null) return;
        var currentText = timerText.text;
        if (string.IsNullOrEmpty(currentText)) return;

        // --- MAYOR UI ---
        if (PlayerControl.LocalPlayer.Data.Role is MayorRole mayor)
        {
            if (count == 3) return; // Default, no need to replace
            
            if (mayor.Revealed)
            {
                if (currentText.Contains("3 votes at once!"))
                    timerText.text = currentText.Replace("3 votes at once!", $"{count} votes at once!");
                else if (currentText.Contains("3 głosy na raz!"))
                    timerText.text = currentText.Replace("3 głosy na raz!", $"{count} głosy na raz!");
                else if (currentText.Contains("3 glosy na raz!"))
                    timerText.text = currentText.Replace("3 glosy na raz!", $"{count} glosy na raz!");
            }
            else
            {
                if (currentText.Contains("3 total votes!"))
                    timerText.text = currentText.Replace("3 total votes!", $"{count} total votes!");
                else if (currentText.Contains("łącznie 3 głosy!"))
                    timerText.text = currentText.Replace("łącznie 3 głosy!", $"łącznie {count} głosy!");
                else if (currentText.Contains("lacznie 3 glosy!"))
                    timerText.text = currentText.Replace("lacznie 3 glosy!", $"lacznie {count} glosy!");
            }
        }
        // --- PRESIDENT UI ---
        else if (PlayerControl.LocalPlayer.Data.Role is PresidentRole president)
        {
            var voteData = PlayerControl.LocalPlayer.GetVoteData();
            if (voteData == null) return;

            var remaining = voteData.VotesRemaining;
            var knightBonus = 0;
            
            bool isKnight = false;
            foreach (var mod in PlayerControl.LocalPlayer.GetModifiers<MiraAPI.Modifiers.BaseModifier>()) {
                if (mod.GetType().Name.Contains("KnightedModifier")) {
                    isKnight = true;
                    break;
                }
            }

            if (isKnight)
            {
                var monarchOpts = OptionGroupSingleton<TownOfUs.Options.Roles.Crewmate.MonarchOptions>.Instance;
                if (monarchOpts != null) knightBonus = (int)monarchOpts.VotesPerKnight;
            }

            string info;
            if (knightBonus > 0)
            {
                info = "\n" + string.Format(TouLocale.Get("ExtensionMeetingPresidentBankInfoKnighted"), remaining, knightBonus);
            }
            else
            {
                info = "\n" + string.Format(TouLocale.Get("ExtensionMeetingPresidentBankInfo"), remaining);
            }

            // Append to the timer text if it's not already there
            // We use the localized strings to check for presence
            var bankCheckEn = "bank votes";
            var bankCheckPl = "glosow w banku";

            if (!currentText.Contains(bankCheckEn) && !currentText.Contains(bankCheckPl))
            {
                timerText.text = currentText + info;
            }
            else
            {
                // Update existing bank info
                var lines = currentText.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains(bankCheckEn) || lines[i].Contains(bankCheckPl))
                    {
                        lines[i] = info.TrimStart('\n');
                        break;
                    }
                }
                timerText.text = string.Join("\n", lines);
            }
        }
    }
}
