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
    // // private static readonly BepInEx.Logging.ManualLogSource Log =
        // // BepInEx.Logging.Logger.CreateLogSource("PirateDuelSystem");

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
            .ToArray().FirstOrDefault(x => x.TargetPlayerId == targetId);

        if (voteArea == null)
        {
            // Log.LogError($"Could not find vote area for player {targetId}");
            return;
        }

        // Cache the reflection lookup
        if (_coAnimateDeathMethod == null)
        {
            _coAnimateDeathMethod = typeof(TownOfUsEventHandlers).GetMethod(
                "CoAnimateDeath",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (_coAnimateDeathMethod == null)
            {
                // Log.LogError("Could not find CoAnimateDeath method via reflection");
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
            // Log.LogError($"Failed to invoke CoAnimateDeath");
            voteArea.AmDead = true;
            voteArea.Overlay.gameObject.SetActive(true);
            voteArea.XMark.gameObject.SetActive(true);
        }
    }
}
