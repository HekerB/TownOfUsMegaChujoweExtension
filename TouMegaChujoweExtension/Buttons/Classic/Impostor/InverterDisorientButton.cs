using System.Collections;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TouMegaChujoweExtension.Modifiers.Impostor;
using MiraAPI.Roles;
using TownOfUs.Events;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modifiers;
using TownOfUs.Modules;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace TouMegaChujoweExtension.Buttons.Classic.Impostor;

public sealed class InverterDisorientButton : TownOfUsRoleButton<InverterRole>
{
    private bool isProcessingClick;
    private bool isMenuOpen;
    private bool _wasEffectActive;
    private Image? _cooldownFillImage;
    private Color? _originalCooldownColor;
    private ActionButton? _lastButton;
    private CustomPlayerMenu? _activeMenu;

    public override string Name => TouLocale.Get("ExtensionRoleInverterDisorient", "Disorient");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => Palette.ImpostorRed;
    public override float Cooldown => OptionGroupSingleton<InverterOptions>.Instance.DisorientCooldown;
    public override int MaxUses => (int)OptionGroupSingleton<InverterOptions>.Instance.MaxDisorients;
    public override bool ZeroIsInfinite { get; set; } = true;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.InverterDisorientButtonSprite;
    public override bool HasEffect => false;
    public override float EffectDuration => OptionGroupSingleton<InverterOptions>.Instance.DisorientDuration;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        UpdateUsesDisplay();

