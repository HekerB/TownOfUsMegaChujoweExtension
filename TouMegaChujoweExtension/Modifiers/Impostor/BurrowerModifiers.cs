using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Modifiers;
using TownOfUs.Utilities;
using TownOfUs.Utilities.Appearances;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class BurrowerInvisibleModifier : ConcealedModifier, IVisualAppearance
{
    public override string ModifierName => "Burrowed";
    public override bool HideOnUi => true;
    public override bool AutoStart => true;

    public VisualAppearance GetVisualAppearance()
    {
        var isLocal = Player == PlayerControl.LocalPlayer;
        var isImpostor = PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.IsImpostorAligned();

        var playerColor = isLocal
            ? new Color(1f, 1f, 1f, 0.5f)
            : isImpostor
                ? new Color(1f, 0f, 0f, 0.25f)
                : Color.clear;
        var nameColor = isLocal || isImpostor ? new Color(1f, 0f, 0f, isLocal ? 0.5f : 0.25f) : Color.clear;

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

        if (Player != PlayerControl.LocalPlayer &&
            (PlayerControl.LocalPlayer == null || !PlayerControl.LocalPlayer.IsImpostorAligned()))
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

public sealed class BurrowerSpeedModifier(float speedMultiplier) : TimedModifier, IVisualAppearance
{
    public override string ModifierName => "Burrow Speed";
    public override float Duration => 10f;
    public override bool HideOnUi => true;

    public VisualAppearance GetVisualAppearance()
    {
        var appearance = Player.GetDefaultModifiedAppearance();
        var role = Player.GetRole<BurrowerRole>();
        appearance.Speed *= role?.GetUndergroundSpeedMultiplier() ?? speedMultiplier;
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
