using MiraAPI.Modifiers.Types;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers;

public sealed class IcenbergBlizzardModifier(float duration) : TimedModifier
{
    public override string ModifierName => TouLocale.Get("ExtensionModifierIcenbergBlizzard", "Blizzard");
    public override bool HideOnUi => false;
    public override LoadableAsset<Sprite>? ModifierIcon => TouExtensionNeuAssets.BlizzardButtonSprite;
    public override float Duration => duration;

    private float _originalSpeed;

    public override void OnActivate()
    {
        if (Player == null)
        {
            return;
        }

        _originalSpeed = Player.MyPhysics.Speed;
        Player.MyPhysics.Speed = _originalSpeed * 0.5f;

    }

    public override void OnDeactivate()
    {
        if (Player != null)
        {
            Player.MyPhysics.Speed = _originalSpeed;
        }
    }
}
