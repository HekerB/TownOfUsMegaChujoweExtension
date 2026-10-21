using MiraAPI.Utilities;
using Reactor.Utilities;
using TownOfUs.Networking;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modules;

public static class PirateDuelSystem
{
    public static int GetDuelResult(int pirateChoice, int targetChoice)
    {
        if (pirateChoice == targetChoice)
        {
            return 0;
        }

        if ((pirateChoice == 0 && targetChoice == 2) ||
            (pirateChoice == 1 && targetChoice == 0) ||
            (pirateChoice == 2 && targetChoice == 1))
        {
            return 1;
        }

        return 2;
    }

    public static void FlashScreen(Color color, float duration = 0.5f, float alpha = 0.3f)
    {
        Coroutines.Start(MiscUtils.CoFlash(color, duration, alpha));
    }

    public static bool IsDuelValid(PirateRole pirateRole)
    {
        if (pirateRole.Player.HasDied())
        {
            return false;
        }

        if (pirateRole.DuelTargetId == byte.MaxValue)
        {
            return false;
        }

        var target = MiscUtils.PlayerById(pirateRole.DuelTargetId);
        return target != null && !target.HasDied();
    }

    public static void AnimateMeetingDeath(byte targetId)
    {
        if (MeetingHud.Instance == null) return;

        var voteArea = MeetingHud.Instance.playerStates
            .FirstOrDefault(x => x.TargetPlayerId == targetId);

        if (voteArea == null)
        {
            return;
        }

        try
        {
            // -1 picks a random nameplate death animation, the same way TOU does it.
            Coroutines.Start(CustomTouMurderRpcs.CoAnimateDeath(voteArea, -1));
        }
        catch (System.Exception)
        {
            voteArea.AmDead = true;
            voteArea.Overlay.gameObject.SetActive(true);
            voteArea.XMark.gameObject.SetActive(true);
        }
    }
}