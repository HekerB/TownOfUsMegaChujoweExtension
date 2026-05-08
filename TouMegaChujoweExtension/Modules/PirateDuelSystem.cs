using System.Collections;
using System.Reflection;
using Reactor.Utilities;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Events;
using TownOfUs.Utilities;
using MiraAPI.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modules;

public static class PirateDuelSystem
{
    private static MethodInfo? _coAnimateDeathMethod;

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

    /// <summary>
    /// Plays the TownOfUs meeting death animation via reflection on the private CoAnimateDeath method.
    /// </summary>
    public static void AnimateMeetingDeath(byte targetId)
    {
        if (MeetingHud.Instance == null) return;

        var voteArea = MeetingHud.Instance.playerStates
            .FirstOrDefault(x => x.TargetPlayerId == targetId);

        if (voteArea == null)
        {
            return;
        }

        // Cache the reflection lookup
        if (_coAnimateDeathMethod == null)
        {
#pragma warning disable S3011
            _coAnimateDeathMethod = typeof(TownOfUsEventHandlers).GetMethod(
                "CoAnimateDeath",
                BindingFlags.NonPublic | BindingFlags.Static);
#pragma warning restore S3011

            if (_coAnimateDeathMethod == null)
            {
                // Fallback - just show dead overlay
                voteArea.AmDead = true;
                voteArea.Overlay.gameObject.SetActive(true);
                voteArea.XMark.gameObject.SetActive(true);
                return;
            }
        }

        try
        {
            var coroutine = _coAnimateDeathMethod.Invoke(null, new object[] { voteArea }) as IEnumerator;
            if (coroutine != null)
            {
                Coroutines.Start(coroutine);
            }
        }
        catch (System.Exception)
        {
            voteArea.AmDead = true;
            voteArea.Overlay.gameObject.SetActive(true);
            voteArea.XMark.gameObject.SetActive(true);
        }
    }
}