        if (Button?.usesRemainingSprite != null)
        {
            Button.usesRemainingSprite.sprite = TouAssets.AbilityCounterBasicSprite.LoadAsset();
            Button.usesRemainingSprite.color = TextOutlineColor;
        }
    }

    public override bool CanUse()
    {
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data.IsDead || PlayerControl.LocalPlayer.inVent)
        {
            return false;
        }

        if (isMenuOpen || Minigame.Instance || MeetingHud.Instance || HudManager.Instance.Chat.IsOpenOrOpening)
        {
            return false;
        }

        if (GetActiveDisorientedModifier(out _) != null)
        {
            return false;
        }

        return Timer <= 0f && (!LimitedUses || UsesLeft > 0);
    }

    public override void ClickHandler()
    {
        if (isProcessingClick || isMenuOpen)
        {
            return;
        }

        isProcessingClick = true;
        try
        {
            if (!CanUse())
            {
                return;
            }

            if (PlayerControl.LocalPlayer.GetModifiers<DisabledModifier>().Any(x => !x.CanUseAbilities))
            {
                return;
            }

            OnClick();
        }
        finally
        {
            Reactor.Utilities.Coroutines.Start(ResetProcessingFlag());
        }
    }

    private void CloseMenu()
    {
        if (_activeMenu != null)
        {
            try
            {
                _activeMenu.ForceClose();
            }
            catch { /* ignore */ }
            _activeMenu = null;
        }
        isMenuOpen = false;
    }

    protected override void OnClick()
    {
        if (Minigame.Instance)
        {
            return;
        }

        isMenuOpen = true;
        var playerMenu = CustomPlayerMenu.Create();
        _activeMenu = playerMenu;
        playerMenu.Begin(
            player => player != null
                && !player.HasDied()
                && player.PlayerId != PlayerControl.LocalPlayer.PlayerId
                && !player.IsImpostorAligned()
                && (OptionGroupSingleton<InverterOptions>.Instance.DisorientSamePersonTwice || Role.LastDisorientedPlayerId != player.PlayerId),
            player =>
            {
                _activeMenu = null;
                isMenuOpen = false;
                playerMenu.ForceClose();
                if (player == null)
                {
                    Timer = 0.01f;
                    return;
                }

                if (LimitedUses)
                {
                    UsesLeft--;
                    Button?.SetUsesRemaining(UsesLeft);
                }

                InverterRole.RpcDisorient(PlayerControl.LocalPlayer, player);
                Timer = Cooldown;
            });
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        var shouldShow = Role != null && !playerControl.HasDied();
        if (Button != null && Button.gameObject.activeSelf != shouldShow)
        {
            Button.gameObject.SetActive(shouldShow);
        }

        if (shouldShow)
        {
            if (isMenuOpen && playerControl != null && playerControl.inVent)
            {
                CloseMenu();
            }

            if (Minigame.Instance is not CustomPlayerMenu)
            {
                isMenuOpen = false;
            }

            UpdateUsesDisplay();

            try
            {
                if (_lastButton != Button && Button != null)
                {
                    _lastButton = Button;
                    _cooldownFillImage = Button.gameObject.transform.Find("CooldownFill")?.GetComponent<Image>();
                    if (_cooldownFillImage != null)
                    {
                        _originalCooldownColor = _cooldownFillImage.color;
                    }
                }
            }
            catch { /* ignore */ }

            var activeMod = GetActiveDisorientedModifier(out var victim);
            if (activeMod != null)
            {
                if (!EffectActive)
                {
                    EffectActive = true;
                    OverrideName(TouLocale.Get("ExtensionModifierDisoriented", "Disoriented"));
                }
                
                // Keep the button technically enabled by setting Timer to -1f,
                // matching the Injector design. This prevents desaturation / disabled label text.
                Timer = -1f;

                if (Button != null)
                {
                    Button.SetEnabled();
                    Button.SetFillUp(activeMod.TimeRemaining, EffectDuration);
                    
                    var time = activeMod.TimeRemaining;
                    var format = time <= 10f && MiraAPI.LocalSettings.LocalSettingsTabSingleton<TownOfUs.TownOfUsLocalSettings>.Instance.PreciseCooldownsToggle.Value
                        ? "0.0"
                        : "0";
                    Button.cooldownTimerText.text = time.ToString(format, System.Globalization.NumberFormatInfo.InvariantInfo);
                    
                    Button.cooldownTimerText.gameObject.SetActive(true);
                    Button.cooldownTimerText.color = Color.white;

                    if (_cooldownFillImage != null)
                    {
                        // Semi-transparent red fill (alpha = 0.4) for beautiful active state
                        _cooldownFillImage.color = new Color(Palette.ImpostorRed.r, Palette.ImpostorRed.g, Palette.ImpostorRed.b, 0.4f);
                    }
                    if (Button.graphic != null)
                    {
                        Button.graphic.color = Color.white;
                        if (Button.graphic.material != null)
                            Button.graphic.material.SetFloat("_Desat", 0f);
                    }
                }

                if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data?.Role is InverterRole inverter)
                {
                    var panel = TownOfUsEventHandlers.TryGetRoleTab();
                    if (panel != null && panel.taskText != null)
                    {
                        panel.SetTaskText(((ICustomRole)inverter).SetTabText().ToString());
                    }
                }
                return;
            }
            else
            {
                if (EffectActive)
                {
                    EffectActive = false;
                    OverrideName(TouLocale.Get("ExtensionRoleInverterDisorient", "Disorient"));
                    Timer = Cooldown;
                    if (Button != null)
                    {
                        Button.SetCooldownFormat(Timer, Cooldown, CooldownTimerFormatString);
                        if (_cooldownFillImage != null && _originalCooldownColor.HasValue)
                        {
                            _cooldownFillImage.color = _originalCooldownColor.Value;
                        }
                    }
                }

                if (Timer > 0f && Button != null)
                {
                    Button.SetEnabled(); // Keep button fully colored (saturated) during cooldown!
                    
                    if (_cooldownFillImage != null)
                    {
                        // Semi-transparent red fill (alpha = 0.4) for beautiful cooldown state as well!
                        _cooldownFillImage.color = new Color(Palette.ImpostorRed.r, Palette.ImpostorRed.g, Palette.ImpostorRed.b, 0.4f);
                    }
                    if (Button.graphic != null)
                    {
                        Button.graphic.color = Color.white;
                        if (Button.graphic.material != null)
                            Button.graphic.material.SetFloat("_Desat", 0f);
                    }
                }
                else if (Timer <= 0f && Button != null)
                {
                    if (_cooldownFillImage != null && _originalCooldownColor.HasValue)
                    {
                        _cooldownFillImage.color = _originalCooldownColor.Value;
                    }
                }
            }

            base.FixedUpdate(playerControl);
        }
    }

    private InverterDisorientedModifier? GetActiveDisorientedModifier(out PlayerControl? victim)
    {
        victim = null;
        if (Role == null) return null;
        var lastId = Role.LastDisorientedPlayerId;
        if (lastId == byte.MaxValue) return null;

        var target = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.PlayerId == lastId);
        if (target != null && target.TryGetModifier<InverterDisorientedModifier>(out var mod))
        {
            victim = target;
            return mod;
        }
        return null;
    }

    public override void SetUses(int amount)
    {
        base.SetUses(amount);
        UpdateUsesDisplay();
    }

    private void UpdateUsesDisplay()
    {
        if (Button == null)
        {
            return;
        }

        var showUses = MaxUses > 0;
        if (Button.usesRemainingSprite != null)
        {
            Button.usesRemainingSprite.gameObject.SetActive(showUses);
        }

        if (Button.usesRemainingText != null)
        {
            Button.usesRemainingText.gameObject.SetActive(showUses);
        }
    }

    private IEnumerator ResetProcessingFlag()
    {
        yield return new WaitForSeconds(0.2f);
        isProcessingClick = false;
    }
}
