using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities;
using System.Collections;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMegaChujoweExtension.Buttons.Impostor;

public sealed class CharlatanConcealButton : TownOfUsRoleButton<CharlatanRole, DeadBody>
{
    private bool _isChanneling;

    public override string Name => TouLocale.GetParsed("ExtensionRoleCharlatanConceal", "Conceal");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Charlatan;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<CharlatanOptions>.Instance.ConcealCooldown + MapCooldown, 5f, 120f);
    public override float EffectDuration => OptionGroupSingleton<CharlatanOptions>.Instance.ConcealDelay;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.ConcealButtonSprite;
    public override float Distance => 2f;

    public override bool ZeroIsInfinite { get; set; } = true;

    public override int MaxUses => (int)OptionGroupSingleton<CharlatanOptions>.Instance.ConcealUses;

    private static DeadBody[]? _allBodiesCache;
    private static float _lastCacheTime;

    public override DeadBody? GetTarget()
    {
        if (PlayerControl.LocalPlayer == null)
        {
            return null;
        }

        if (_allBodiesCache == null || Time.time - _lastCacheTime > 0.2f)
        {
            _allBodiesCache = Object.FindObjectsOfType<DeadBody>();
            _lastCacheTime = Time.time;
        }

        var player = PlayerControl.LocalPlayer;
        DeadBody? closest = null;
        var closestDistance = float.MaxValue;

        foreach (var body in _allBodiesCache)
        {
            if (body == null) continue;
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

        if (UsesLeft <= 0 && LimitedUses)
        {
            return false;
        }

        if (CharlatanConcealSystem.IsBodyConcealed(target.ParentId))
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

        if (UsesLeft <= 0 && LimitedUses)
        {
            return false;
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

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || Target == null)
        {
            return;
        }

        _isChanneling = true;
        EffectActive = true;
        Timer = EffectDuration;
        Button?.SetDisabled();

        // Don't call RPC yet - wait for channeling to complete
        Coroutines.Start(CoChannelConceal(Target.ParentId));
    }

    private IEnumerator CoChannelConceal(byte bodyId)
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            _isChanneling = false;
            EffectActive = false;
            yield break;
        }

        var options = OptionGroupSingleton<CharlatanOptions>.Instance;
        var channelDuration = options.ConcealDelay;
        var elapsed = 0f;

        while (elapsed < channelDuration)
        {
            if (player.HasDied() || MeetingHud.Instance != null)
            {
                // Channel cancelled - don't conceal
                _isChanneling = false;
                EffectActive = false;
                yield break;
            }

            var body = Object.FindObjectsOfType<DeadBody>().FirstOrDefault(x => x.ParentId == bodyId);
            if (body == null)
            {
                // Body disappeared - don't conceal
                _isChanneling = false;
                EffectActive = false;
                yield break;
            }

            var distance = Vector2.Distance(player.GetTruePosition(), body.TruePosition);
            if (distance > Distance)
            {
                // Moved too far - cancel channel and don't conceal
                _isChanneling = false;
                EffectActive = false;
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        var finalBody = Object.FindObjectsOfType<DeadBody>().FirstOrDefault(x => x.ParentId == bodyId);
        if (finalBody != null && player != null && !player.HasDied())
        {
            var finalDistance = Vector2.Distance(player.GetTruePosition(), finalBody.TruePosition);
            if (finalDistance <= Distance)
            {
                CharlatanRole.RpcCharlatanConceal(player, bodyId);
            }
        }

        _isChanneling = false;
        EffectActive = false;

        if (UsesLeft > 0 && LimitedUses)
        {
            UsesLeft--;
            SetUses(UsesLeft);
        }

        if (OptionGroupSingleton<CharlatanOptions>.Instance.ResetKillConcealCooldownsTogether && player != null)
        {
            player.SetKillTimer(player.GetKillCooldown());
        }

        ResetCooldownAndOrEffect();
    }

    public override void OnEffectEnd()
    {
        base.OnEffectEnd();
        _isChanneling = false;
    }
}
