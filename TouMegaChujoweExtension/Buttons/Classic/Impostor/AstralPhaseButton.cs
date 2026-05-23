using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using TownOfUs.Assets;
using TownOfUs.Networking;
using UnityEngine;
using System;
using System.Linq;
using TownOfUs.Modifiers;
using TouMegaChujoweExtension.Assets;

namespace TouMegaChujoweExtension.Buttons.Classic.Impostor;

public sealed class AstralPhaseButton : TownOfUsRoleButton<AstralRole>
{
    private Vector3 _startPosition;
    private Vector3 _defaultButtonLocalPos;
    private bool _hasCapturedButtonPos;
    private bool _isPhasing;

    public override string Name => _isPhasing
        ? TouLocale.Get("ExtensionRoleAstralMaterialize", "Materialize")
        : TouLocale.Get("ExtensionRoleAstralPhase", "Phase");

    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Astral;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<AstralOptions>.Instance.PhaseCooldown + MapCooldown, 5f, 120f);
    public override float EffectDuration => OptionGroupSingleton<AstralOptions>.Instance.PhaseDuration;
    public override LoadableAsset<Sprite> Sprite =>
        (EffectActive && Timer <= 3f && (int)(Timer * 8) % 2 == 0)
        ? TouCrewAssets.RewindingSprite
        : TouCrewAssets.RewindSprite;

    public override void ClickHandler()
    {
        if (!CanUse()) return;

        var player = PlayerControl.LocalPlayer;
        if (player == null) return;

        if (EffectActive)
        {
            Materialize(player);
        }
        else
        {
            _startPosition = player.transform.position;
            _isPhasing = true;

            if (Role != null) Role.KillMadeDuringPhase = false;

            Timer = EffectDuration;
            EffectActive = true;

            player.RpcAddModifier<AstralPhaseModifier>();

            OnClick();
        }
    }

    protected override void OnClick()
    {
        // Logic handled in ClickHandler
    }

    private static void ExitVentIfNeeded(PlayerControl player)
    {
        if (player != null && player.inVent)
        {
            var currentVent = Vent.currentVent;
            if (currentVent != null)
            {
                currentVent.SetButtons(false);
                player.MyPhysics.RpcExitVent(currentVent.Id);
            }
            player.MyPhysics.ExitAllVents();
        }
    }

    private void Materialize(PlayerControl player)
    {
        bool shouldDie = false;
        var options = OptionGroupSingleton<AstralOptions>.Instance;

        if (options.DieIfNoKillDuringPhase && Role != null && !Role.KillMadeDuringPhase && Timer < EffectDuration - 0.5f)
        {
            shouldDie = true;
        }

        _isPhasing = false;
        EffectActive = false;

        player.RpcRemoveModifier<AstralPhaseModifier>();

        if (shouldDie)
        {
            ExitVentIfNeeded(player);
            player.NetTransform.RpcSnapTo(_startPosition);
            player.RpcSpecialMurder(player, causeOfDeath: "AstralShatter");
            return;
        }

        ExitVentIfNeeded(player);
        player.NetTransform.RpcSnapTo(_startPosition);

        if (options.InvisibilityAfterTeleport)
        {
            player.RpcAddModifier<AstralInvisibilityModifier>();
        }
    }

    public override void OnEffectEnd()
    {
        if (!_isPhasing) return;

        var player = PlayerControl.LocalPlayer;
        if (player == null) return;

        var options = OptionGroupSingleton<AstralOptions>.Instance;
        bool shouldDie = false;

        bool killMade = Role != null && Role.KillMadeDuringPhase;

        if (!killMade && options.DieIfNoKillDuringPhase)
        {
            shouldDie = true;
        }

        if (shouldDie)
        {
            _isPhasing = false;
            EffectActive = false;
            player.RpcRemoveModifier<AstralPhaseModifier>();
            ExitVentIfNeeded(player);
            player.NetTransform.RpcSnapTo(_startPosition);
            player.RpcSpecialMurder(player, causeOfDeath: "AstralShatter");
        }
        else
        {
            Materialize(player);
        }
    }

    public override void FixedUpdateHandler(PlayerControl playerControl)
    {
        TimerPaused = ShouldPauseInVent && PlayerControl.LocalPlayer.inVent && !EffectActive;
        base.FixedUpdateHandler(playerControl);
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        if (Button == null) return;

        if (Button.graphic != null)
        {
            Button.graphic.color = Color.white;
            Button.graphic.material.SetFloat("_Desat", 0f);
        }

        if (Button.buttonLabelText != null)
        {
            Button.buttonLabelText.color = Color.white;
            Button.buttonLabelText.alpha = 1f;
        }

        if (EffectActive)
        {
            Button.cooldownTimerText.gameObject.SetActive(true);
        }

        OverrideName(_isPhasing
            ? TouLocale.Get("ExtensionRoleAstralMaterialize", "Materialize")
            : TouLocale.Get("ExtensionRoleAstralPhase", "Phase"));

        if (EffectActive && Timer <= 3f && Button.gameObject.activeInHierarchy)
        {
            if (!_hasCapturedButtonPos)
            {
                _defaultButtonLocalPos = Button.transform.localPosition;
                _hasCapturedButtonPos = true;
            }
            var urgency = Mathf.Clamp01((3f - Timer) / 3f);
            var amp = Mathf.Lerp(0.01f, 0.06f, urgency);
            var speed = Mathf.Lerp(18f, 35f, urgency);
            var nx = Mathf.PerlinNoise(Time.time * speed, 0.123f) - 0.5f;
            var ny = Mathf.PerlinNoise(0.456f, Time.time * speed) - 0.5f;
            Button.transform.localPosition = _defaultButtonLocalPos + new Vector3(nx * amp, ny * amp, 0f);
        }
        else if (_hasCapturedButtonPos)
        {
            Button.transform.localPosition = _defaultButtonLocalPos;
            _hasCapturedButtonPos = false;
        }
    }

    public override bool CanUse()
    {
        if (HudManager.Instance.Chat.IsOpenOrOpening || MeetingHud.Instance) return false;

        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data.IsDead) return false;

        if (player.GetModifiers<DisabledModifier>().Any(x => !x.CanUseAbilities)) return false;

        if (EffectActive)
        {
            return false;
        }

        return Timer <= 0;
    }
}