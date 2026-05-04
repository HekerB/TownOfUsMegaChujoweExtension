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
    // // private static readonly BepInEx.Logging.ManualLogSource Log =
        // // BepInEx.Logging.Logger.CreateLogSource("BH");

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
        ClearArrowModifiers();

        if (bh == null || bh.Data == null || bh.Data.IsDead) return;

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
            // Log.LogWarning("[BH] No candidates for target!");
            return;
        }

        CurrentTarget = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        LastTargetPlayerId = CurrentTarget.PlayerId;
        TargetKilledThisRound = false;

        // Log.LogWarning($"[BH] New target assigned: {CurrentTarget.Data.PlayerName} (PlayerId={CurrentTarget.PlayerId}), BH PlayerId={bh.PlayerId}");

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

        // Log.LogWarning($"[BH] Target killed! {KillsDone}/{needed}");

        if (KillsDone >= needed)
        {
            HasWon = true;
            ClearArrowModifiers();
            // Log.LogWarning("[BH] WIN CONDITION MET! HasWon=true");
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
