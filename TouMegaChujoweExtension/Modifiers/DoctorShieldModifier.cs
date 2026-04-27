using MiraAPI.Modifiers;
using TownOfUs.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Events.Crewmate;

namespace TouMegaChujoweExtension.Modifiers;

public sealed class DoctorShieldModifier : TimedModifier, IShieldModifier
{
    public override string ModifierName => "Doctor Shield";
    public override bool HideOnUi => true;

    private float _duration;
    private DoctorEffectDurationType _durationType;

    public DoctorShieldModifier(float duration, DoctorEffectDurationType durationType)
    {
        _duration = duration;
        _durationType = durationType;
    }

    public override float Duration
    {
        get
        {
            return _durationType switch
            {
                DoctorEffectDurationType.AllRound => -1f,
                DoctorEffectDurationType.AllGame => -1f,
                DoctorEffectDurationType.SetTime => _duration,
                _ => _duration
            };
        }
    }

    public override void OnDeactivate()
    {
        if (Player != null && Player.AmOwner)
        {
            DoctorEvents.ShowNotification(Player, "ExtensionDoctorNotificationShieldWoreOff", "Shield wore off");
        }
    }

    public bool ShouldCancelKill(PlayerControl killer, PlayerControl target) => true;

    public void OnKillCancelled(PlayerControl killer, PlayerControl target)
    {
        target.RemoveModifier(this);
        // Show flash or notification?
    }
}
