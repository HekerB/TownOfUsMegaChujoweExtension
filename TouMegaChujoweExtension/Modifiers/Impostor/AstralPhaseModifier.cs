using MiraAPI.Modifiers;
using MiraAPI.GameOptions;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Utilities.Appearances;
using TownOfUs.Modifiers;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using TownOfUs.Interfaces;
using UnityEngine;
using TownOfUs.Roles;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class AstralPhaseModifier : ConcealedModifier, IVisualAppearance
{
    public override string ModifierName => "AstralPhasing";
    public override float Duration => OptionGroupSingleton<AstralOptions>.Instance.PhaseDuration;
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
            RendererColor = new Color(0f, 0f, 0f, 0.1f),
            NameColor = Color.clear,
            ColorBlindTextColor = Color.clear
        };
    }

    public override void OnActivate()
    {
        Player.RawSetAppearance(this);
        Player.cosmetics.ToggleNameVisible(false);
        
        // Noclip
        Player.gameObject.layer = LayerMask.NameToLayer("Ghost");
    }

    public override void OnDeactivate()
    {
        Player.ResetAppearance();
        Player.cosmetics.ToggleNameVisible(true);
        
        // Restore layer
        Player.gameObject.layer = LayerMask.NameToLayer("Players");
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
