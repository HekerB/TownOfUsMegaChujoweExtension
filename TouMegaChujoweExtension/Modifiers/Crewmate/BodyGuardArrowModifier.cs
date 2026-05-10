using MiraAPI.GameOptions;
using TownOfUs.Modifiers;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Crewmate;

public sealed class BodyguardBacklashArrowModifier(PlayerControl owner, Color color)
    : ArrowTargetModifier(owner, color, 0)
{
    public override string ModifierName => "Bodyguard Backlash Arrow";

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (Arrow == null) return;

        var local = PlayerControl.LocalPlayer;
        if (local == null || Player == null || Player.Data == null || Player.Data.IsDead)
        {
            Arrow.gameObject.SetActive(false);
            return;
        }

        if (Owner == null || !Owner.AmOwner)
        {
            Arrow.gameObject.SetActive(false);
            return;
        }

        if (!OptionGroupSingleton<BodyguardOptions>.Instance.ShowBacklashArrow)
        {
            Arrow.gameObject.SetActive(false);
            return;
        }

        Arrow.gameObject.SetActive(true);
    }

    public override void OnMeetingStart()
    {
        base.OnMeetingStart();
        ModifierComponent!.RemoveModifier(this);
    }
}














