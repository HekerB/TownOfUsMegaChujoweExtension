using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Modifiers;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class IcenbergFreezeButton : TownOfUsRoleButton<IcenbergRole>
{
    public override string Name => TouLocale.Get("ExtensionRoleIcenbergFreeze", "Freeze");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Icenberg;
    public override float Cooldown => OptionGroupSingleton<IcenbergOptions>.Instance.FreezeCooldown;
    public override float EffectDuration => 0f;
    public override bool HasEffect => false;
    public override LoadableAsset<Sprite> Sprite => TouExtensionNeuAssets.FreezeButtonSprite;
    public override bool ZeroIsInfinite { get; set; } = true;
    public override int MaxUses => (int)OptionGroupSingleton<IcenbergOptions>.Instance.FreezeUses;

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
            Error("Icenberg Freeze: LocalPlayer is null");
            return;
        }

        player.NetTransform?.Halt();
        var menu = CustomPlayerMenu.Create();
        menu.Begin(IsValidTarget, selected =>
        {
            menu.ForceClose();
            if (selected == null)
            {
                Timer = 0.01f;
                return;
            }

            IcenbergRole.RpcFreeze(player, selected, OptionGroupSingleton<IcenbergOptions>.Instance.FreezeDuration);
            if (UsesLeft > 0)
            {
                UsesLeft--;
                Button?.SetUsesRemaining(UsesLeft);
            }

            SetTimer(Cooldown);
            Button?.SetDisabled();
        });
    }

    private static bool IsValidTarget(PlayerControl? target)
    {
        var local = PlayerControl.LocalPlayer;
        return target != null
               && local != null
               && target.Data != null
               && !target.Data.Disconnected
               && !target.Data.IsDead
               && !target.Data.Disconnected
               && target.PlayerId != local.PlayerId
               && !target.HasModifier<IcenbergFrozenModifier>()
               && !target.HasModifier<FirstDeadShield>();
    }
}
