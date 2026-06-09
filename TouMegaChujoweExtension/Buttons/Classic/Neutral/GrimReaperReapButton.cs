using System;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using TownOfUs.Extensions;
using MiraAPI.Modifiers;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class GrimReaperReapButton : TownOfUsRoleButton<GrimReaperRole, PlayerControl>
{
    public override string Name => TouLocale.Get("ExtensionRoleGrimReaperReap", "Reap");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.GrimReaper;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<GrimReaperOptions>.Instance.ReapCooldown + MapCooldown, 0f, 120f);
    public override float EffectDuration => 0f;
    public override LoadableAsset<Sprite> Sprite => TownOfUs.Assets.TouNeutAssets.ReapSprite;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
    }

    public override float Distance => OptionGroupSingleton<GrimReaperOptions>.Instance.ReapRange;

    public override PlayerControl? GetTarget()
    {
        if (PlayerControl.LocalPlayer == null)
        {
            return null;
        }
        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(false, Distance);
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (target == null || PlayerControl.LocalPlayer == null || target == PlayerControl.LocalPlayer)
        {
            return false;
        }

        if (target.HasModifier<GrimReaperMarkedModifier>())
        {
            return false;
        }

        var distance = Vector2.Distance(PlayerControl.LocalPlayer.GetTruePosition(), target.GetTruePosition());
        return distance <= Distance && !target.HasDied();
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || Target == null)
        {
            return;
        }

        GrimReaperRole.RpcMarkPlayer(player, Target);
        Timer = Cooldown;
    }
}
