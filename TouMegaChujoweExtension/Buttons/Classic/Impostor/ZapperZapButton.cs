using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using TownOfUs.Extensions;
using UnityEngine;
using TouMegaChujoweExtension.Assets;

namespace TouMegaChujoweExtension.Buttons.Impostor;

public sealed class ZapperZapButton : TownOfUsRoleButton<ZapperRole, PlayerControl>
{
    public override string Name => TouLocale.Get("ExtensionRoleZapperZap", "Zap");
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.ZapButtonIcon;
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override float Cooldown => OptionGroupSingleton<ZapperOptions>.Instance.ZapCooldown;

    public override bool IsTargetValid(PlayerControl? target)
    {
        return target != null && !target.AmOwner && !target.HasDied() && !target.IsImpostorAligned();
    }

    public override PlayerControl? GetTarget()
    {
        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(true, Distance);
    }

    protected override void OnClick()
    {
        if (Target != null && PlayerControl.LocalPlayer.Data.Role is ZapperRole)
        {
            ZapperRole.RpcZap(PlayerControl.LocalPlayer, Target);
            TouAudio.PlaySound(TouExtensionAudio.ElectricitySound);
        }
    }
}
