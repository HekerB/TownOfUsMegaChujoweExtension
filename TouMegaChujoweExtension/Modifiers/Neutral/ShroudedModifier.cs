using MiraAPI.Modifiers;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

public sealed class ShroudedModifier(PlayerControl shroudOwner) : BaseModifier
{
    private readonly Color _shroudColor = TouExtensionColors.Shroud;

    public override string ModifierName => "Shrouded";
    public override bool HideOnUi => true;

    public byte ShroudOwnerId { get; } = shroudOwner.PlayerId;
    public bool WasInteractedWith { get; set; }

    public override void FixedUpdate()
    {
        if (Player == null) return;

        if (PlayerControl.LocalPlayer.Data.Role is ShroudRole shroud &&
            shroud.Player.PlayerId == ShroudOwnerId &&
            Player != PlayerControl.LocalPlayer)
        {
            Player.cosmetics.SetOutline(true, new Il2CppSystem.Nullable<Color>(_shroudColor));
        }
    }

    public override void OnDeactivate()
    {
        Player?.cosmetics.SetOutline(false, new Il2CppSystem.Nullable<Color>(_shroudColor));
    }

    public override void OnDeath(DeathReason reason)
    {
        ModifierComponent?.RemoveModifier(this);
    }

    public override void OnMeetingStart()
    {
        if (Player == null || Player.HasDied())
        {
            ModifierComponent?.RemoveModifier(this);
            return;
        }

        if (WasInteractedWith)
        {
            ModifierComponent?.RemoveModifier(this);
            return;
        }

        // Kill handled by ShroudMeetingPatch
    }
}














