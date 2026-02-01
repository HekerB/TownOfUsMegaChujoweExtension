using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TouMiraRolesExtension.Modifiers.Universal;
using TouMiraRolesExtension.Options.Modifiers;
using TownOfUs.Utilities;

namespace TouMiraRolesExtension.Events.Universal;

public static class SpitefulEvents
{
    [RegisterEvent]
    public static void EjectionEventHandler(EjectionEvent @event)
    {
        var exiled = @event.ExileController?.initData?.networkedPlayer?.Object;
        if (exiled == null || !exiled.HasModifier<SpitefulModifier>())
        {
            return;
        }

        if (MeetingHud.Instance == null)
        {
            return;
        }

        var options = OptionGroupSingleton<UniversalModifierOptions>.Instance;
        var effectType = options.SpitefulEffectType.Value;
        var durationType = options.SpitefulDurationType.Value;
        var rounds = (int)options.SpitefulRoundCount.Value;
        var impact = options.SpitefulImpact;

        foreach (var state in MeetingHud.Instance.playerStates)
        {
            // VotedFor is the byte ID of the player being voted for
            if (state.AmDead || state.VotedFor != exiled.PlayerId)
            {
                continue;
            }

            var voter = MiscUtils.PlayerById(state.TargetPlayerId);
            if (voter != null && !voter.HasDied())
            {
                voter.AddModifier(new SpitefulEffectModifier(
                    effectType,
                    durationType,
                    rounds,
                    impact
                ));
            }
        }
    }
}
