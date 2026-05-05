using System;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modifiers.Universal;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using MiraAPI.Keybinds;
using TownOfUs.Events;
using TownOfUs.Options;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;

namespace TouMegaChujoweExtension.Buttons.Neutral;

public sealed class BountyHunterKillButton : TownOfUsRoleButton<BountyHunterRole, PlayerControl>
{
    // // private static readonly BepInEx.Logging.ManualLogSource Log =
        // // BepInEx.Logging.Logger.CreateLogSource("BH-Button");

    public override string Name => TouLocale.Get("ExtensionRoleBountyHunterKill", "Hunt");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override float Distance => 1.5f;

    public override float Cooldown
    {
        get
        {
            var opts = OptionGroupSingleton<BountyHunterOptions>.Instance;
            float cd = opts.KillCooldown.Value;
            return Math.Clamp(cd + MapCooldown, 5f, 120f);
        }
    }

    public override float EffectDuration => 0f;
    public override LoadableAsset<Sprite> Sprite => new LoadableBundleAsset<Sprite>("OfficerShootButton", TouAssets.MainBundle);
    public override Color TextOutlineColor => TouExtensionColors.BountyHunter;

    public override int MaxUses =>
        Mathf.Max(1, (int)OptionGroupSingleton<BountyHunterOptions>.Instance.TargetsToKill.Value);

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);

        if (Button != null)
        {
            Button.usesRemainingSprite.sprite = TouAssets.AbilityCounterPlayerSprite.LoadAsset();
            Button.usesRemainingText.gameObject.SetActive(true);
            Button.usesRemainingSprite.gameObject.SetActive(true);
        }

        RefreshKillCounter();
    }

    private void RefreshKillCounter()
    {
        int remaining = Mathf.Clamp(MaxUses - BountyHunterSystem.KillsDone, 0, MaxUses);
        SetUses(remaining);
    }

    public override bool Enabled(RoleBehaviour? role)
    {
        var opts = OptionGroupSingleton<BountyHunterOptions>.Instance;
        if (!opts.CanKillInRoundOne && TownOfUs.Events.DeathEventHandlers.CurrentRound <= 1)
            return false;

        return base.Enabled(role) && !PlayerControl.LocalPlayer.Data.IsDead;
    }

    public override PlayerControl? GetTarget()
    {
        if (PlayerControl.LocalPlayer == null)
            return null;

        if (BountyHunterSystem.CurrentTarget == null)
            return null;

        if (BountyHunterSystem.CurrentTarget.Data == null ||
            BountyHunterSystem.CurrentTarget.Data.IsDead ||
            BountyHunterSystem.CurrentTarget.Data.Disconnected)
            return null;

        var closest = PlayerControl.LocalPlayer.GetClosestLivingPlayer(true, Distance);

        if (closest == null)
            return null;

        if (closest.PlayerId != BountyHunterSystem.CurrentTarget.PlayerId)
            return null;

        if (closest.TryGetModifier<ChildModifier>(out var child) && !child.IsAdult)
            return null;

        return closest;
    }

    protected override void FixedUpdate(PlayerControl rolePlayer)
    {
        base.FixedUpdate(rolePlayer);
        RefreshKillCounter();
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
            return;
        }

        OnClick();
    }

    protected override void OnClick()
    {
        if (Target == null || PlayerControl.LocalPlayer == null)
        {
            // Log.LogWarning("[BH-Button] OnClick: Target or LocalPlayer is null");
            return;
        }

        if (BountyHunterSystem.CurrentTarget == null)
        {
            // Log.LogWarning("[BH-Button] OnClick: CurrentTarget is null");
            return;
        }

        if (Target.PlayerId != BountyHunterSystem.CurrentTarget.PlayerId)
        {
            // Log.LogWarning("[BH-Button] OnClick: Target mismatch!");
            return;
        }

        BountyHunterSystem.LastTargetPlayerId = Target.PlayerId;

        // Log.LogWarning($"[BH-Button] OnClick: Killing {Target.Data.PlayerName} (PlayerId={Target.PlayerId})");

        PlayerControl.LocalPlayer.RpcCustomMurder(Target);

        RefreshKillCounter();
        ResetCooldownAndOrEffect();
    }
}
