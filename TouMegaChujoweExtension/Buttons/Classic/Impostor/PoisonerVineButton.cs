using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using System.Collections;
using System;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Impostor;

public sealed class PoisonerVineButton : TownOfUsRoleButton<PoisonerRole>
{
    public override string Name => TouLocale.GetParsed("ExtensionRolePoisonerVine", "Vine");
    public override BaseKeybind Keybind => Keybinds.TertiaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Poisoner;
    public override float Cooldown
    {
        get
        {
            var baseKc = GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown;
            var multiplier = PlayerControl.LocalPlayer != null && baseKc > 0 
                ? PlayerControl.LocalPlayer.GetKillCooldown() / baseKc 
                : 1f;
            return Math.Clamp((OptionGroupSingleton<PoisonerOptions>.Instance.VineCooldown + MapCooldown) * multiplier, 5f, 120f);
        }
    }
    public override float EffectDuration => 0f;
    public override int MaxUses => 0;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.VineButtonSprite;
    public override bool ZeroIsInfinite { get; set; } = true;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        Reactor.Utilities.Coroutines.Start(CoMoveWithDelay());
    }

    private IEnumerator CoMoveWithDelay()
    {
        yield return MiscUtils.CoMoveButtonIndex(this, false);
    }

    private PlayerControl? _closestInRange;
    private PlayerControl? _lastOutlined;

    // Countdown window
    private bool _isVining;
    private float _vineTimer;
    private float _vineDuration;



    public override bool CanUse()
    {
        if (MeetingHud.Instance || HudManager.Instance.Chat.IsOpenOrOpening) return false;
        if (PoisonSystem.IsVineActive) return false;
        if (PoisonSystem.HasActivePoison) return false;
        if (_isVining) return false;

        var player = PlayerControl.LocalPlayer;
        if (player == null || player.HasDied()) return false;
        if (player.inVent) return false;

        return _closestInRange != null;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        if (MeetingHud.Instance)
        {
            if (_isVining) EndVineWindow();
            base.FixedUpdate(playerControl);
            return;
        }

        if (playerControl == null || !playerControl.IsRole<PoisonerRole>())
        {
            _closestInRange = null;
            ClearOutline();
            if (_isVining) EndVineWindow();
            if (playerControl != null) base.FixedUpdate(playerControl);
            return;
        }

        // === Vine countdown window ===
        if (_isVining)
        {
            _vineTimer -= Time.fixedDeltaTime;

            if (_vineTimer <= 0f)
            {
                EndVineWindow();

                Timer = Cooldown;
                playerControl.SetKillTimer(GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown);
                PoisonerPoisonButton.SetOwnCooldown();

                OverrideSprite(TouExtensionImpAssets.VineButtonSprite.LoadAsset());
                OverrideName(TouLocale.GetParsed("ExtensionRolePoisonerVine", "Vine"));
            }
            else
            {
                Timer = -1f;

                if (Button != null)
                {
                    Button.SetEnabled();
                    Button.SetFillUp(_vineTimer, _vineDuration);
                    Button.cooldownTimerText.text = Mathf.CeilToInt(_vineTimer).ToString();
                    Button.cooldownTimerText.gameObject.SetActive(true);
                }
            }

            _closestInRange = null;
            ClearOutline();

            Button?.gameObject.SetActive(
                HudManager.Instance.UseButton.isActiveAndEnabled ||
                HudManager.Instance.PetButton.isActiveAndEnabled);
            return;
        }

        // === Normalny tryb ===
        if (PoisonSystem.HasActivePoison || PoisonSystem.IsVineActive || playerControl.inVent)
        {
            _closestInRange = null;
            ClearOutline();
            base.FixedUpdate(playerControl);
            return;
        }

        _closestInRange = FindClosestInRange(playerControl);
        UpdateOutline();
        base.FixedUpdate(playerControl);
    }

    private static PlayerControl? FindClosestInRange(PlayerControl poisoner)
    {
        var range = OptionGroupSingleton<PoisonerOptions>.Instance.VineRange;
        PlayerControl? closest = null;
        var minDist = float.MaxValue;

        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc == null || pc.Data.IsDead || pc.PlayerId == poisoner.PlayerId) continue;
            if (pc.IsImpostorAligned()) continue;

            var dist = Vector2.Distance(poisoner.transform.position, pc.transform.position);
            if (dist <= range && dist < minDist)
            {
                minDist = dist;
                closest = pc;
            }
        }

        return closest;
    }

    private void UpdateOutline()
    {
        if (_lastOutlined != null && _lastOutlined != _closestInRange)
        {
            _lastOutlined.cosmetics.SetOutline(false, new Il2CppSystem.Nullable<Color>());
        }

        if (_closestInRange != null)
        {
            _closestInRange.cosmetics.SetOutline(true,
                new Il2CppSystem.Nullable<Color>(new Color(0.1f, 0.6f, 0.1f)));
        }

        _lastOutlined = _closestInRange;
    }

    private void ClearOutline()
    {
        if (_lastOutlined != null)
        {
            _lastOutlined.cosmetics.SetOutline(false, new Il2CppSystem.Nullable<Color>());
            _lastOutlined = null;
        }
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || _closestInRange == null) return;

        PoisonerRole.RpcVineTarget(player, _closestInRange.PlayerId);

        _vineDuration = OptionGroupSingleton<PoisonerOptions>.Instance.VineDuration;
        _vineTimer = _vineDuration;
        _isVining = true;

        player.killTimer = _vineDuration + 1f;

        OverrideSprite(TouExtensionImpAssets.VineButtonSprite.LoadAsset());
        OverrideName(TouLocale.GetParsed("ExtensionRolePoisonerVining", "Vining..."));
    }

    private void EndVineWindow()
    {
        _isVining = false;
        _vineTimer = 0f;
    }

    public override void ResetCooldownAndOrEffect()
    {
        EndVineWindow();
        OverrideSprite(TouExtensionImpAssets.VineButtonSprite.LoadAsset());
        OverrideName(TouLocale.GetParsed("ExtensionRolePoisonerVine", "Vine"));
        base.ResetCooldownAndOrEffect();
    }

    public static void SetOwnCooldown()
    {
        var instance = CustomButtonSingleton<PoisonerVineButton>.Instance;
        if (instance != null)
        {
            instance.Timer = instance.Cooldown;
        }
    }

    public override void OnEffectEnd() { }
}
















