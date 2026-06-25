using MiraAPI.GameOptions;
using TownOfUs.Modifiers;
using TownOfUs.Options;
using TownOfUs.Patches;
using TownOfUs.Utilities;
using TownOfUs.Utilities.Appearances;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

public sealed class DeathInvisibleModifier : ConcealedModifier, IVisualAppearance
{
    public override string ModifierName => "Death Invisible";
    public override float Duration => float.MaxValue;
    public override bool HideOnUi => true;
    public override bool AutoStart => true;
    public override bool VisibleToOthers => false;
    public bool VisualPriority => true;

    public VisualAppearance GetVisualAppearance()
    {
        var local = PlayerControl.LocalPlayer;
        var canSeeFaintly = Player.AmOwner ||
                            (local != null && local.DiedOtherRound() &&
                             OptionGroupSingleton<GeneralOptions>.Instance.TheDeadKnow);

        return new VisualAppearance(Player.GetDefaultModifiedAppearance(), TownOfUsAppearances.Swooper)
        {
            HatId = "hat_NoHat",
            SkinId = "skin_None",
            VisorId = "visor_EmptyVisor",
            PlayerName = string.Empty,
            PetId = "pet_EmptyPet",
            RendererColor = canSeeFaintly ? new Color(0f, 0f, 0f, 0.1f) : Color.clear,
            NameColor = Color.clear,
            ColorBlindTextColor = Color.clear,
            NameVisible = false
        };
    }

    public override void OnActivate()
    {
        Player.RawSetAppearance(this);
        Player.cosmetics.ToggleNameVisible(false);
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (VanillaSystemCheckPatches.ShroomSabotageSystem && VanillaSystemCheckPatches.ShroomSabotageSystem.IsActive)
        {
            Player.RawSetAppearance(this);
            Player.cosmetics.ToggleNameVisible(false);
        }
    }

    public override void OnDeactivate()
    {
        Player.ResetAppearance();
        Player.cosmetics.ToggleNameVisible(true);

        if (HudManagerPatches.CamouflageCommsEnabled)
        {
            Player.cosmetics.ToggleNameVisible(false);
        }
    }
}
