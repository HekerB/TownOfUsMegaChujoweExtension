using MiraAPI.Modifiers;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Utilities.Appearances;
using TownOfUs.Modifiers;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class AstralInvisibilityModifier : ConcealedModifier, IVisualAppearance
{
    public override string ModifierName => "AstralPostTeleportInvis";
    public override float Duration => OptionGroupSingleton<AstralOptions>.Instance.InvisibilityDuration.Value;
    public override bool HideOnUi => true;
    public override bool AutoStart => true;
    public bool VisualPriority => true;

    public VisualAppearance GetVisualAppearance()
    {
        return new VisualAppearance(Player.GetDefaultModifiedAppearance(), TownOfUsAppearances.Swooper)
        {
            HatId = string.Empty,
            SkinId = string.Empty,
            VisorId = string.Empty,
            PlayerName = string.Empty,
            PetId = string.Empty,
            RendererColor = Player.AmOwner ? new Color(0f, 0f, 0f, 0.1f) : Color.clear,
            NameColor = Color.clear,
            ColorBlindTextColor = Color.clear
        };
    }

    public override void OnActivate()
    {
        Player.RawSetAppearance(this);
        Player.cosmetics.ToggleNameVisible(false);
    }

    public override void OnDeactivate()
    {
        Player.ResetAppearance();
        Player.cosmetics.ToggleNameVisible(true);
    }

    public override void OnDeath(DeathReason reason)
    {
        Player.RemoveModifier(this);
    }

    public override void OnMeetingStart()
    {
        Player.RemoveModifier(this);
    }
}
