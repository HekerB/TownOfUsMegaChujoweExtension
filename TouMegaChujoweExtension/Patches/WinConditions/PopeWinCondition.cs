using MiraAPI.GameEnd;
using TouMegaChujoweExtension.GameOver;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Interfaces;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.WinConditions;

public sealed class PopeWinCondition : IWinCondition, IWinConditionWithBlocking
{
    public int Priority => 1;
    public bool BlocksOthers => true;

    public bool IsMet(LogicGameFlowNormal gameFlow)
    {
        if (AmongUsClient.Instance == null) return false;

        bool isJudgementActive = PopeJudgementSystem.Instance != null && PopeJudgementSystem.Instance.Stage >= PopeJudgementStage.Countdown;
        bool popeRaceCondition = false;

        if (isJudgementActive)
        {
            var alive = PlayerControl.AllPlayerControls.ToArray().Where(x => !x.HasDied() && !x.Data.Disconnected).ToList();
            if (alive.Count > 0 && alive.All(x => x.Data.Role is PopeRole))
            {
                popeRaceCondition = true;
            }
        }

        // Block others as soon as the countdown ends (Finished stage starts)
        // Or if the Pope is the only survivor during an active Judgement (prevents race condition when host sends death RPCs but client timer hasn't hit 0 yet)
        return PopeJudgementSystem.GlobalBombFinished || 
               (PopeJudgementSystem.Instance != null && PopeJudgementSystem.Instance.Stage == PopeJudgementStage.Finished) || 
               popeRaceCondition;
    }

    public void TriggerGameOver(LogicGameFlowNormal gameFlow)
    {
        // Handled in PopeJudgementSystem for precise timing after the animation
    }
}
