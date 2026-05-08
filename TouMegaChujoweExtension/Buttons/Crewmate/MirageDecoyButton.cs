using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Buttons;
using TownOfUs.Modules;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using MiraAPI.Hud;
using MiraAPI.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Crewmate;

public sealed class MirageDecoyButton : TownOfUsRoleButton<MirageRole>
{
    private enum Stage
    {
        Prime,
        Place,
        Destroy
    }

    private const float PostPlaceLockSeconds = 3f;

    public static MirageDecoyButton? LocalInstance { get; private set; }

    private Stage _stage = Stage.Prime;
    private byte? _primedAppearanceId;
    private Vector3 _primedWorldPos;
    private bool _primedFlipX;
    private float _primedZRot;
    private float _destroyUnlockAt;
    private bool _isProcessingClick;

    public override string Name => TouLocale.GetParsed("ExtensionRoleMirageDecoyPrime", "Prime");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Mirage;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<MirageOptions>.Instance.DecoyCooldown + MapCooldown, 5f, 120f);
    public override float EffectDuration => OptionGroupSingleton<MirageOptions>.Instance.DecoyDuration.Value;
    public override int MaxUses => (int)OptionGroupSingleton<MirageOptions>.Instance.InitialUses;
    public override LoadableAsset<Sprite> Sprite => TouExtensionCrewAssets.DecoyButtonSprite;
    public override bool ZeroIsInfinite { get; set; } = true;


    public override void ClickHandler()
    {
        if (_isProcessingClick)
        {
            return;
        }

        _isProcessingClick = true;

        try
        {
            if (!CanUse())
            {
                return;
            }

            OnClick();
        }
        finally
        {
            Coroutines.Start(ResetProcessingFlag());
        }
    }

    private System.Collections.IEnumerator ResetProcessingFlag()
    {
        yield return new WaitForSeconds(0.2f);
        _isProcessingClick = false;
    }

    public override bool CanUse()
    {
        if (TimeLordRewindSystem.IsRewinding || MeetingHud.Instance || HudManager.Instance.Chat.IsOpenOrOpening)
        {
            return false;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null || player.HasDied())
        {
            return false;
        }

        if (_stage == Stage.Destroy)
        {
            return Time.time >= _destroyUnlockAt;
        }

        return Timer <= 0f && (!LimitedUses || UsesLeft > 0);
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        var player = PlayerControl.LocalPlayer;
        if (player == null || !player.IsRole<MirageRole>())
        {
            return;
        }

        LocalInstance = this;
        
        if (ShipStatus.Instance == null)
        {
            Button?.gameObject.SetActive(false);
            return;
        }

        if (Button != null)
        {
            if (Button.usesRemainingSprite != null)
            {
                Button.usesRemainingSprite.gameObject.SetActive(MaxUses > 0);
            }
            if (Button.usesRemainingText != null)
            {
                Button.usesRemainingText.gameObject.SetActive(MaxUses > 0);
            }
        }
        var hasVisible = MirageDecoySystem.HasVisible(player.PlayerId);
        var hasAny = MirageDecoySystem.HasAny(player.PlayerId);

        if (hasVisible)
        {
            _stage = Stage.Destroy;

            if (!EffectActive && EffectDuration > 0f)
            {
                EffectActive = true;
                Timer = EffectDuration;
            }
        }
        else if (hasAny)
        {
            _stage = Stage.Place;
            EffectActive = false;
        }
        else if (_stage == Stage.Destroy || _stage == Stage.Place)
        {
            _stage = Stage.Prime;
            _primedAppearanceId = null;
            EffectActive = false;
        }

        switch (_stage)
        {
            case Stage.Prime:
                OverrideName(TouLocale.GetParsed("ExtensionRoleMirageDecoyPrime", "Prime"));
                break;
            case Stage.Place:
                OverrideName(TouLocale.GetParsed("ExtensionRoleMirageDecoyPlace", "Place"));
                break;
            case Stage.Destroy:
                OverrideName(TouLocale.GetParsed("ExtensionRoleMirageDecoyDestroy", "Destroy"));
                break;
        }

        if (_stage == Stage.Destroy &&
            hasVisible &&
            EffectDuration <= 0f &&
            Button != null)
        {
            var lockRemaining = _destroyUnlockAt - Time.time;
            if (lockRemaining > 0f)
            {
                try
                {
                    Button.SetFillUp(lockRemaining, PostPlaceLockSeconds);
                    Button.cooldownTimerText.text = Mathf.Ceil(lockRemaining)
                        .ToString(CooldownTimerFormatString, System.Globalization.NumberFormatInfo.InvariantInfo);
                    Button.cooldownTimerText.gameObject.SetActive(true);
                }
                catch
                {
                    // ignore
                }
            }
        }
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            return;
        }

        if (_stage == Stage.Destroy)
        {
            EffectActive = false;
            MirageRole.RpcMirageDestroyDecoy(player);
            return;
        }

        if (_stage == Stage.Prime)
        {
            _primedWorldPos = player.transform.position;
            OpenTablet();
            return;
        }

        var appearance = (_primedAppearanceId.HasValue ? MiscUtils.PlayerById(_primedAppearanceId.Value) : null) ?? player;

        if (LimitedUses)
        {
            if (UsesLeft <= 0)
            {
                return;
            }

            UsesLeft--;
            Button?.SetUsesRemaining(UsesLeft);
        }

        MirageRole.RpcMiragePlaceDecoy(
            player,
            appearance,
            new Vector2(_primedWorldPos.x, _primedWorldPos.y),
            _primedWorldPos.z,
            OptionGroupSingleton<MirageOptions>.Instance.DecoyDuration.Value,
            _primedZRot,
            _primedFlipX);
        _stage = Stage.Destroy;
        _destroyUnlockAt = Time.time + PostPlaceLockSeconds;
    }

    public void StartCooldownAndReset()
    {
        _stage = Stage.Prime;
        _primedAppearanceId = null;
        EffectActive = false;
        Timer = Cooldown;
        Button?.SetDisabled();
    }

    private void OpenTablet()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null) return;

        if (Minigame.Instance)
        {
            return;
        }

        var menu = CustomPlayerMenu.Create();
        menu.transform.FindChild("PhoneUI").GetChild(0).GetComponent<SpriteRenderer>().material =
            player.cosmetics.currentBodySprite.BodySprite.material;
        menu.transform.FindChild("PhoneUI").GetChild(1).GetComponent<SpriteRenderer>().material =
            player.cosmetics.currentBodySprite.BodySprite.material;

        menu.Begin(
            plr => ((!plr.Data.Disconnected && !plr.HasDied()) || Helpers.GetBodyById(plr.PlayerId)),
            plr =>
            {
                menu.ForceClose();

                if (plr == null)
                {
                    return;
                }

                _primedAppearanceId = plr.PlayerId;
                _primedFlipX = plr.cosmetics.currentBodySprite.BodySprite.flipX;
                _primedZRot = plr.transform.rotation.eulerAngles.z;
                _stage = Stage.Place;

                MirageRole.RpcMiragePrimeDecoy(
                    player,
                    plr,
                    new Vector2(_primedWorldPos.x, _primedWorldPos.y),
                    _primedWorldPos.z,
                    _primedZRot,
                    _primedFlipX);
            }
        );
        
        foreach (var panel in menu.potentialVictims)
        {
            panel.PlayerIcon.cosmetics.SetPhantomRoleAlpha(1f);
            if (panel.NameText.text != player.Data.PlayerName)
            {
                panel.NameText.color = Color.white;
            }
        }
    }



    public override void OnEffectEnd()
    {
        base.OnEffectEnd();

        var player = PlayerControl.LocalPlayer;
        if (player != null && player.IsRole<MirageRole>() && MirageDecoySystem.HasVisible(player.PlayerId))
        {
            MirageRole.RpcMirageDestroyDecoy(player);
        }
    }
}
