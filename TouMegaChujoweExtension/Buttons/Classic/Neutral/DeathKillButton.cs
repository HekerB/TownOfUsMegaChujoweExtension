using System;
using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class DeathKillButton : TownOfUsRoleButton<DeathRole, PlayerControl>, IDiseaseableButton, IKillButton
{
    public override string Name => TouLocale.Get("ExtensionRoleSoulCollectorReap", "Reap");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Death;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<SoulCollectorOptions>.Instance.DeathKillCooldown + MapCooldown, 0f, 120f);
    public override float EffectDuration => 0f;
    public override LoadableAsset<Sprite> Sprite => TouNeutAssets.ReapSprite;
    public override float Distance => GameManager.Instance.LogicOptions.GetKillDistance();

    public void SetDiseasedTimer(float multiplier)
    {
        SetTimer(Cooldown * multiplier);
    }

    public override PlayerControl? GetTarget()
    {
        return PlayerControl.LocalPlayer?.GetClosestLivingPlayer(true, Distance, predicate: IsTargetValid);
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        var local = PlayerControl.LocalPlayer;
        if (target == null || local == null || target == local || target.HasDied())
        {
            return false;
        }

        if (ApocalypseUtils.AreAllied(local, target))
        {
            return false;
        }

        var distance = Vector2.Distance(local.GetTruePosition(), target.GetTruePosition());
        return distance <= Distance;
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || Target == null)
        {
            return;
        }

        if (PoisonSystem.CheckAndTriggerShields(player, Target))
        {
            Timer = Cooldown;
            return;
        }

        DeathRole.RpcDeathKill(player, Target);
        DeathRole.RpcMarkDeathBody(player, Target.PlayerId);
        Timer = Cooldown;
    }
}
