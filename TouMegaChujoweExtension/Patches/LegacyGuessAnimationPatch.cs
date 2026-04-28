using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.LocalSettings;
using MiraAPI.Modifiers;
using TownOfUs.Modifiers.Game;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles.Neutral;

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

        if (source == null || source.Data == null || target == null || target.Data == null)
        {
            return;
        }

        var isMeetingGuesser =
            source.HasModifier<AssassinModifier>() ||
            source.Data.Role is VigilanteRole ||
            source.Data.Role is DoomsayerRole ||
            source.Data.Role is JailorRole;

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
            HudManager.Instance.KillOverlay.ShowKillAnimation(target.Data, target.Data);
        }
        catch (System.Exception e)
        {
            Warning($"[LegacyGuessAnimationPatch] Error showing legacy animation: {e.Message}");
        }
    }
}
