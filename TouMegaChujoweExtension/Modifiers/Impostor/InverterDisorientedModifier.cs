using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Modifiers;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using MiraAPI.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers;

/// <summary>
/// Modifier applied to players disoriented by the Inverter.
/// Flips screen upside-down and inverts WASD controls.
/// </summary>
public sealed class InverterDisorientedModifier : DisabledModifier
{
    private readonly float _duration;

    public InverterDisorientedModifier(float duration)
    {
        _duration = duration;
    }

    public override string ModifierName => TouLocale.Get("ExtensionModifierDisoriented", "Disoriented");
    public override bool HideOnUi => true;
    public override float Duration => _duration;
    public override bool AutoStart => true;
    public override bool CanUseAbilities => true;
    public override bool CanUseConsoles => true;
    public override bool CanOpenMap => true;
    public override bool CanReport => true;
    public override bool CanBeInteractedWith => true;
    public override bool IsConsideredAlive => true;

    public override string GetDescription()
    {
        var seconds = Mathf.CeilToInt(Mathf.Max(0f, TimeRemaining));
        return $"You are disoriented!\nRemaining: {seconds}s";
    }

    public override void OnActivate()
    {
        if (Player != null && Player.AmOwner)
        {
            SetCameraFlipped(true);
        }
    }

    public override void OnDeactivate()
    {
        if (Player != null && Player.AmOwner)
        {
            SetCameraFlipped(false);
        }
    }

    public override void OnDeath(DeathReason reason)
    {
        if (Player != null && Player.AmOwner)
        {
            SetCameraFlipped(false);
        }
        Player?.RemoveModifier(this);
    }

    /// <summary>
    /// Flips the main camera upside-down using InverterCameraBehaviour.
    /// </summary>
    private static void SetCameraFlipped(bool flipped)
    {
        var camObj = HudManager.Instance?.PlayerCam?.gameObject ?? Camera.main?.gameObject;
        if (camObj == null) return;

        var inverter = camObj.GetComponent<InverterCameraBehaviour>();
        if (inverter == null)
        {
            if (!flipped) return;
            inverter = camObj.AddComponent<InverterCameraBehaviour>();
        }

        inverter.SetDisoriented(flipped);
    }
}
