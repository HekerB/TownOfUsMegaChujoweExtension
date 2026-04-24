using System;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Impostor;

public sealed class PoisonerPoisonButton : TownOfUsRoleButton<PoisonerRole>
{
    public override string Name => TouLocale.GetParsed("ExtensionRolePoisonerPoison", "Poison");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => Palette.ImpostorRed;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<PoisonerOptions>.Instance.PoisonCooldown + MapCooldown, 5f, 120f);
    public override float EffectDuration => 0f;
    public override bool HasEffect => false;
    public override int MaxUses => 0;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.PoisonButtonSprite;
    public override bool ZeroIsInfinite { get; set; } = true;

    private PlayerControl? _closestTarget;
    private PlayerControl? _lastOutlined;

    private bool _isPoisoning;
    private float _poisonTimer;
    private float _poisonDuration;

    private const float ShakeStartTime = 1.5f;
    private const float ShakeMaxIntensity = 0.08f;
    private Vector3 _buttonOriginalPos;
    private bool _hasOriginalPos;

    public override bool CanUse()
    {
        if (MeetingHud.Instance || HudManager.Instance.Chat.IsOpenOrOpening) return false;

        var player = PlayerControl.LocalPlayer;
        if (player == null || player.HasDied()) return false;
        if (player.inVent) return false;

        if (_isPoisoning) return true;

        if (PoisonSystem.HasActivePoison) return false;

        return _closestTarget != null;
    }

    public override bool CanClick()
    {
        if (_isPoisoning) return false;
        return CanUse() && Timer <= 0f;
    }

    public override void ClickHandler()
    {
        if (!CanClick()) return;

        var player = PlayerControl.LocalPlayer;
        if (player == null || _closestTarget == null) return;

        PoisonerRole.RpcPoisonTarget(player, _closestTarget.PlayerId);

        _poisonDuration = OptionGroupSingleton<PoisonerOptions>.Instance.PoisonDuration;
        _poisonTimer = _poisonDuration;
        _isPoisoning = true;

        player.killTimer = _poisonDuration + 1f;

        OverrideSprite(TouExtensionImpAssets.PoisonedButtonSprite.LoadAsset());
        OverrideName(TouLocale.GetParsed("ExtensionRolePoisonerPoisoning", "Poisoning..."));

        Button?.SetCooldownFill(1f);
    }

    protected override void OnClick()
    {
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        if (MeetingHud.Instance)
        {
            if (_isPoisoning) EndPoisonWindow();
            base.FixedUpdate(playerControl);
            return;
        }

        if (playerControl == null || !playerControl.IsRole<PoisonerRole>())
        {
            _closestTarget = null;
            ClearOutline();
            if (_isPoisoning) EndPoisonWindow();
            base.FixedUpdate(playerControl);
            return;
        }

        if (_isPoisoning)
        {
            _poisonTimer -= Time.fixedDeltaTime;

            if (_poisonTimer <= 0f)
            {
                EndPoisonWindow();

                Timer = Cooldown;
                playerControl.SetKillTimer(GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown);
                PoisonerVineButton.SetOwnCooldown();

                OverrideSprite(TouExtensionImpAssets.PoisonButtonSprite.LoadAsset());
                OverrideName(TouLocale.GetParsed("ExtensionRolePoisonerPoison", "Poison"));
            }
            else
            {
                Timer = 0f;

                var remaining = Mathf.Clamp(_poisonTimer / _poisonDuration, 0f, 1f);
                Button?.SetCooldownFill(remaining);

                if (Button != null)
                {
                    Button.cooldownTimerText.text = Mathf.CeilToInt(_poisonTimer).ToString();
                    Button.cooldownTimerText.gameObject.SetActive(true);
                }

                ApplyShake();
            }

            _closestTarget = null;
            ClearOutline();

            Button?.gameObject.SetActive(
                HudManager.Instance.UseButton.isActiveAndEnabled ||
                HudManager.Instance.PetButton.isActiveAndEnabled);

            return;
        }

        if (playerControl.inVent || PoisonSystem.HasActivePoison)
        {
            _closestTarget = null;
            ClearOutline();
            base.FixedUpdate(playerControl);
            return;
        }

        var killDist = GameManager.Instance.LogicOptions.GetKillDistance();
        _closestTarget = null;
        var minDist = float.MaxValue;

        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc == null || pc.Data.IsDead || pc.PlayerId == playerControl.PlayerId) continue;
            if (pc.Data.Role.IsImpostor) continue;

            var dist = Vector2.Distance(playerControl.transform.position, pc.transform.position);
            if (dist <= killDist && dist < minDist)
            {
                minDist = dist;
                _closestTarget = pc;
            }
        }

        UpdateOutline();
        base.FixedUpdate(playerControl);
    }

    private void ApplyShake()
    {
        if (Button == null) return;
        var btnTransform = Button.transform;

        if (!_hasOriginalPos)
        {
            _buttonOriginalPos = btnTransform.localPosition;
            _hasOriginalPos = true;
        }

        if (_poisonTimer > ShakeStartTime)
        {
            btnTransform.localPosition = _buttonOriginalPos;
            return;
        }

        var progress = 1f - Mathf.Clamp01(_poisonTimer / ShakeStartTime);
        var intensity = Mathf.Lerp(0f, ShakeMaxIntensity, progress);
        var offset = UnityEngine.Random.insideUnitCircle * intensity;
        btnTransform.localPosition = _buttonOriginalPos + new Vector3(offset.x, offset.y, 0f);
    }

    private void ResetShake()
    {
        if (_hasOriginalPos && Button != null)
            Button.transform.localPosition = _buttonOriginalPos;
        _hasOriginalPos = false;
    }

    private void UpdateOutline()
    {
        if (_lastOutlined != null && _lastOutlined != _closestTarget)
            _lastOutlined.cosmetics.SetOutline(false, new Il2CppSystem.Nullable<Color>());

        if (_closestTarget != null)
            _closestTarget.cosmetics.SetOutline(true, new Il2CppSystem.Nullable<Color>(Palette.ImpostorRed));

        _lastOutlined = _closestTarget;
    }

    private void ClearOutline()
    {
        if (_lastOutlined != null)
        {
            _lastOutlined.cosmetics.SetOutline(false, new Il2CppSystem.Nullable<Color>());
            _lastOutlined = null;
        }
    }

    private void EndPoisonWindow()
    {
        _isPoisoning = false;
        _poisonTimer = 0f;
        ResetShake();

        Button?.SetCooldownFill(0f);
        if (Button != null)
            Button.cooldownTimerText.gameObject.SetActive(false);
    }

    public override void ResetCooldownAndOrEffect()
    {
        EndPoisonWindow();
        OverrideSprite(TouExtensionImpAssets.PoisonButtonSprite.LoadAsset());
        OverrideName(TouLocale.GetParsed("ExtensionRolePoisonerPoison", "Poison"));
        base.ResetCooldownAndOrEffect();
    }

    public static void SetOwnCooldown()
    {
        var instance = CustomButtonSingleton<PoisonerPoisonButton>.Instance;
        if (instance != null)
            instance.Timer = instance.Cooldown;
    }

    public override void OnEffectEnd() { }
}