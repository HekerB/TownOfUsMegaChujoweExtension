using Il2CppInterop.Runtime.Attributes;
using MiraAPI.Modifiers.Types;
using Reactor.Utilities.Extensions;
using TownOfUs.Modules;
using TownOfUs.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;
using TouMegaChujoweExtension.Assets;
using Reactor.Utilities;

namespace TouMegaChujoweExtension.Modifiers;

/// <summary>
/// Arrow pointing to a dead body for the Vulture's Scavenge ability.
/// Handles Submerged map special case where arrows point to elevator if body is on different floor.
/// </summary>
public sealed class VultureBodyArrowModifier(DeadBody deadBody, byte bodyId) : TimedModifier
{
    public override string ModifierName => "Vulture Body Arrow";
    public override bool HideOnUi => true;
    public override bool AutoStart => true;
    public override bool RemoveOnComplete => false;
    public override float Duration => float.MaxValue;

    [HideFromIl2Cpp] public bool IsHiddenFromList => true;

    public DeadBody DeadBody { get; } = deadBody;
    public byte BodyId { get; } = bodyId;

    private ArrowBehaviour? _arrow;
    private Vector3? _elevatorTarget;

    public override void OnActivate()
    {
        base.OnActivate();

        if (!Player.AmOwner)
        {
            return;
        }

        _arrow = MiscUtils.CreateArrow(Player.transform, TouExtensionColors.Vulture);
        Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Vulture));
        UpdateArrowTarget();
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (!Player.AmOwner || _arrow == null)
        {
            return;
        }

        if (DeadBody == null || DeadBody.gameObject == null || !DeadBody.gameObject.activeSelf)
        {
            ModifierComponent?.RemoveModifier(this);
            return;
        }

        UpdateArrowTarget();

        if (_arrow != null)
        {
            var target = _elevatorTarget ?? DeadBody.transform.position;
            _arrow.target = target;
            _arrow.Update();
        }
    }

    private void UpdateArrowTarget()
    {
        if (DeadBody == null || Player == null)
        {
            return;
        }

        if (ModCompatibility.IsSubmerged())
        {
            var playerFloor = Player.transform.position.y > -7f;
            var bodyFloor = DeadBody.transform.position.y > -7f;

            if (playerFloor != bodyFloor)
            {
                var elevatorTarget = FindNearestElevator();
                if (elevatorTarget.HasValue)
                {
                    _elevatorTarget = elevatorTarget.Value;
                    return;
                }
            }
        }

        _elevatorTarget = null;
    }

    private static Vector3[]? _cachedElevators;
    private static int _lastMapId = -1;

    private Vector3? FindNearestElevator()
    {
        if (!ModCompatibility.IsSubmerged())
        {
            return null;
        }

        var currentMapId = GameOptionsManager.Instance.currentNormalGameOptions.MapId;
        if (_cachedElevators == null || _lastMapId != currentMapId)
        {
            try
            {
                var consoles = Object.FindObjectsOfType<Console>();
                var elevators = new List<Vector3>();
                foreach (var console in consoles)
                {
                    if (console != null && console.name != null && console.name.Contains("Elevator", System.StringComparison.OrdinalIgnoreCase))
                    {
                        elevators.Add(console.transform.position);
                    }
                }
#pragma warning disable S2696 // Cache updating
                _cachedElevators = elevators.ToArray();
                _lastMapId = currentMapId;
#pragma warning restore S2696
            }
            catch
            {
                return new Vector3(0f, -7f, 0f);
            }
        }

        if (_cachedElevators == null || _cachedElevators.Length == 0) return null;

        var playerPos = Player.transform.position;
        Vector3? closest = null;
        float minDist = float.MaxValue;

        foreach (var elevator in _cachedElevators)
        {
            float dist = Vector3.Distance(playerPos, elevator);
            if (dist < minDist)
            {
                minDist = dist;
                closest = elevator;
            }
        }

        return closest;
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();

        if (_arrow != null && !_arrow.IsDestroyedOrNull())
        {
            _arrow.gameObject.Destroy();
            _arrow.Destroy();
        }

        _arrow = null;
    }
}
