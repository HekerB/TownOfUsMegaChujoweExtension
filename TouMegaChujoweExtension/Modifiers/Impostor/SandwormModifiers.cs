using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using TownOfUs.Modifiers;
using TownOfUs.Utilities.Appearances;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers;

public sealed class SandwormInvisibleModifier : ConcealedModifier, IVisualAppearance
{
    public override string ModifierName => "Invisible";
    public override bool HideOnUi => true;
    public override bool AutoStart => true;

    public VisualAppearance GetVisualAppearance()
    {
        bool isLocal = Player == PlayerControl.LocalPlayer;
        bool isImpostor = PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.IsImpostorAligned();

        Color playerColor;
        if (isLocal)
        {
            playerColor = new Color(1f, 1f, 1f, 0.5f); 
        }
        else if (isImpostor)
        {
            playerColor = new Color(1f, 0f, 0f, 0.25f);
        }
        else
        {
            playerColor = Color.clear;
        }

        Color nameColor = isLocal ? new Color(1f, 0f, 0f, 0.5f) : (isImpostor ? new Color(1f, 0f, 0f, 0.25f) : Color.clear);

        return new VisualAppearance(Player.GetDefaultModifiedAppearance(), TownOfUsAppearances.Swooper)
        {
            HatId = isLocal ? Player.GetDefaultModifiedAppearance().HatId : string.Empty,
            SkinId = isLocal ? Player.GetDefaultModifiedAppearance().SkinId : string.Empty,
            VisorId = isLocal ? Player.GetDefaultModifiedAppearance().VisorId : string.Empty,
            PlayerName = isLocal || isImpostor ? Player.GetDefaultModifiedAppearance().PlayerName : string.Empty,
            PetId = isLocal ? Player.GetDefaultModifiedAppearance().PetId : string.Empty,
            RendererColor = playerColor,
            NameColor = nameColor,
            ColorBlindTextColor = nameColor
        };
    }

    public override void OnActivate()
    {
        Player.RawSetAppearance(this);
        // Only toggle name invisible if not local/impostor
        bool isLocal = Player == PlayerControl.LocalPlayer;
        bool isImpostor = PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.IsImpostorAligned();
        if (!isLocal && !isImpostor)
        {
            Player.cosmetics.ToggleNameVisible(false);
        }
    }

    public override void OnDeactivate()
    {
        Player.ResetAppearance();
        Player.cosmetics.ToggleNameVisible(true);
    }
}

public sealed class SandwormSpeedModifier : TimedModifier, IVisualAppearance
{
    private readonly float _speedMultiplier;

    public SandwormSpeedModifier(float speedMultiplier)
    {
        _speedMultiplier = speedMultiplier;
    }

    public override string ModifierName => "Speed";
    public override float Duration => 10f; // Managed by role
    public override bool HideOnUi => true;

    public VisualAppearance GetVisualAppearance()
    {
        var appearance = Player.GetDefaultModifiedAppearance();
        appearance.Speed *= _speedMultiplier;
        return appearance;
    }

    public override void OnActivate()
    {
        Player.RawSetAppearance(this);
    }

    public override void OnDeactivate()
    {
        Player.ResetAppearance();
    }
}
