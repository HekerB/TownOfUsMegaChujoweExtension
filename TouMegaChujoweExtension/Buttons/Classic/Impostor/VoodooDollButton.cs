using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Impostor;

public sealed class VoodooDollButton : TownOfUsRoleButton<VoodooMasterRole, PlayerControl>
{
    public override string Name => GetButtonName(Role);
    public override BaseKeybind Keybind => Keybinds.ModifierAction;
    public override Color TextOutlineColor => Palette.ImpostorRed;
    public override float Cooldown => OptionGroupSingleton<VoodooMasterOptions>.Instance.CurseCooldown;
    public override int MaxUses => Role?.GetMaxUses(Role.SelectedEffect) ?? (int)OptionGroupSingleton<VoodooMasterOptions>.Instance.MaxBlindCurses;
    public override bool ZeroIsInfinite { get; set; } = true;
    public override LoadableAsset<Sprite> Sprite => GetEffectSprite(Role?.SelectedEffect ?? VoodooEffect.Blindness);

    private bool _isProcessingClick;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        SetupUsesDisplay();
        UpdateUsesDisplay();
    }

    public override bool CanUse()
    {
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data.IsDead || Role == null)
        {
            return false;
        }

        if (Minigame.Instance || MeetingHud.Instance || HudManager.Instance.Chat.IsOpenOrOpening)
        {
            return false;
        }

        return base.CanUse() &&
               Target != null &&
               Timer <= 0f &&
               (Role.GetMaxUses(Role.SelectedEffect) == 0 || Role.GetUsesLeft(Role.SelectedEffect) > 0);
    }

    public override void ClickHandler()
    {
        if (_isProcessingClick)
        {
            return;
        }

        _isProcessingClick = true;

        try
        {
            if (CanUse())
            {
                OnClick();
            }
        }
        finally
        {
            Reactor.Utilities.Coroutines.Start(ResetProcessingFlag());
        }
    }

    private System.Collections.IEnumerator ResetProcessingFlag()
    {
        yield return new WaitForSeconds(0.2f);
        _isProcessingClick = false;
    }

    protected override void OnClick()
    {
        if (Role == null || Target == null)
        {
            return;
        }

        var effect = Role.SelectedEffect;
        if (!Role.TrySpendUse(effect))
        {
            Timer = 0.01f;
            UpdateUsesDisplay();
            return;
        }

        VoodooMasterRole.CastVoodooDoll(PlayerControl.LocalPlayer, Target, effect);
        Timer = Cooldown;
        UpdateUsesDisplay();
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        var shouldShow = Role != null && !playerControl.HasDied() && !MeetingHud.Instance;

        if (Button != null && Button.gameObject.activeSelf != shouldShow)
        {
            Button.gameObject.SetActive(shouldShow);
        }

        if (shouldShow)
        {
            base.FixedUpdate(playerControl);
            OverrideSprite(GetEffectSprite(Role!.SelectedEffect).LoadAsset());
            OverrideName(GetButtonName(Role));
            UpdateUsesDisplay();
            UpdateActiveEffectTimer(playerControl);
        }
    }

    private void SetupUsesDisplay()
    {
        if (Button?.usesRemainingSprite != null)
        {
            Button.usesRemainingSprite.sprite = TouAssets.AbilityCounterBasicSprite.LoadAsset();
            Button.usesRemainingSprite.color = TextOutlineColor;
        }

        if (Button?.usesRemainingText != null)
        {
            Button.usesRemainingText.color = Color.white;
        }
    }

    public void UpdateUsesDisplay()
    {
        if (Button == null || Role == null)
        {
            return;
        }

        var maxUses = Role.GetMaxUses(Role.SelectedEffect);
        UsesLeft = Role.GetUsesLeft(Role.SelectedEffect);
        Button.SetUsesRemaining(UsesLeft);

        if (Button.usesRemainingSprite != null)
        {
            Button.usesRemainingSprite.gameObject.SetActive(maxUses > 0);
            Button.usesRemainingSprite.color = TextOutlineColor;
        }

        if (Button.usesRemainingText != null)
        {
            Button.usesRemainingText.gameObject.SetActive(maxUses > 0);
            Button.usesRemainingText.color = Color.white;
        }
    }

    private static string GetButtonName(VoodooMasterRole? role)
    {
        if (role == null)
        {
            return TouLocale.Get("ExtensionRoleVoodooMasterCast", "Curse");
        }

        return TouLocale.Get($"ExtensionVoodooEffect{role.SelectedEffect}", role.SelectedEffect.ToString());
    }

    private void UpdateActiveEffectTimer(PlayerControl playerControl)
    {
        if (Button == null || Role == null)
        {
            return;
        }

        var timeRemaining = GetActiveEffectTimeRemaining(playerControl.PlayerId, Role.SelectedEffect);
        if (timeRemaining <= 0f)
        {
            return;
        }

        Button.SetFillUp(timeRemaining, GetEffectDuration(Role.SelectedEffect));

        if (Button.cooldownTimerText == null)
        {
            return;
        }

        var format = timeRemaining <= 10f && MiraAPI.LocalSettings.LocalSettingsTabSingleton<TownOfUs.TownOfUsLocalSettings>.Instance.PreciseCooldownsToggle.Value
            ? "0.0"
            : "0";
        Button.cooldownTimerText.text = timeRemaining.ToString(format, System.Globalization.NumberFormatInfo.InvariantInfo);
        Button.cooldownTimerText.gameObject.SetActive(true);
        Button.cooldownTimerText.color = Color.white;
    }

    private static float GetActiveEffectTimeRemaining(byte voodooMasterId, VoodooEffect effect)
    {
        foreach (var player in PlayerControl.AllPlayerControls.ToArray())
        {
            if (player == null)
            {
                continue;
            }

            if (effect == VoodooEffect.Blindness &&
                player.TryGetModifier<VoodooBlindModifier>(out var blind) &&
                blind.VoodooMaster != null &&
                blind.VoodooMaster.PlayerId == voodooMasterId)
            {
                return blind.TimeRemaining;
            }

            if (effect == VoodooEffect.Confuse &&
                player.TryGetModifier<VoodooConfusedModifier>(out var confuse) &&
                confuse.VoodooMaster != null &&
                confuse.VoodooMaster.PlayerId == voodooMasterId)
            {
                return confuse.TimeRemaining;
            }
        }

        return 0f;
    }

    private static float GetEffectDuration(VoodooEffect effect)
    {
        var options = OptionGroupSingleton<VoodooMasterOptions>.Instance;
        return effect switch
        {
            VoodooEffect.Confuse => options.ConfuseDuration,
            VoodooEffect.Mute => options.MuteDuration,
            _ => options.BlindDuration
        };
    }

    private static LoadableAsset<Sprite> GetEffectSprite(VoodooEffect effect)
    {
        return effect switch
        {
            VoodooEffect.Mute => TouImpAssets.BlackmailSprite,
            VoodooEffect.Confuse => TouImpAssets.HerbConfuseSprite,
            _ => TouImpAssets.BlindSprite
        };
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        return base.IsTargetValid(target) &&
               target != null &&
               !target.HasDied() &&
               target.PlayerId != PlayerControl.LocalPlayer.PlayerId;
    }

    public override PlayerControl? GetTarget()
    {
        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(true, Distance);
    }
}
