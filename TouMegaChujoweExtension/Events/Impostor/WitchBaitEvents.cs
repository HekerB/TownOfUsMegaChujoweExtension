using HarmonyLib;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events;
using MiraAPI.Modifiers;
using TownOfUs.Events.Modifiers;
using TownOfUs.Modifiers.Game.Crewmate;
using TownOfUs.Utilities;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Events.Impostor;

/// <summary>
/// Prevents Bait modifier from triggering when a Witch tags someone (spellbound modifier).
/// When a Witch tags Bait, the Bait should not force the Witch to report after the meeting.
/// </summary>
[HarmonyPatch(typeof(BaitEvents), nameof(BaitEvents.AfterMurderEventHandler))]
public static class WitchBaitEvents
{
    [HarmonyPrefix]
    public static bool Prefix(AfterMurderEvent @event)
    {
        var target = @event.Target;
        var source = @event.Source;

        if (target != null && target.HasModifier<BaitModifier>() &&
            source != null && source.IsRole<WitchRole>())
        {
            if (target.HasModifier<WitchSpellboundModifier>())
            {
                return false;
            }

            if (target.TryGetModifier<DeathHandlerModifier>(out var deathHandler) &&
                deathHandler.CauseOfDeath == TouLocale.Get("DiedToWitch"))
            {
                return false;
            }
        }

        return true;
    }
}


















