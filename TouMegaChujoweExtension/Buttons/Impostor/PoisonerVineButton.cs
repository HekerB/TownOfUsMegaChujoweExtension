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

public sealed class PoisonerVineButton : TownOfUsRoleButton<PoisonerRole>
{
    public override string Name => TouLocale.GetParsed("ExtensionRolePoisonerVine", "Vine");
    public override BaseKeybind Keybind => Keybinds.TertiaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Poisoner;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<PoisonerOptions>.Instance.VineCooldown + MapCooldown, 5f, 120f);
    public override float EffectDuration => 0f;
    public override int MaxUses => 0;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.VineButtonSprite;
    public override bool ZeroIsInfinite { get; set; } = true;

    private PlayerControl? _closestInRange;
    private PlayerControl? _lastOutlined;

    // Countdown window
    private bool _isVining;
    private float _vineTimer;
    private float _vineDuration;

    // Shake
    private const float ShakeStartTime = 1.0f;
    private const float ShakeMaxIntensity = 0.1f;
    private Vector3 _buttonOriginalPos;
    private bool _hasOriginalPos;

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
            base.FixedUpdate(playerControl);
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
                Timer = 0f;

                var fill = Mathf.Clamp(_vineTimer / _vineDuration, 0f, 1f);
                Button?.SetCooldownFill(fill);

                if (Button != null)
                {
                    Button.cooldownTimerText.text = Mathf.CeilToInt(_vineTimer).ToString();
                    Button.cooldownTimerText.gameObject.SetActive(true);
                }

                ApplyShake();
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
            if (pc.Data.Role.IsImpostor) continue;

            var dist = Vector2.Distance(poisoner.transform.position, pc.transform.position);
            if (dist <= range && dist < minDist)
            {
                minDist = dist;
                closest = pc;
            }
        }

        return closest;
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

        if (_vineTimer > ShakeStartTime)
        {
            btnTransform.localPosition = _buttonOriginalPos;
            return;
        }

        var progress = 1f - Mathf.Clamp01(_vineTimer / ShakeStartTime);
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
        ResetShake();
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