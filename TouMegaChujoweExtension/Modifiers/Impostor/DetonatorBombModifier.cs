using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using TouMegaChujoweExtension.Networking;
using UnityEngine;
using TownOfUs.Utilities;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Assets;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class DetonatorBombModifier : TimedModifier
{
    private PlayerControl _detonator;
    private float _duration;

    public override string ModifierName => "Bomb Attached";
    public override bool HideOnUi => true;
    public override float Duration => _duration;

    public DetonatorBombModifier(PlayerControl detonator, float duration)
    {
        _detonator = detonator;
        _duration = duration;
    }

    public override void OnActivate()
    {
        base.OnActivate();
        ResumeTimer();
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        
        if (Player == null || Player.HasDied() || MeetingHud.Instance)
        {
            if (Player != null) Player.RemoveModifier(this);
            return;
        }
    }

    public override void OnTimerComplete()
    {
        base.OnTimerComplete();
        // Detonation is handled by DetonatorSystem to ensure persistence and correct killer attribution
    }
}
