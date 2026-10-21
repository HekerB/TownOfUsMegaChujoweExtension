using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Modules.Localization;
using TownOfUs.Modifiers;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers;

public sealed class IcenbergFrozenModifier(float duration) : DisabledModifier
{
    public override string ModifierName => TouLocale.Get("ExtensionModifierIcenbergFrozen", "Frozen");
    public override bool HideOnUi => false;
    public override LoadableAsset<Sprite>? ModifierIcon => TouExtensionIcons.IcenbergRoleIcon;
    public override float Duration => duration;
    public override bool AutoStart => true;
    public override bool CanUseAbilities => false;
    public override bool CanUseConsoles => false;
    public override bool CanOpenMap => false;
    public override bool CanReport => false;
    public override bool CanBeInteractedWith => true;
    public override bool IsConsideredAlive => true;

    public override void OnActivate()
    {
        if (Player != null && Player.AmOwner)
        {
            Player.NetTransform?.Halt();
            Player.moveable = false;
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        if (Player != null && Player.AmOwner)
        {
            Player.NetTransform?.Halt();
            Player.moveable = false;
        }
    }

    public override void OnDeactivate()
    {
        if (Player != null && Player.AmOwner)
        {
            Player.moveable = true;
        }
    }

    public override bool? CanVent() => false;
}
