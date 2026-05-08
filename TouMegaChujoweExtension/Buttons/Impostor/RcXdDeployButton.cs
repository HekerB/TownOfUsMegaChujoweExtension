using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Buttons;
using TownOfUs.Interfaces;
using TownOfUs.Assets;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Impostor;

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
    public override float Cooldown => OptionGroupSingleton<RcXdOptions>.Instance.DeployCooldown;
    public override float EffectDuration => OptionGroupSingleton<RcXdOptions>.Instance.DriveTime;
    public override int MaxUses => (int)OptionGroupSingleton<RcXdOptions>.Instance.MaxDeploys;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.RcXdDeployButton;

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
            return true;
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
        if (_driving)
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
        PlayerControl.LocalPlayer.SetKillTimer(
            GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown);
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
