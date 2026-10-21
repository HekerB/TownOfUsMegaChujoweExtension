using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Modifiers;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class IcenbergBlizzardButton : TownOfUsRoleButton<IcenbergRole>
{
    public override string Name => TouLocale.Get("ExtensionRoleIcenbergBlizzard", "Blizzard");
    public override BaseKeybind Keybind => Keybinds.TertiaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Icenberg;
    public override float Cooldown => OptionGroupSingleton<IcenbergOptions>.Instance.BlizzardCooldown;
    public override float EffectDuration => 0f;
    public override bool HasEffect => false;
    public override bool ZeroIsInfinite { get; set; } = true;
    public override int MaxUses => (int)OptionGroupSingleton<IcenbergOptions>.Instance.BlizzardUses;
    public override LoadableAsset<Sprite> Sprite => TouExtensionNeuAssets.BlizzardButtonSprite;

    public override bool CanUse()
    {
        var player = PlayerControl.LocalPlayer;
        return player != null
               && !player.Data.IsDead
               && !HudManager.Instance.Chat.IsOpenOrOpening
               && !MeetingHud.Instance
               && !player.GetModifiers<DisabledModifier>().Any(modifier => !modifier.CanUseAbilities)
               && Timer <= 0
               && (MaxUses == 0 || UsesLeft > 0);
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            return;
        }

        IcenbergRole.RpcBlizzard(player, OptionGroupSingleton<IcenbergOptions>.Instance.BlizzardDuration);
        if (UsesLeft > 0)
        {
            UsesLeft--;
            Button?.SetUsesRemaining(UsesLeft);
        }

        SetTimer(Cooldown);
    }
}
