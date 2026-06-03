using System;
using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class SoulCollectorReapButton : TownOfUsRoleButton<SoulCollectorRole, PlayerControl>
{
    public override string Name => TouLocale.Get("ExtensionRoleSoulCollectorReap", "Reap");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.SoulCollector;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<SoulCollectorOptions>.Instance.ReapCooldown + MapCooldown, 0f, 120f);
    public override float EffectDuration => 0f;
    public override LoadableAsset<Sprite> Sprite => TouNeutAssets.ReapSprite;
    public override float Distance => GameManager.Instance.LogicOptions.GetKillDistance();

    public override PlayerControl? GetTarget()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null)
        {
            return null;
        }

        return local.GetClosestLivingPlayer(true, Distance, predicate: IsTargetValid);
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        var local = PlayerControl.LocalPlayer;
        if (target == null || local == null || target == local || target.HasDied())
        {
            return false;
        }

        if (target.TryGetModifier<SoulReapedModifier>(out var existing) &&
            existing.SoulCollectorId == local.PlayerId &&
            !existing.IsExpired())
        {
            return false;
        }

        if (SoulCollectorRole.GetActiveMarkCount(local.PlayerId) >= (int)OptionGroupSingleton<SoulCollectorOptions>.Instance.MaxMarks)
        {
            return false;
        }

        var distance = Vector2.Distance(local.GetTruePosition(), target.GetTruePosition());
        return distance <= Distance;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        if (!playerControl.AmOwner)
        {
            return;
        }

        var maxMarks = (int)OptionGroupSingleton<SoulCollectorOptions>.Instance.MaxMarks;
        var activeMarks = SoulCollectorRole.GetActiveMarkCount(playerControl.PlayerId);
        SetUses(Math.Max(0, maxMarks - activeMarks));
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || Target == null)
        {
            return;
        }

        SoulCollectorRole.RpcReapTarget(player, Target);
        Timer = Cooldown;
    }
}
