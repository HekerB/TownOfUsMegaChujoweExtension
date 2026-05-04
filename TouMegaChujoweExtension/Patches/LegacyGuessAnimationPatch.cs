using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.LocalSettings;
using MiraAPI.Modifiers;
using TownOfUs.Modifiers.Game;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles.Neutral;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;

namespace TouMegaChujoweExtension.Patches;

public class LegacyGuessAnimationPatch
{
    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        if (!MeetingHud.Instance)
        {
            return;
        }

        if (!LocalSettingsTabSingleton<TouExtensionLocalSettings>.Instance.UseLegacyGuessDeathAnimation.Value)
        {
            return;
        }

        var source = @event.Source;
        var target = @event.Target;

        if (target == null || target.Data == null)
        {
            return;
        }

        var isMeetingGuesser = source != null && source.Data != null && IsGuesser(source);

        // Handle Missguess / Suicide for guessers (Vigilante/Doomsayer)
        // In some cases source might be null or equal to target for self-kills
        if (!isMeetingGuesser && IsGuesser(target) && (source == null || source.PlayerId == target.PlayerId))
        {
            isMeetingGuesser = true;
        }

        if (!isMeetingGuesser)
        {
            return;
        }

        if (HudManager.Instance == null || HudManager.Instance.KillOverlay == null)
        {
            return;
        }

        try
        {
            // If target is a Doppelganger, reveal their true form before showing animation
            if (target.TryGetModifier<DoppelgangerDisguiseModifier>(out var disguise))
            {
                disguise.OnDeactivate();
            }

            HudManager.Instance.KillOverlay.ShowKillAnimation(target.Data, target.Data);
        }
        catch (System.Exception e)
        {
            Warning($"[LegacyGuessAnimationPatch] Error showing legacy animation: {e.Message}");
        }
    }

    private static bool IsGuesser(PlayerControl player)
    {
        return player.HasModifier<AssassinModifier>() ||
               player.HasModifier<DeathNoteModifier>() ||
               player.Data.Role is VigilanteRole ||
               player.Data.Role is DoomsayerRole ||
               player.Data.Role is JailorRole ||
               player.Data.Role is PirateRole;
    }
}
