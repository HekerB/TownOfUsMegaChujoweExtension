using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TownOfUs.Modifiers;
using TownOfUs.Assets;
using TownOfUs.Modules.Anims;
using PowerTools;
using UnityEngine;
using TouMegaChujoweExtension.Events.Crewmate;
using Reactor.Utilities.Extensions;
using MiraAPI.GameOptions;
using TownOfUs.Extensions;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Modifiers.Crewmate;

public sealed class DoctorShieldModifier : BaseShieldModifier
{
    public override string ModifierName => "Doctor Shield";
    public override bool HideOnUi => true;

    private float _duration;
    private DoctorEffectDurationType _durationType;
    public PlayerControl Doctor { get; }
    public GameObject? ClericBarrier { get; set; }

    public DoctorShieldModifier(PlayerControl doctor, float duration, DoctorEffectDurationType durationType)
    {
        Doctor = doctor;
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

    public bool IsShieldVisible
    {
        get
        {
            var options = OptionGroupSingleton<DoctorOptions>.Instance;
            if (PlayerControl.LocalPlayer.PlayerId == Player.PlayerId) return options.TargetSeesShield;
            if (Doctor != null && PlayerControl.LocalPlayer.PlayerId == Doctor.PlayerId) return options.DoctorSeesShield;
            return false;
        }
    }

    public override bool VisibleSymbol
    {
        get
        {
            var options = OptionGroupSingleton<DoctorOptions>.Instance;
            if (PlayerControl.LocalPlayer.PlayerId == Player.PlayerId) return options.TargetSeesShield;
            if (Doctor != null && PlayerControl.LocalPlayer.PlayerId == Doctor.PlayerId) return options.DoctorSeesShield;
            return false;
        }
    }

    public override string ShieldDescription => "You are protected by the Doctor!";

    public override void OnActivate()
    {
        base.OnActivate();

        // Visual effect from Cleric
        ClericBarrier = AnimStore.SpawnAnimBody(Player, TouAssets.ClericBarrier.LoadAsset(), false, -1.1f, -0.35f, 1.5f)!;
        ClericBarrier.GetComponent<SpriteAnim>().SetSpeed(2f);
    }

    public override void OnMeetingStart()
    {
        if (_durationType == DoctorEffectDurationType.AllRound)
        {
            ModifierComponent?.RemoveModifier(this);
        }
    }

    public override void Update()
    {
        if (Player == null || Doctor == null)
        {
            ModifierComponent?.RemoveModifier(this);
            return;
        }

        if (!MeetingHud.Instance && ClericBarrier?.gameObject != null)
        {
            ClericBarrier?.SetActive(!Player.IsConcealed() && IsShieldVisible);
        }
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();
        if (ClericBarrier?.gameObject != null)
        {
            ClericBarrier.gameObject.Destroy();
        }
    }

}
