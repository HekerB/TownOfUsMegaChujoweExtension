using System.Collections;
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

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        Reactor.Utilities.Coroutines.Start(CoMoveWithDelay());
    }

    private IEnumerator CoMoveWithDelay()
    {
        yield return MiscUtils.CoMoveButtonIndex(this, true);
    }

    private PlayerControl? _closestTarget;
    private PlayerControl? _lastOutlined;

    private bool _isPoisoning;
    private float _poisonTimer;
    private float _poisonDuration;



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
                Timer = -1f;

                if (Button != null)
                {
                    Button.SetEnabled();
                    Button.SetFillUp(_poisonTimer, _poisonDuration);
                    Button.cooldownTimerText.text = Mathf.CeilToInt(_poisonTimer).ToString();
                    Button.cooldownTimerText.gameObject.SetActive(true);
                }
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