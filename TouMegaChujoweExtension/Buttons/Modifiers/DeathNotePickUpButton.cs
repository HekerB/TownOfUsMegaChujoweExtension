using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using System.Linq;
using TownOfUs.Buttons;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Modifiers;

public sealed class DeathNotePickUpButton : TownOfUsButton
{
    public override string Name => "Pick Up";
    public override float Cooldown => 0f;
    public override int MaxUses => -1;
    public override ButtonLocation Location => ButtonLocation.BottomLeft;
    public override LoadableAsset<Sprite> Sprite => TouExtensionModifierIcons.DeathNoteModifierIcon;
    public override Color TextOutlineColor => new Color32(42, 10, 42, 255);
    public override BaseKeybind Keybind => Keybinds.ModifierAction;

    public override bool Enabled(RoleBehaviour? role)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data == null || local.Data.IsDead)
            return false;

        if (!local.TryGetModifier<DeathNoteModifier>(out var dnMod))
            return false;

        if (dnMod.IsUsed)
            return false;

        return DeathNotePickupBehaviour.Instances.Count > 0;
    }

    public override bool CanUse()
    {
        if (!base.CanUse())
            return false;

        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data == null || local.Data.IsDead)
            return false;

        if (!local.TryGetModifier<DeathNoteModifier>(out var dnMod))
            return false;

        if (dnMod.IsUsed)
            return false;

        if (DeathNoteUIController.Instance != null)
            return false;

        return DeathNotePickupBehaviour.Instances.Any(p => p != null && p.IsInRange());
    }

    protected override void OnClick()
    {
        if (!CanUse())
            return;

        if (!PlayerControl.LocalPlayer.TryGetModifier<DeathNoteModifier>(out var dnMod))
            return;

        if (DeathNoteUIController.Instance != null)
            return;

        var uiObj = new GameObject("DeathNoteUI");
        var controller = uiObj.AddComponent<DeathNoteUIController>();
        controller.Initialize(dnMod);
    }
}














