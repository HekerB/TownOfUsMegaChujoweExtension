using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class VoodooBlindModifier(PlayerControl voodooMaster, float duration) : TimedModifier
{
    public PlayerControl VoodooMaster { get; } = voodooMaster;
    public float VisionPerc { get; private set; } = 1f;
    public override string ModifierName => "Voodoo Blindness";
    public override bool HideOnUi => true;
    public override LoadableAsset<Sprite>? ModifierIcon => null;
    public override float Duration => duration;
    public override bool AutoStart => true;

    public override void OnActivate()
    {
        base.OnActivate();

        VisionPerc = 1f;

        if (Player.AmOwner &&
            !voodooMaster.AmOwner)
        {
            var notification = Helpers.CreateAndShowNotification(
                $"<b>{Palette.ImpostorRed.ToTextColor()}{TouLocale.Get("ExtensionVoodooBlindAlert", "You have been cursed by the Voodoo Master!")}</color></b>",
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TouExtensionIcons.VoodooRoleIcon.LoadAsset());

            notification.AdjustNotification();
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (TimeRemaining > Duration - 1f)
        {
            VisionPerc = Mathf.Clamp01(TimeRemaining - Duration + 1f);
        }
        else if (TimeRemaining < 1f)
        {
            VisionPerc = Mathf.Clamp01(1f - TimeRemaining);
        }
        else
        {
            VisionPerc = 0f;
        }
    }

    public override void OnMeetingStart()
    {
        Player.RemoveModifier(this);
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();

        VisionPerc = 1f;
    }
}
