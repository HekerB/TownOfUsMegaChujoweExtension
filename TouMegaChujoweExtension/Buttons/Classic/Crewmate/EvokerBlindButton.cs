using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using MiraAPI.Utilities;
using Reactor.Utilities;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using UnityEngine.UI;
using System;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Modules;

namespace TouMegaChujoweExtension.Buttons.Classic.Crewmate;

public sealed class EvokerBlindButton : TownOfUsRoleButton<EvokerRole>
{
    private Image? _cooldownFillImage;
    private ActionButton? _lastButton;

    public override string Name => TouLocale.Get("ExtensionRoleEvokerBlind", "Blind");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Evoker;
    public override LoadableAsset<Sprite> Sprite => TouExtensionCrewAssets.EvokerBlindButtonSprite;

    public override float Cooldown => Math.Clamp(OptionGroupSingleton<EvokerOptions>.Instance.BlindCooldown.Value + MapCooldown, 5f, 120f);
    public override float InitialCooldown => OptionGroupSingleton<EvokerOptions>.Instance.BlindCooldown.Value;

    public override float EffectDuration => OptionGroupSingleton<EvokerOptions>.Instance.BlindDuration.Value;

    public override bool CanUse()
    {
        if (EffectActive) return true;

        if (!base.CanUse()) return false;

        var player = PlayerControl.LocalPlayer;
        if (player == null || player.HasDied()) return false;

        return !EvokerSystem.IsBlindActive;
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null) return;

        var duration = OptionGroupSingleton<EvokerOptions>.Instance.BlindDuration.Value;
        EvokerRole.RpcEvokerBlind(player, duration);
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        if (Button == null) return;

        if (EffectActive && !EvokerSystem.IsBlindActive)
        {
            EffectActive = false;
            Timer = Cooldown;
        }

        try
        {
            if (_lastButton != Button)
            {
                _lastButton = Button;
                _cooldownFillImage = Button.gameObject.transform.Find("CooldownFill")?.GetComponent<Image>();
            }
        }
        catch { /* ignore */ }

        if (EffectActive)
        {
            // Force bright color (no desaturation) when effect is active
            if (Button.graphic != null)
            {
                Button.graphic.color = Color.white;
                Button.graphic.material.SetFloat("_Desat", 0f);
            }

            if (_cooldownFillImage != null)
            {
                _cooldownFillImage.color = TouExtensionColors.Evoker;
            }

            // Smooth blink during the last 3 seconds (2Hz)
            if (Timer <= 3f)
            {
                bool blink = Mathf.FloorToInt(Time.time * 2f) % 2 == 0;
                if (Button.cooldownTimerText != null)
                {
                    Button.cooldownTimerText.color = blink ? Color.red : Color.white;
                }
            }
            else
            {
                if (Button.cooldownTimerText != null)
                {
                    Button.cooldownTimerText.color = Color.white;
                }
            }
        }
        else
        {
            if (_cooldownFillImage != null)
            {
                _cooldownFillImage.color = Color.white;
            }
            if (Button.cooldownTimerText != null)
            {
                Button.cooldownTimerText.color = Color.white;
            }
        }
    }
}
