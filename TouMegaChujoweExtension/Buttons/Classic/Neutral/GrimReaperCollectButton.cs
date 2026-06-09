using System;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class GrimReaperCollectButton : TownOfUsRoleButton<GrimReaperRole, DeadBody>
{
    public override string Name => TouLocale.Get("ExtensionRoleGrimReaperCollect", "Collect");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction; // Use secondary action keybind (e.g. F / Left Trigger)
    public override Color TextOutlineColor => TouExtensionColors.GrimReaper;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<GrimReaperOptions>.Instance.ReapCooldown + MapCooldown, 0f, 120f);
    public override float EffectDuration => 0f;
    public override LoadableAsset<Sprite> Sprite => TownOfUs.Assets.TouNeutAssets.ReapSprite; // Reuse ReapSprite

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
    }

    public override float Distance => OptionGroupSingleton<GrimReaperOptions>.Instance.ReapRange;

    private static DeadBody[]? _allBodiesCache;
    private static float _lastCacheTime;

    public override DeadBody? GetTarget()
    {
        if (PlayerControl.LocalPlayer == null)
        {
            return null;
        }

        if (_allBodiesCache == null || Time.time - _lastCacheTime > 0.1f)
        {
            _allBodiesCache = UnityEngine.Object.FindObjectsOfType<DeadBody>();
            _lastCacheTime = Time.time;
        }

        var player = PlayerControl.LocalPlayer;
        DeadBody? closest = null;
        var closestDistance = float.MaxValue;
        var range = Distance;

        foreach (var body in _allBodiesCache)
        {
            if (body == null) continue;

            if (GrimReaperSystem.ActiveSouls.TryGetValue(body.ParentId, out var soul))
            {
                var distance = Vector2.Distance(player.GetTruePosition(), soul.Position);
                if (distance <= range && distance < closestDistance)
                {
                    closest = body;
                    closestDistance = distance;
                }
            }
        }

        return closest;
    }

    public override bool IsTargetValid(DeadBody? target)
    {
        if (target == null || PlayerControl.LocalPlayer == null)
        {
            return false;
        }

        if (!GrimReaperSystem.ActiveSouls.TryGetValue(target.ParentId, out var soul))
        {
            return false;
        }

        var player = PlayerControl.LocalPlayer;
        var distance = Vector2.Distance(player.GetTruePosition(), soul.Position);
        return distance <= Distance;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        if (playerControl == null || playerControl.Data == null) return;
        if (playerControl.Data.Role is not GrimReaperRole role) return;

        var options = OptionGroupSingleton<GrimReaperOptions>.Instance;
        var total = (int)options.SoulsToWin;
        var reaped = role.SoulsReaped;

        OverrideName($"{Name} ({reaped}/{total})");
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || Target == null)
        {
            return;
        }

        GrimReaperRole.RpcReapSoul(player, Target.ParentId);
        Timer = Cooldown;
    }
}
