using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers;

/// <summary>
/// Frozen by the Icenberg: cannot move, use abilities or consoles, open the map, report or vent.
/// Movement is held by <c>IcenbergFreezeCanMovePatch</c> on <c>PlayerControl.CanMove</c> while this modifier exists,
/// never by writing <c>PlayerControl.moveable</c>, which meetings, minigames and other effects also own - so the
/// end of the freeze releases only the freeze.
/// </summary>
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

    public override string GetDescription()
    {
        var seconds = Mathf.CeilToInt(Mathf.Max(0f, TimeRemaining));
        return TouLocale.Get("ExtensionModifierIcenbergFrozenTabDescription", "Frozen by the Icenberg: <seconds>s")
            .Replace("<seconds>", seconds.ToString(TownOfUs.TownOfUsPlugin.Culture));
    }

    public override void OnActivate()
    {
        base.OnActivate();
        if (Player != null && Player.AmOwner)
        {
            Player.NetTransform?.Halt();
            IcenbergOverlay.SetFrozen(true);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        if (Player != null && Player.AmOwner && Player.MyPhysics != null && Player.MyPhysics.body != null)
        {
            Player.MyPhysics.body.velocity = Vector2.zero;
        }
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();
        if (Player != null && Player.AmOwner)
        {
            IcenbergOverlay.SetFrozen(false);
        }
    }

    public override bool? CanVent() => false;
}
