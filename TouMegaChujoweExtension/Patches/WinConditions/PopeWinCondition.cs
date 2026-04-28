using MiraAPI.GameEnd;
using TouMegaChujoweExtension.GameOver;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Interfaces;

namespace TouMegaChujoweExtension.Patches.WinConditions;

public sealed class PopeWinCondition : IWinCondition, IWinConditionWithBlocking
{
    public int Priority => 11;
    public bool BlocksOthers => true;

    public bool IsMet(LogicGameFlowNormal gameFlow)
    {
        if (AmongUsClient.Instance == null) return false;

        // Block others as soon as the countdown ends (Finished stage starts)
        return PopeJudgementSystem.GlobalBombFinished || (PopeJudgementSystem.Instance != null && PopeJudgementSystem.Instance.Stage == PopeJudgementStage.Finished);
    }

    public void TriggerGameOver(LogicGameFlowNormal gameFlow)
    {
        // Handled in PopeJudgementSystem for precise timing after the animation
    }
}
