using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Networking;
using MiraAPI.Events;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Options;
using TownOfUs.Utilities;
using TouMegaChujoweExtension.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Neutral;

public sealed class PelicanSwallowButton : TownOfUsRoleButton<PelicanRole, PlayerControl>
{
    public override string Name => TouLocale.GetParsed("ExtensionRolePelicanSwallow", "Swallow");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Pelican;
    public override float Cooldown => Math.Clamp(
        OptionGroupSingleton<PelicanOptions>.Instance.SwallowCooldown + MapCooldown, 5f, 120f);
    public override float EffectDuration => 0f;
    public override bool HasEffect => false;
    public override LoadableAsset<Sprite> Sprite => TouExtensionNeuAssets.PelicanSwallowButtonSprite;


    public override int MaxUses =>
        Mathf.Max(1, (int)OptionGroupSingleton<PelicanOptions>.Instance.MaxSwallowed);

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);

        if (Button != null)
        {
            Button.usesRemainingSprite.sprite = TouAssets.AbilityCounterBodySprite.LoadAsset();
            Button.usesRemainingText.gameObject.SetActive(true);
            Button.usesRemainingSprite.gameObject.SetActive(true);
        }

        RefreshUses();
    }

    private void RefreshUses()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null) return;

        int max = Mathf.Max(1, (int)OptionGroupSingleton<PelicanOptions>.Instance.MaxSwallowed);
        int currentCount = PelicanSystem.GetSwallowedByPelican(player.PlayerId).Count;
        int remaining = Mathf.Clamp(max - currentCount, 0, max);

        SetUses(remaining);
    }

    public override PlayerControl? GetTarget()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null) return null;

        PlayerControl? bestTarget = null;
        float bestDistance = float.MaxValue;
        var myPos = player.GetTruePosition();

        foreach (var other in PlayerControl.AllPlayerControls)
        {
            if (other == null) continue;
            if (other.PlayerId == player.PlayerId) continue;
            if (other.Data == null) continue;
            if (other.Data.Disconnected) continue;
            if (other.HasDied()) continue;
            if (PelicanSystem.IsSwallowed(other.PlayerId)) continue;

            float dist = Vector2.Distance(myPos, other.GetTruePosition());
            if (dist > Distance) continue;

            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestTarget = other;
            }
        }

        return bestTarget;
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (target == null || target.HasDied() || target.Data == null) return false;
        if (target.Data.Disconnected) return false;
        if (PelicanSystem.IsSwallowed(target.PlayerId)) return false;

        return base.IsTargetValid(target);
    }

    public override bool CanUse()
    {
        if (!base.CanUse()) return false;

        var player = PlayerControl.LocalPlayer;
        if (player == null) return false;

        var options = OptionGroupSingleton<PelicanOptions>.Instance;
        var currentCount = PelicanSystem.GetSwallowedByPelican(player.PlayerId).Count;
        return currentCount < (int)options.MaxSwallowed;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);
        RefreshUses();
    }

    public override void ClickHandler()
    {
        if (!CanClick()) return;
        if (Target == null) return;

        var player = PlayerControl.LocalPlayer;
        if (player == null) return;

        var beforeMurderEvent = new BeforeMurderEvent(player, Target, MeetingCheck.OutsideMeeting);
        MiraEventManager.InvokeEvent(beforeMurderEvent);
        
        if (beforeMurderEvent.IsCancelled)
        {
            Timer = OptionGroupSingleton<GeneralOptions>.Instance.TempSaveCdReset;
            RefreshUses();
            return;
        }

        if (player.AmOwner)
        {
            try
            {
                TouAudio.PlaySound(TouExtensionAudio.SwallowSound);
            }
            catch (System.Exception ex)
            {
                Logger<TouMegaChujoweExtensionPlugin>.Error($"[PelicanSwallow] Sound error: {ex.Message}");
            }
        }

        OnClick();
        Timer = Cooldown;
        RefreshUses();
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || Target == null) return;

        try
        {
            PelicanRole.RpcPelicanSwallow(player, Target.PlayerId);
        }
        catch (System.Exception ex)
        {
            Logger<TouMegaChujoweExtensionPlugin>.Error($"[PelicanSwallow] RPC error: {ex.Message}");
        }
    }
}