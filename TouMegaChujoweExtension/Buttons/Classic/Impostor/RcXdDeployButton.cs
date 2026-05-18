using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Interfaces;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Impostor;

public sealed class RcXdDeployButton : TownOfUsKillRoleButton<RcXdRole>, IDiseaseableButton
{
    private bool _driving;
    private float _deployGrace;
    private float _wallCheckTimer;
    private bool _isNearWall;

    private const float WallDetectRadius = 0.3f;

    public override string Name => TouLocale.Get("ExtensionRoleRcXdDeploy", "Deploy");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => Palette.ImpostorRed;
    public override float Cooldown => PlayerControl.LocalPlayer?.GetKillCooldown() ?? 25f;
    public override float EffectDuration => OptionGroupSingleton<RcXdOptions>.Instance.DriveTime;
    public override int MaxUses => (int)OptionGroupSingleton<RcXdOptions>.Instance.MaxDeploys;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.RcXdDeployButton;
    public override bool ZeroIsInfinite { get; set; } = true;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        if (Button != null)
        {
            Button.usesRemainingSprite.gameObject.SetActive(MaxUses > 0);
            if (Button.usesRemainingText != null)
            {
                Button.usesRemainingText.gameObject.SetActive(MaxUses > 0);
            }
        }
    }

    public void SetDiseasedTimer(float multiplier) => SetTimer(Cooldown * multiplier);

    public override bool CanUse()
    {
        if (_driving) return true;
        if (_isNearWall) return false;

        if (!OptionGroupSingleton<RcXdOptions>.Instance.CanUseInFirstRound &&
            TownOfUs.Events.DeathEventHandlers.CurrentRound <= 1)
        {
            return false;
        }

        return base.CanUse();
    }

    public override bool CanClick()
    {
        if (_driving && Role.ActiveCar != null && Role.ActiveCar.IsDriving)
        {
            if (_deployGrace > 0f) return false;
            return OptionGroupSingleton<RcXdOptions>.Instance.AllowEarlyDetonation;
        }

        if (_isNearWall) return false;

        if (PlayerControl.LocalPlayer != null &&
            (PlayerControl.LocalPlayer.inVent || PlayerControl.LocalPlayer.walkingToVent))
            return false;

        return base.CanClick();
    }

    public override void ClickHandler()
    {
        if (_driving && Role.ActiveCar != null && Role.ActiveCar.IsDriving)
        {
            if (_deployGrace > 0f) return;
            
            var opts = OptionGroupSingleton<RcXdOptions>.Instance;
            if (!opts.AllowEarlyDetonation) return;

            RcXdRole.RpcDetonateCar(PlayerControl.LocalPlayer);
            ResetButton();
            return;
        }

        if (!CanClick()) return;
        OnClick();
        Button?.SetDisabled();
    }

    protected override void OnClick()
    {
        if (PlayerControl.LocalPlayer.inVent || PlayerControl.LocalPlayer.walkingToVent)
            return;

        if (IsNearWall(PlayerControl.LocalPlayer.transform.position)) return;

        var pos = (Vector2)PlayerControl.LocalPlayer.transform.position;
		var clip = TouAudio.TrackerActivateSound.LoadAsset();
		Info($"[RC-XD] Deploy sound clip: {clip?.name ?? "NULL"}, ShouldPlaySfx: {Constants.ShouldPlaySfx()}");
		TouAudio.PlaySound(TouAudio.TrackerActivateSound, 0.8f);

        RcXdRole.RpcDeployCar(PlayerControl.LocalPlayer, pos);

        _driving = true;
        _deployGrace = 0.5f;
        OverrideSprite(TouExtensionImpAssets.RcXdDetonateButton.LoadAsset());
        OverrideName(TouLocale.Get("ExtensionRoleRcXdDetonate", "Detonate"));

        // Zablokuj kill na czas jazdy
        PlayerControl.LocalPlayer.killTimer = EffectDuration + 1f;

        EffectActive = true;
        Timer = EffectDuration;
    }

    public override void OnEffectEnd()
    {
        if (_driving && Role.ActiveCar != null && !Role.ActiveCar.IsDetonated)
            RcXdRole.RpcDetonateCar(PlayerControl.LocalPlayer);
        ResetButton();
    }

    private void ResetButton()
    {
        if (_driving && LimitedUses)
        {
            DecreaseUses();
        }
        _driving = false;
        _deployGrace = 0f;
        _isNearWall = false;
        EffectActive = false;
        OverrideSprite(Sprite.LoadAsset());
        OverrideName(TouLocale.Get("ExtensionRoleRcXdDeploy", "Deploy"));
        Timer = Cooldown;
        PlayerControl.LocalPlayer.SetKillTimer(PlayerControl.LocalPlayer.GetKillCooldown());
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        if (_deployGrace > 0f)
            _deployGrace -= Time.deltaTime;

        if (!_driving)
        {
            _wallCheckTimer -= Time.deltaTime;
            if (_wallCheckTimer <= 0f)
            {
                _wallCheckTimer = 0.15f;
                _isNearWall = PlayerControl.LocalPlayer != null &&
                              IsNearWall(PlayerControl.LocalPlayer.transform.position);
            }
        }
        else
        {
            _isNearWall = false;

            // Insta-detonate on meeting/report
            if (MeetingHud.Instance || ExileController.Instance)
            {
                OnEffectEnd();
                return;
            }
        }

        if (_driving && (Role.ActiveCar == null || Role.ActiveCar.IsDetonated))
            ResetButton();
    }

    /// <summary>
    /// Wywoływane przez RcXdKillSyncPatch gdy gracz użyje regularnego killa.
    /// </summary>
    public static void SetOwnCooldown()
    {
        var instance = CustomButtonSingleton<RcXdDeployButton>.Instance;
        if (instance != null)
        {
            instance.Timer = instance.Cooldown;
        }
    }

    private static bool IsNearWall(Vector2 pos)
    {
        var cols = Physics2D.OverlapCircleAll(pos, WallDetectRadius, Constants.ShipAndAllObjectsMask);
        foreach (var c in cols)
        {
            if (c != null && !c.isTrigger) return true;
        }
        return false;
    }
}















