using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using Object = UnityEngine.Object;
using Reactor.Utilities;
using System.Collections;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class VultureEatButton : TownOfUsRoleButton<VultureRole, DeadBody>
{
    private bool _isChanneling;

    public override string Name => TouLocale.GetParsed("ExtensionRoleVultureEat", "Eat");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Vulture;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<VultureOptions>.Instance.EatCooldown + MapCooldown, 5f, 120f);
    public override float EffectDuration => 0f;
    public override LoadableAsset<Sprite> Sprite => TouExtensionNeuAssets.VultureEatButtonSprite;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
    }
    public override float Distance => 1.5f;

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
            _allBodiesCache = Object.FindObjectsOfType<DeadBody>();
            _lastCacheTime = Time.time;
        }

        var player = PlayerControl.LocalPlayer;
        DeadBody? closest = null;
        var closestDistance = float.MaxValue;

        foreach (var body in _allBodiesCache)
        {
            if (body == null || VultureSystem.IsBodyEaten(body.ParentId))
            {
                continue;
            }

            var distance = Vector2.Distance(player.GetTruePosition(), body.TruePosition);
            if (distance <= Distance && distance < closestDistance)
            {
                closest = body;
                closestDistance = distance;
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

        if (VultureSystem.IsBodyEaten(target.ParentId))
        {
            return false;
        }

        var player = PlayerControl.LocalPlayer;
        var distance = Vector2.Distance(player.GetTruePosition(), target.TruePosition);
        return distance <= Distance;
    }

    public override bool CanUse()
    {
        if (!base.CanUse())
        {
            return false;
        }

        if (_isChanneling)
        {
            return true;
        }

        return true;
    }

    public override void ClickHandler()
    {
        if (!CanClick())
        {
            return;
        }

        if (Target == null)
        {
            return;
        }

        if (_isChanneling)
        {
            return;
        }

        OnClick();
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        if (playerControl == null || playerControl.Data == null) return;
        if (playerControl.Data.Role is not VultureRole role) return;

        var options = OptionGroupSingleton<VultureOptions>.Instance;
        var total = (int)options.BodiesToWin;
        var eaten = role.BodiesEaten;

        OverrideName($"{Name} ({eaten}/{total})");
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || Target == null)
        {
            return;
        }

        VultureRole.RpcVultureEat(player, Target.ParentId);
        Timer = Cooldown;
    }

    public override void OnEffectEnd()
    {
        base.OnEffectEnd();
        _isChanneling = false;
    }
}

public sealed class VultureScavengeButton : TownOfUsRoleButton<VultureRole>
{
    public override string Name => TouLocale.GetParsed("ExtensionRoleVultureScavenge", "Scavenge");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Vulture;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<VultureOptions>.Instance.ScavengeCooldown.Value + MapCooldown, 5f, 120f);
    public override float EffectDuration => OptionGroupSingleton<VultureOptions>.Instance.ScavengeDuration.Value;
    public override LoadableAsset<Sprite> Sprite => TouExtensionNeuAssets.VultureScavengeButtonSprite;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
    }

    public override bool ZeroIsInfinite { get; set; } = true;

    public override bool Enabled(RoleBehaviour? role)
    {
        return base.Enabled(role) && OptionGroupSingleton<VultureOptions>.Instance.ScavengeEnabled;
    }

    protected override void OnClick()
    {
        if (PlayerControl.LocalPlayer == null)
        {
            return;
        }

        VultureRole.RpcVultureScavenge(PlayerControl.LocalPlayer);
        ResetCooldownAndOrEffect();
    }
}















