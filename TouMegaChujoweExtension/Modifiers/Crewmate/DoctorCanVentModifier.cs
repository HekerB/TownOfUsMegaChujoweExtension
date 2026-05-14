using TownOfUs.Modifiers.Game;
using TownOfUs.Modules.Wiki;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using UnityEngine;
using MiraAPI.Utilities.Assets;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Modifiers.Crewmate;

public sealed class DoctorCanVentModifier : TimedModifier, IWikiDiscoverable
{
    public override string ModifierName => TouLocale.Get("ExtensionModifierDoctorCanVent", "Doctor (Can Vent)");
    public override bool HideOnUi => true;
    public override LoadableAsset<Sprite>? ModifierIcon => null;

    private readonly float _duration;
    private readonly DoctorEffectDurationType _durationType;

    public DoctorCanVentModifier(float duration, DoctorEffectDurationType durationType)
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

    public override bool AutoStart => true;

    public override bool? CanVent()
    {
        return true;
    }

    public override void OnMeetingStart()
    {
        if (_durationType == DoctorEffectDurationType.AllRound)
        {
            Player.RemoveModifier(this);
        }
    }

    public override void OnDeactivate()
    {
        if (Player != null && Player.AmOwner)
        {
            if (Player.inVent)
            {
                var currentVent = Vent.currentVent;
                if (currentVent != null)
                {
                    currentVent.SetButtons(false);
                    Player.MyPhysics.RpcExitVent(currentVent.Id);
                }
                Player.MyPhysics.ExitAllVents();
            }

            if (HudManager.Instance != null && HudManager.Instance.ImpostorVentButton != null)
            {
                HudManager.Instance.ImpostorVentButton.gameObject.SetActive(false);
            }
        }
    }
}
