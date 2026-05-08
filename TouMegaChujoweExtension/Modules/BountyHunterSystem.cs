using System.Linq;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Modifiers.Universal;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using UnityEngine;

namespace TouMegaChujoweExtension.Modules;

public static class BountyHunterSystem
{


    public static PlayerControl? CurrentTarget { get; set; }
    public static byte? LastTargetPlayerId { get; set; }
    public static int KillsDone { get; set; }
    public static bool TargetKilledThisRound { get; set; }
    public static bool HasWon { get; set; }
    public static bool GameEndedByBH { get; set; }

    public static bool IntroFinished { get; set; }
    public static float IntroFinishTime { get; set; }
    public static bool Hunting { get; set; }
    public static byte? BountyHunterPlayerId { get; set; }

    public static void AssignNewTarget(PlayerControl bh)
    {
        if (bh == null || bh.Data == null || bh.Data.IsDead || HasWon) return;

        if (CurrentTarget != null && !CurrentTarget.Data.IsDead && !CurrentTarget.Data.Disconnected)
            return;

        ClearArrowModifiers();

        var candidates = PlayerControl.AllPlayerControls.ToArray()
            .Where(p => p != null
                        && p.Data != null
                        && !p.Data.IsDead
                        && !p.Data.Disconnected
                        && p.PlayerId != bh.PlayerId
                        && (!p.TryGetModifier<ChildModifier>(out var child) || child.IsAdult))
            .ToList();

        if (candidates.Count > 1 && CurrentTarget != null)
        {
            var filtered = candidates.Where(p => p.PlayerId != CurrentTarget.PlayerId).ToList();
            if (filtered.Count > 0)
                candidates = filtered;
        }

        if (candidates.Count == 0)
        {
            CurrentTarget = null;
            LastTargetPlayerId = null;
            return;
        }

        CurrentTarget = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        LastTargetPlayerId = CurrentTarget.PlayerId;
        TargetKilledThisRound = false;

        var opts = OptionGroupSingleton<BountyHunterOptions>.Instance;
        var needed = (int)opts.TargetsToKill.Value;
        if (KillsDone >= needed)
        {
            HasWon = true;
            ClearArrowModifiers();
            return;
        }

        if (bh.AmOwner && CurrentTarget != null)
        {
            CurrentTarget.AddModifier<BountyHunterArrowModifier>(bh, TouExtensionColors.BountyHunter);
        }
    }

    public static void ClearArrowModifiers()
    {
        var players = ModifierUtils.GetPlayersWithModifier<BountyHunterArrowModifier>();
        foreach (var player in players)
        {
            player.RemoveModifier<BountyHunterArrowModifier>();
        }
    }

    public static void OnTargetKilled(PlayerControl bh)
    {
        if (HasWon) return;

        KillsDone++;
        TargetKilledThisRound = true;

        var opts = OptionGroupSingleton<BountyHunterOptions>.Instance;
        var needed = (int)opts.TargetsToKill.Value;



        if (KillsDone >= needed)
        {
            HasWon = true;
            ClearArrowModifiers();
            return;
        }

        if (bh.AmOwner)
        {
            AssignNewTarget(bh);
        }
    }

    public static void Reset()
    {
        CurrentTarget = null;
        LastTargetPlayerId = null;
        KillsDone = 0;
        TargetKilledThisRound = false;
        HasWon = false;
        GameEndedByBH = false;
        IntroFinished = false;
        IntroFinishTime = 0f;
        Hunting = false;
        BountyHunterPlayerId = null;
        ClearArrowModifiers();
    }
}
