using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using TownOfUs.Modifiers;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Neutral;

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
    public override LoadableAsset<Sprite> Sprite => TouMegaChujoweExtension.Assets.TouExtensionNeuAssets.BlizzardButtonSprite;

    public override bool CanUse()
    {
        if (HudManager.Instance.Chat.IsOpenOrOpening || MeetingHud.Instance) return false;
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data.IsDead) return false;

        if (player.GetModifiers<DisabledModifier>().Any(x => !x.CanUseAbilities)) return false;

        return Timer <= 0 && (MaxUses == 0 || UsesLeft > 0);
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

    protected override void OnClick()
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null)
        {
            return;
        }

        IcenbergRole.RpcBlizzard(localPlayer, OptionGroupSingleton<IcenbergOptions>.Instance.BlizzardDuration);
        SpendUse();
        SetTimer(Cooldown);
    }

    private void SpendUse()
    {
        if (UsesLeft > 0)
        {
            UsesLeft--;
        }
    }
}
