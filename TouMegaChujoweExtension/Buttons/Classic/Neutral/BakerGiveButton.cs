using System;
using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using System.Linq;
using TownOfUs.Extensions;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class BakerGiveButton : TownOfUsRoleButton<BakerRole, PlayerControl>
{
    public override string Name => TouLocale.Get("ExtensionRoleBakerGive", "Give");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Baker;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<BakerOptions>.Instance.GiveCooldown + MapCooldown, 0f, 120f);
    public override float EffectDuration => 0f;
    public override int MaxUses => (int)OptionGroupSingleton<BakerOptions>.Instance.BreadNeeded;
    public override bool ZeroIsInfinite => false;
    public override LoadableAsset<Sprite> Sprite => TownOfUs.Assets.TouNeutAssets.InfectSprite;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
    }

    public override float Distance => GameManager.Instance.LogicOptions.GetKillDistance();

    public override PlayerControl? GetTarget()
    {
        if (PlayerControl.LocalPlayer == null || Role.BreadGivenThisRound)
        {
            return null;
        }
        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(false, Distance);
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (target == null || PlayerControl.LocalPlayer == null || target == PlayerControl.LocalPlayer || Role.BreadGivenThisRound)
        {
            return false;
        }

        if (target.HasModifier<BakerBreadModifier>())
        {
            return false;
        }

        var distance = Vector2.Distance(PlayerControl.LocalPlayer.GetTruePosition(), target.GetTruePosition());
        return distance <= Distance && !target.HasDied();
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);
        if (playerControl.AmOwner)
        {
            var breadGivenCount = PlayerControl.AllPlayerControls.ToArray()
                .Count(x => x != null && !x.HasDied() && x.HasModifier<BakerBreadModifier>());
            var newUses = Math.Clamp(0, MaxUses - breadGivenCount, MaxUses);
            SetUses(newUses);
        }
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || Target == null)
        {
            return;
        }

        BakerRole.RpcGiveBread(player, Target);
        Timer = Cooldown;
    }
}
