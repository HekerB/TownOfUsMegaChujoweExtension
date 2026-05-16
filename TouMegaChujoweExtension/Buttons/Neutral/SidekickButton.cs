using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using UnityEngine;
using TownOfUs.Assets;

namespace TouMegaChujoweExtension.Buttons.Neutral;

public sealed class SidekickButton : TownOfUsButton
{
    public SidekickButton() : base() { }

    public override string Name => "Recruit Team";
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Jackal;
    public override LoadableAsset<Sprite> Sprite => TouExtensionIcons.SidekickModifierIcon;

    public override float Cooldown => 0f;
    public override bool Enabled(RoleBehaviour? role) => false;

    public override bool CanUse()
    {
        return false;
    }

    protected override void OnClick()
    {
        // Do nothing
    }
}
