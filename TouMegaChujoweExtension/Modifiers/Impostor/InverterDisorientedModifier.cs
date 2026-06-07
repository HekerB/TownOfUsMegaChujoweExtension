using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities.Appearances;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class InverterDisorientedModifier : TimedModifier, IVisualAppearance
{
    private readonly float duration;

    public InverterDisorientedModifier(float duration)
    {
        this.duration = duration;
    }

    public override string ModifierName => TouLocale.Get("ExtensionModifierDisoriented", "Disoriented");
    public override bool HideOnUi => true;
    public override LoadableAsset<Sprite>? ModifierIcon => null;
    public override float Duration => duration;
    public override bool AutoStart => true;

    public override void OnActivate()
    {
        Player.RawSetAppearance(this);
        if (Player.AmOwner)
        {
            InverterCameraBehaviour.Apply();
        }
    }

    public override void OnDeactivate()
    {
        Player?.ResetAppearance(fullReset: true);
        if (Player != null && Player.AmOwner)
        {
            InverterCameraBehaviour.ResetCamera();
        }
    }

    public override void OnMeetingStart()
    {
        if (Player.AmOwner)
        {
            InverterCameraBehaviour.ResetCamera();
        }

        Player.RemoveModifier(this);
    }

    public VisualAppearance GetVisualAppearance()
    {
        return Player.GetDefaultAppearance();
    }
}
