using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modifiers;
using TownOfUs.Modules;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Neutral;

public sealed class IcenbergFreezeButton : TownOfUsRoleButton<IcenbergRole>, IDiseaseableButton
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

    public void SetDiseasedTimer(float multiplier)
    {
        SetTimer(Cooldown * multiplier);
    }

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        if (Button != null)
        {
            var icon = Button.transform.FindChild("Icon");
            if (icon != null) icon.localScale = Vector3.one * 0.75f;
        }
    }

    public override bool CanUse()
    {
        if (HudManager.Instance.Chat.IsOpenOrOpening || MeetingHud.Instance) return false;
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data.IsDead) return false;

        if (player.GetModifiers<DisabledModifier>().Any(x => !x.CanUseAbilities)) return false;

        return Timer <= 0 && (MaxUses == 0 || UsesLeft > 0);
    }

    protected override void OnClick()
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null)
        {
            Error("Icenberg Freeze: LocalPlayer is null");
            return;
        }

        localPlayer.NetTransform?.Halt();

        var playerMenu = CustomPlayerMenu.Create();
        playerMenu.Begin(
            IsValidFreezeTarget,
            selected =>
            {
                playerMenu.ForceClose();

                if (selected == null)
                {
                    Timer = 0.01f;
                    return;
                }

                IcenbergRole.RpcFreeze(localPlayer, selected, OptionGroupSingleton<IcenbergOptions>.Instance.FreezeDuration);

                if (UsesLeft > 0)
                {
                    UsesLeft--;
                    Button?.SetUsesRemaining(UsesLeft);
                }

                SetTimer(Cooldown);
                Button?.SetDisabled();
            });
    }

    private static bool IsValidFreezeTarget(PlayerControl? target)
    {
        var localPlayer = PlayerControl.LocalPlayer;

        return target != null
               && localPlayer != null
               && target.Data != null
               && !target.Data.Disconnected
               && !target.HasDied()
               && target.PlayerId != localPlayer.PlayerId
               && !target.HasModifier<IcenbergFrozenModifier>()
               && !target.HasModifier<FirstDeadShield>();
    }
}
