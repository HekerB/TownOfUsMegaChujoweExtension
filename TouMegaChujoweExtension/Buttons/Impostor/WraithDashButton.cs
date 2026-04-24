using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Impostor;

public sealed class WraithDashButton : TownOfUsRoleButton<WraithRole>
{
    public override string Name => TouLocale.GetParsed("ExtensionRoleWraithDash", "Dash");
    public override BaseKeybind Keybind => Keybinds.ModifierAction;
    public override Color TextOutlineColor => TouExtensionColors.Wraith;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<WraithOptions>.Instance.DashCooldown + MapCooldown, 10f, 60f);
    public override float EffectDuration => OptionGroupSingleton<WraithOptions>.Instance.DashDuration;
    public override LoadableAsset<Sprite> Sprite => TouImpAssets.SprintSprite;

    public override bool ZeroIsInfinite { get; set; } = true;

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.HasDied())
        {
            return;
        }

        TouAudio.PlaySound(TouExtensionAudio.WraithDashSound);
        player.AddModifier<WraithDashModifier>();
    }
}