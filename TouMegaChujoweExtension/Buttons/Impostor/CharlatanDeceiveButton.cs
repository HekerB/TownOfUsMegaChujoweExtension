using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Roles;
using MiraAPI.Utilities.Assets;
using System.Globalization;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMegaChujoweExtension.Buttons.Impostor;

public sealed class CharlatanDeceiveButton : TownOfUsRoleButton<CharlatanRole, DeadBody>
{
    private Sprite? _defaultCounterSprite;
    private Vector3 _defaultCounterScale;
    private Vector3 _defaultCounterEuler;
    private Vector3 _defaultButtonLocalPos;
    private bool _hasCapturedButtonPos;

    public override string Name => TouLocale.GetParsed("ExtensionRoleCharlatanDeceive", "Deceive");
    public override BaseKeybind Keybind => Keybinds.TertiaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Charlatan;
    public override bool Enabled(RoleBehaviour? role) => base.Enabled(role) && OptionGroupSingleton<CharlatanOptions>.Instance.DeceiveEnabled;
    public override float Cooldown => 0.01f;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.DeceiveButtonSprite;
    public override float Distance => float.MaxValue;

    public override bool ZeroIsInfinite { get; set; } = true;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        _hasCapturedButtonPos = false;

        if (Button?.usesRemainingSprite != null)
        {
            _defaultCounterSprite = Button.usesRemainingSprite.sprite;
            _defaultCounterScale = Button.usesRemainingSprite.transform.localScale;
            _defaultCounterEuler = Button.usesRemainingSprite.transform.localEulerAngles;

            if (_defaultCounterScale == Vector3.zero)
            {
                _defaultCounterScale = Vector3.one;
            }
        }
    }

    private DeadBody[]? _allBodiesCache;
    private float _lastCacheTime;

    public override DeadBody? GetTarget()
    {
        if (PlayerControl.LocalPlayer == null)
        {
            return null;
        }

        var charlatan = PlayerControl.LocalPlayer;
        if (charlatan.Data?.Role is not CharlatanRole)
        {
            return null;
        }

        if (_allBodiesCache == null || Time.time - _lastCacheTime > 0.2f)
        {
            _allBodiesCache = Object.FindObjectsOfType<DeadBody>();
            _lastCacheTime = Time.time;
        }

        foreach (var body in _allBodiesCache)
        {
            if (body == null) continue;
            if (CharlatanDeceiveSystem.CanDeceiveReport(charlatan.PlayerId, body.ParentId))
            {
                return body;
            }
        }

        return null;
    }

    public override bool IsTargetValid(DeadBody? target)
    {
        if (target == null || PlayerControl.LocalPlayer == null)
        {
            return false;
        }

        var charlatan = PlayerControl.LocalPlayer;
        if (charlatan.Data?.Role is not CharlatanRole)
        {
            return false;
        }

        return CharlatanDeceiveSystem.CanDeceiveReport(charlatan.PlayerId, target.ParentId);
    }

    public override void ClickHandler()
    {
        if (!CanClick())
        {
            return;
        }

        if (Target == null)
        {
            return;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            return;
        }

        var bodyPlayer = MiscUtils.PlayerById(Target.ParentId);
        if (bodyPlayer != null)
        {
            player.CmdReportDeadBody(bodyPlayer.Data);
        }
        Button?.SetDisabled();
    }

    protected override void OnClick()
    {
        ClickHandler();
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        if (Button == null || Button.gameObject == null)
        {
            return;
        }

        var local = PlayerControl.LocalPlayer;
        if (local?.Data?.Role is not CharlatanRole)
        {
            ClearDeceiveTimerVisual();
            return;
        }

        var remainingTime = CharlatanDeceiveSystem.GetRemainingTime(local.PlayerId);
        if (remainingTime > 0f)
        {
            UpdateDeceiveTimerVisual(remainingTime);
        }
        else
        {
            ClearDeceiveTimerVisual();
        }
    }

    private void UpdateDeceiveTimerVisual(float remainingSeconds)
    {
        if (Button == null)
        {
            return;
        }

        if (Button.usesRemainingSprite != null)
        {
            Button.usesRemainingSprite.sprite = TouAssets.TimerImpSprite.LoadAsset();
            Button.usesRemainingSprite.gameObject.SetActive(true);

            var endUrgency = Mathf.Clamp01((5f - remainingSeconds) / 5f);
            var pulseAmp = Mathf.Lerp(0.003f, 0.012f, endUrgency);
            var pulseSpeed = Mathf.Lerp(1.5f, 3.0f, endUrgency);
            var pulse = 1f + pulseAmp * Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f);

            Button.usesRemainingSprite.transform.localScale = _defaultCounterScale * pulse;
            Button.usesRemainingSprite.transform.localEulerAngles = _defaultCounterEuler;
        }

        if (Button.usesRemainingText != null)
        {
            Button.usesRemainingText.text =
                Mathf.CeilToInt(remainingSeconds).ToString(CultureInfo.InvariantCulture);
            Button.usesRemainingText.gameObject.SetActive(true);
        }

        if (remainingSeconds <= 5f)
        {
            if (!_hasCapturedButtonPos)
            {
                _defaultButtonLocalPos = Button.transform.localPosition;
                _hasCapturedButtonPos = true;
            }

            var urgency = Mathf.Clamp01((5f - remainingSeconds) / 5f);
            var amp = Mathf.Lerp(0.01f, 0.06f, urgency);
            var speed = Mathf.Lerp(18f, 35f, urgency);
            var nx = Mathf.PerlinNoise(Time.time * speed, 0.123f) - 0.5f;
            var ny = Mathf.PerlinNoise(0.456f, Time.time * speed) - 0.5f;
            Button.transform.localPosition = _defaultButtonLocalPos + new Vector3(nx * amp, ny * amp, 0f);
        }
        else
        {
            _hasCapturedButtonPos = false;
        }
    }

    private void ClearDeceiveTimerVisual()
    {
        if (Button == null)
        {
            return;
        }

        if (_hasCapturedButtonPos)
        {
            Button.transform.localPosition = _defaultButtonLocalPos;
            _hasCapturedButtonPos = false;
        }

        if (Button.usesRemainingSprite != null)
        {
            if (_defaultCounterSprite != null)
            {
                Button.usesRemainingSprite.sprite = _defaultCounterSprite;
            }

            Button.usesRemainingSprite.transform.localScale = _defaultCounterScale;
            Button.usesRemainingSprite.transform.localEulerAngles = _defaultCounterEuler;
            Button.usesRemainingSprite.gameObject.SetActive(false);
        }

        Button.usesRemainingText?.gameObject.SetActive(false);
    }
}
