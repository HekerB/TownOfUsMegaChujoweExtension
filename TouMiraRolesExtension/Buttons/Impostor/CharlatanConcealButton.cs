using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities;
using System.Collections;
using TouMiraRolesExtension.Assets;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Options.Roles.Impostor;
using TouMiraRolesExtension.Roles.Impostor;
using TownOfUs.Buttons;
using TownOfUs.Modifiers;
using TownOfUs.Modules;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMiraRolesExtension.Buttons.Impostor;

public sealed class CharlatanConcealButton : TownOfUsRoleButton<CharlatanRole, DeadBody>
{
    private bool _isChanneling;

    public override string Name => TouLocale.GetParsed("ExtensionRoleCharlatanConceal", "Conceal");
    public override BaseKeybind Keybind => Keybinds.TertiaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Charlatan;
    public override float Cooldown => 0.01f;
    public override float EffectDuration => OptionGroupSingleton<CharlatanOptions>.Instance.ConcealChannelDuration;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.ConcealButtonSprite;
    public override float Distance => 2f;

    public override bool ZeroIsInfinite { get; set; } = true;

    public override int MaxUses => (int)OptionGroupSingleton<CharlatanOptions>.Instance.ConcealUses;

    public override DeadBody? GetTarget()
    {
        if (PlayerControl.LocalPlayer == null)
        {
            return null;
        }

        var player = PlayerControl.LocalPlayer;
        var allBodies = Object.FindObjectsOfType<DeadBody>();
        DeadBody? closest = null;
        var closestDistance = float.MaxValue;

        foreach (var body in allBodies)
        {
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

        CharlatanRole.RpcCharlatanConceal(player, Target.ParentId);

        Coroutines.Start(CoChannelConceal(Target.ParentId));
    }

    private IEnumerator CoChannelConceal(byte bodyId)
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            yield break;
        }

        var options = OptionGroupSingleton<CharlatanOptions>.Instance;
        var channelDuration = options.ConcealChannelDuration;
        var elapsed = 0f;

        while (elapsed < channelDuration)
        {
            if (player.HasDied() || MeetingHud.Instance != null)
            {
                _isChanneling = false;
                EffectActive = false;
                yield break;
            }

            var body = Object.FindObjectsOfType<DeadBody>().FirstOrDefault(x => x.ParentId == bodyId);
            if (body == null)
            {
                _isChanneling = false;
                EffectActive = false;
                yield break;
            }

            var distance = Vector2.Distance(player.GetTruePosition(), body.TruePosition);
            if (distance > Distance)
            {
                _isChanneling = false;
                EffectActive = false;
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        _isChanneling = false;
        EffectActive = false;

        // Mark channel as complete - conceal will now persist
        CharlatanConcealSystem.MarkChannelComplete(bodyId);

        if (UsesLeft > 0 && LimitedUses)
        {
            UsesLeft--;
            SetUses(UsesLeft);
        }
    }

    public override void OnEffectEnd()
    {
        base.OnEffectEnd();
        _isChanneling = false;
    }
}

