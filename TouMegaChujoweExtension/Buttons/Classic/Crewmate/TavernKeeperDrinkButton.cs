using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using MiraAPI.Utilities;
using TownOfUs.Buttons;
using TownOfUs.Utilities;
using UnityEngine;
using UnityEngine.UI;
using MiraAPI.Modifiers;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Buttons.Classic.Crewmate;

public sealed class TavernKeeperDrinkButton : TownOfUsRoleButton<TavernKeeperRole, PlayerControl>
{
    private Image? _cooldownFillImage;
    private Color? _originalCooldownColor;
    private ActionButton? _lastButton;
    private bool _lastMeetingState;
    private bool _waitingForModifier;

    public override string Name => TouLocale.Get("ExtensionRoleTavernKeeperDrink", "Drink");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.TavernKeeper;
    public override int MaxUses => (int)OptionGroupSingleton<TavernKeeperOptions>.Instance.MaxUses;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<TavernKeeperOptions>.Instance.DrinkCooldown + MapCooldown, 5f, 120f);
    public override LoadableAsset<Sprite> Sprite => TouCrewAssets.CleanseSprite;
    public override bool ZeroIsInfinite { get; set; } = true;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        if (Button != null)
        {
            Button.usesRemainingSprite.sprite = TouAssets.AbilityCounterBasicSprite.LoadAsset();
            Button.usesRemainingSprite.gameObject.SetActive(MaxUses > 0);
        }
    }

    public override bool CanUse()
    {
        if (EffectActive || GetActiveRoleblockedModifier(out _) != null) return false;
        return base.CanUse() && Role != null;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        if (Button != null)
        {
            if (ZeroIsInfinite && MaxUses == 0)
            {
                Button.usesRemainingText.gameObject.SetActive(false);
                Button.usesRemainingSprite.gameObject.SetActive(false);
            }
            else
            {
                Button.usesRemainingText.gameObject.SetActive(true);
                Button.usesRemainingSprite.gameObject.SetActive(true);
            }
        }

        bool inMeeting = MeetingHud.Instance != null;
        if (_lastMeetingState && !inMeeting)
        {
            Timer = 10f;
        }
        _lastMeetingState = inMeeting;

        if (Role == null || playerControl.HasDied()) return;

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

        var activeMod = GetActiveRoleblockedModifier(out _);
        if (activeMod != null)
        {
            _waitingForModifier = false;
        }

        if (EffectActive && (_waitingForModifier || activeMod != null))
        {
            Timer = -1f;

            if (Button != null)
            {
                Button.SetEnabled();
                var duration = OptionGroupSingleton<TavernKeeperOptions>.Instance.RoleblockDuration;
                var timeRemaining = activeMod != null ? activeMod.TimeRemaining : duration;
                Button.SetFillUp(timeRemaining, duration);

                var format = timeRemaining <= 10f && MiraAPI.LocalSettings.LocalSettingsTabSingleton<TownOfUs.TownOfUsLocalSettings>.Instance.PreciseCooldownsToggle.Value
                    ? "0.0"
                    : "0";
                Button.cooldownTimerText.text = timeRemaining.ToString(format, System.Globalization.NumberFormatInfo.InvariantInfo);

                Button.cooldownTimerText.gameObject.SetActive(true);
                Button.cooldownTimerText.color = Color.white;

                if (_cooldownFillImage != null)
                {
                    _cooldownFillImage.color = new Color(TouExtensionColors.TavernKeeper.r, TouExtensionColors.TavernKeeper.g, TouExtensionColors.TavernKeeper.b, 0.4f);
                }
                if (Button.graphic != null)
                {
                    Button.graphic.color = Color.white;
                    Button.graphic.material?.SetFloat("_Desat", 0f);
                }
            }
        }
        else
        {
            if (EffectActive)
            {
                EffectActive = false;
                _waitingForModifier = false;
                OverrideName(Name);
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
                Button.SetEnabled();
                if (_cooldownFillImage != null)
                {
                    _cooldownFillImage.color = new Color(TouExtensionColors.TavernKeeper.r, TouExtensionColors.TavernKeeper.g, TouExtensionColors.TavernKeeper.b, 0.4f);
                }
                if (Button.graphic != null)
                {
                    Button.graphic.color = Color.white;
                    Button.graphic.material?.SetFloat("_Desat", 0f);
                }
            }
            else if (Timer <= 0f && Button != null && _cooldownFillImage != null && _originalCooldownColor.HasValue)
            {
                _cooldownFillImage.color = _originalCooldownColor.Value;
            }
        }
    }

    private RoleblockedModifier? GetActiveRoleblockedModifier(out PlayerControl? victim)
    {
        victim = null;
        if (Role == null) return null;
        var lastId = Role.LastRoleblockedPlayerId;
        if (lastId == byte.MaxValue) return null;

        var target = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.PlayerId == lastId);
        if (target != null && target.TryGetModifier<RoleblockedModifier>(out var mod))
        {
            victim = target;
            return mod;
        }
        return null;
    }

    public override PlayerControl? GetTarget()
    {
        if (PlayerControl.LocalPlayer == null) return null;
        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(true, Distance,
            predicate: x => x.PlayerId != PlayerControl.LocalPlayer.PlayerId);
    }

    public override void ClickHandler()
    {
        if (!CanClick()) return;
        if (Target == null) return;

        var targetImmune = Target.HasModifier<DrinkImmunityModifier>() ||
                           Target.HasModifier<DrunkModifier>() ||
                           Target.HasModifier<RoleblockedModifier>() ||
                           Target.IsRole<TavernKeeperRole>();

        OnClick();

        if (targetImmune)
        {
            Timer = Cooldown;
            _waitingForModifier = false;
            EffectActive = false;
        }
        else
        {
            Timer = -1f;
            _waitingForModifier = true;
            EffectActive = true;
            OverrideName("Roleblocked");
        }

        if (MaxUses > 0)
        {
            UsesLeft--;
            SetUses(UsesLeft);
        }
    }

    protected override void OnClick()
    {
        if (Target == null) return;
        try
        {
            TavernKeeperRole.RpcRoleblock(PlayerControl.LocalPlayer, Target);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public override void ResetCooldownAndOrEffect()
    {
        _waitingForModifier = false;
        EffectActive = false;
        OverrideName(Name);
        base.ResetCooldownAndOrEffect();
    }
}
