using System.Collections;
using System.Linq;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Modules.Components;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class JokerPlaceCloneButton : TownOfUsRoleButton<JokerRole>
{
    private enum Stage
    {
        Select,
        Preview,
        ActiveLocked,
        ActiveFull
    }

    private const float PostPlaceLockSeconds = 3f;

    private Stage _stage = Stage.Select;
    private int _previewCloneIndex = -1;
    private bool _isProcessingClick;
    private float _removeUnlockAt;
    private bool _isShaking;

    public static JokerPlaceCloneButton? LocalInstance { get; private set; }

    public override string Name => TouLocale.Get("ExtensionRoleJokerPlaceClone", "Place Clone");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Joker;
    public override float Cooldown => OptionGroupSingleton<JokerOptions>.Instance.CloneCooldown + MapCooldown;
    public override int MaxUses => (int)OptionGroupSingleton<JokerOptions>.Instance.MaxClones;
    public override LoadableAsset<Sprite> Sprite => TouExtensionNeuAssets.JokerCloneButtonSprite;

    public override bool CanUse()
    {
        if (MeetingHud.Instance || HudManager.Instance.Chat.IsOpenOrOpening)
        {
            return false;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null || player.HasDied())
        {
            return false;
        }

        return _stage switch
        {
            Stage.Select => Timer <= 0f && HasCloneSpace(player.PlayerId),
            Stage.Preview => true,
            Stage.ActiveLocked => Time.time >= _removeUnlockAt,
            Stage.ActiveFull => true,
            _ => false
        };
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        var local = PlayerControl.LocalPlayer;
        if (local == null)
        {
            return;
        }

        LocalInstance = this;

        var activeIndex = FindMyActiveCloneIndex(local.PlayerId);
        var previewIndex = FindMyPreviewCloneIndex(local.PlayerId);
        var activeCount = JokerCloneSystem.GetActiveCloneCountForJoker(local.PlayerId);
        var maxClones = (int)OptionGroupSingleton<JokerOptions>.Instance.MaxClones;
        var hasActive = activeIndex >= 0;
        var hasPreview = previewIndex >= 0;
        UpdateUsesRemaining(local.PlayerId);

        if (hasPreview)
        {
            _stage = Stage.Preview;
            _previewCloneIndex = previewIndex;
            Timer = 0f;
            OverrideName(TouLocale.Get("Confirm", "Confirm"));
            return;
        }

        if (hasActive && Time.time < _removeUnlockAt)
        {
            _stage = Stage.ActiveLocked;
            OverrideName(TouLocale.Get("ExtensionRoleJokerPlaceClone", "Place Clone"));
            return;
        }

        if (activeCount >= maxClones)
        {
            _stage = Stage.ActiveFull;
            Timer = 0f;
            OverrideName(TouLocale.Get("Cancel", "Cancel"));
            return;
        }

        _stage = Stage.Select;
        _previewCloneIndex = -1;
        OverrideName(TouLocale.Get("ExtensionRoleJokerPlaceClone", "Place Clone"));
    }

    public override void ClickHandler()
    {
        if (_stage == Stage.ActiveLocked && Time.time < _removeUnlockAt)
        {
            DoShake();
            return;
        }

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
            else if (_stage == Stage.Select)
            {
                DoShake();
            }
        }
        finally
        {
            Coroutines.Start(ResetProcessingFlag());
        }
    }

    private IEnumerator ResetProcessingFlag()
    {
        yield return new WaitForSeconds(0.2f);
        _isProcessingClick = false;
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            return;
        }

        if (_stage == Stage.Select)
        {
            if (!HasCloneSpace(player.PlayerId) || IsNearWall(player.transform.position))
            {
                DoShake();
                return;
            }

            var menu = CustomPlayerMenu.Create();
            menu.Begin(
                IsLivingCloneAppearanceCandidate,
                selectedPlayer =>
                {
                    if (selectedPlayer != null)
                    {
                        _previewCloneIndex = JokerCloneSystem.PlaceClone(player.PlayerId, selectedPlayer, player.transform.position, true);
                        if (_previewCloneIndex >= 0)
                        {
                            _stage = Stage.Preview;
                            OverrideName(TouLocale.Get("Confirm", "Confirm"));
                        }
                    }

                    menu.Close();
                });
            return;
        }

        if (_stage == Stage.ActiveFull)
        {
            var index = FindLastMyActiveCloneIndex(player.PlayerId);
            if (index >= 0)
            {
                JokerRole.RpcJokerDestroyClone(player, (byte)index);
                Timer = Cooldown;
            }

            return;
        }

        if (_stage != Stage.Preview)
        {
            return;
        }

        if (_previewCloneIndex < 0 || _previewCloneIndex >= JokerCloneSystem.Clones.Count)
        {
            _previewCloneIndex = FindMyPreviewCloneIndex(player.PlayerId);
        }

        if (_previewCloneIndex < 0)
        {
            _stage = Stage.Select;
            OverrideName(TouLocale.Get("ExtensionRoleJokerPlaceClone", "Place Clone"));
            return;
        }

        var clone = JokerCloneSystem.Clones[_previewCloneIndex];
        var position = clone.Fake.Body!.transform.position;
        var appearancePlayerId = clone.AppearancePlayerId;

        JokerCloneSystem.TryRemoveClone(_previewCloneIndex, out _);
        _previewCloneIndex = -1;

        JokerRole.RpcJokerPlaceClone(player, appearancePlayerId, position.x, position.y, position.z);

        _removeUnlockAt = Time.time + PostPlaceLockSeconds;
        _stage = Stage.ActiveLocked;
        Timer = Cooldown;
        OverrideName(TouLocale.Get("ExtensionRoleJokerPlaceClone", "Place Clone"));
    }

    public void ResetStage()
    {
        if (_previewCloneIndex >= 0)
        {
            JokerCloneSystem.TryRemoveClone(_previewCloneIndex, out _);
            _previewCloneIndex = -1;
        }

        var local = PlayerControl.LocalPlayer;
        if (local != null)
        {
            for (var i = JokerCloneSystem.Clones.Count - 1; i >= 0; i--)
            {
                var clone = JokerCloneSystem.Clones[i];
                if (!clone.IsPreview || clone.Fake.Body == null)
                {
                    continue;
                }

                var control = clone.Fake.Body.GetComponent<JokerCloneControlComponent>();
                if (control != null && control.OwnerId == local.PlayerId)
                {
                    JokerCloneSystem.TryRemoveClone(i, out _);
                }
            }
        }

        _stage = Stage.Select;
        _removeUnlockAt = 0f;
        OverrideName(TouLocale.Get("ExtensionRoleJokerPlaceClone", "Place Clone"));
    }

    private static bool HasCloneSpace(byte ownerId)
    {
        return JokerCloneSystem.GetActiveCloneCountForJoker(ownerId) < (int)OptionGroupSingleton<JokerOptions>.Instance.MaxClones;
    }

    private static bool IsLivingCloneAppearanceCandidate(PlayerControl candidate)
    {
        return candidate != null &&
               !candidate.Data.Disconnected &&
               !candidate.HasDied();
    }

    private void UpdateUsesRemaining(byte ownerId)
    {
        var maxClones = (int)OptionGroupSingleton<JokerOptions>.Instance.MaxClones;
        var activeClones = JokerCloneSystem.GetActiveCloneCountForJoker(ownerId);
        UsesLeft = Mathf.Max(0, maxClones - activeClones);
        Button?.SetUsesRemaining(UsesLeft);
    }

    private static int FindMyPreviewCloneIndex(byte ownerId)
    {
        for (var i = 0; i < JokerCloneSystem.Clones.Count; i++)
        {
            var clone = JokerCloneSystem.Clones[i];
            var control = clone.Fake.Body?.GetComponent<JokerCloneControlComponent>();
            if (clone.IsPreview && control != null && control.OwnerId == ownerId)
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindMyActiveCloneIndex(byte ownerId)
    {
        for (var i = 0; i < JokerCloneSystem.Clones.Count; i++)
        {
            var clone = JokerCloneSystem.Clones[i];
            var control = clone.Fake.Body?.GetComponent<JokerCloneControlComponent>();
            if (!clone.IsPreview && control != null && control.OwnerId == ownerId)
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindLastMyActiveCloneIndex(byte ownerId)
    {
        for (var i = JokerCloneSystem.Clones.Count - 1; i >= 0; i--)
        {
            var clone = JokerCloneSystem.Clones[i];
            var control = clone.Fake.Body?.GetComponent<JokerCloneControlComponent>();
            if (!clone.IsPreview && control != null && control.OwnerId == ownerId)
            {
                return i;
            }
        }

        return -1;
    }

    private void DoShake()
    {
        if (!_isShaking && Button != null)
        {
            Coroutines.Start(CoShake());
        }
    }

    private IEnumerator CoShake()
    {
        _isShaking = true;
        var transform = Button.transform;
        var basePosition = transform.localPosition;
        const float duration = 0.14f;
        const float amplitude = 1.6f;
        var time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            var offset = Mathf.Sin(time * 70f) * amplitude;
            transform.localPosition = basePosition + new Vector3(offset, 0f, 0f);
            yield return null;
        }

        transform.localPosition = basePosition;
        _isShaking = false;
    }

    private static bool IsNearWall(Vector2 position)
    {
        var colliders = Physics2D.OverlapCircleAll(position, 0.25f, Constants.ShipAndAllObjectsMask);
        return colliders.Any(collider => collider != null && !collider.isTrigger);
    }
}
