using Il2CppInterop.Runtime.Attributes;
using MiraAPI.Modifiers.Types;
using Reactor.Utilities;
using Reactor.Utilities.Extensions;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

public sealed class SoulDeathArrowModifier(Vector3 targetPosition) : TimedModifier
{
    public override string ModifierName => "Soul Death Arrow";
    public override bool HideOnUi => true;
    public override float Duration => 0.5f;

    [HideFromIl2Cpp] public bool IsHiddenFromList => true;

    private ArrowBehaviour? _arrow;

    public override void OnActivate()
    {
        base.OnActivate();

        if (!Player.AmOwner)
        {
            return;
        }

        _arrow = MiscUtils.CreateArrow(Player.transform, TouExtensionColors.SoulCollector);
        _arrow.target = targetPosition;
        _arrow.Update();
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();

        if (!_arrow.IsDestroyedOrNull())
        {
            _arrow?.gameObject.Destroy();
            _arrow?.Destroy();
        }

        _arrow = null;
    }
}
