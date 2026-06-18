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
using TownOfUs.Extensions;
using TownOfUs.Modules;
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
    private bool _isTabletOpen;
    private float _removeUnlockAt;

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
            Stage.Select => !_isTabletOpen &&
                            !Minigame.Instance &&
                            Timer <= 0f &&
                            HasCloneSpace(player.PlayerId) &&
                            CanPlaceCloneAt(player, player.GetTruePosition()),
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

        if (_isTabletOpen && !Minigame.Instance)
        {
            _isTabletOpen = false;
        }

        var cloneSummary = JokerCloneSystem.GetCloneSummaryForJoker(local.PlayerId);
        var activeIndex = cloneSummary.FirstActiveIndex;
        var previewIndex = cloneSummary.PreviewIndex;
        var activeCount = cloneSummary.ActiveCount;
        var maxClones = (int)OptionGroupSingleton<JokerOptions>.Instance.MaxClones;
        var hasActive = activeIndex >= 0;
        var hasPreview = previewIndex >= 0;
        UpdateUsesRemaining(maxClones, activeCount);

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
        if (_isTabletOpen)
        {
            return;
        }

        if (_stage == Stage.ActiveLocked && Time.time < _removeUnlockAt)
        {
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
            if (!HasCloneSpace(player.PlayerId) || !CanPlaceCloneAt(player, player.GetTruePosition()))
            {
                return;
            }

            if (Minigame.Instance)
            {
                return;
            }

            _isTabletOpen = true;

            var menu = CustomPlayerMenu.Create();
            menu.Begin(
                IsLivingCloneAppearanceCandidate,
                selectedPlayer =>
                {
                    _isTabletOpen = false;
                    menu.ForceClose();

                    if (selectedPlayer != null &&
                        _stage == Stage.Select &&
                        HasCloneSpace(player.PlayerId) &&
                        CanPlaceCloneAt(player, player.GetTruePosition()) &&
                        FindMyPreviewCloneIndex(player.PlayerId) < 0)
                    {
                        var position = player.GetTruePosition();
                        _previewCloneIndex = JokerCloneSystem.PlaceClone(player.PlayerId, selectedPlayer, position, true);
                        if (_previewCloneIndex >= 0)
                        {
                            _stage = Stage.Preview;
                            OverrideName(TouLocale.Get("Confirm", "Confirm"));
                        }
                    }
                });
            return;
        }

        if (_stage == Stage.ActiveFull)
        {
            var index = JokerCloneSystem.GetCloneSummaryForJoker(player.PlayerId).LastActiveIndex;
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

                if (clone.JokerId == local.PlayerId)
                {
                    JokerCloneSystem.TryRemoveClone(i, out _);
                }
            }
        }

        _stage = Stage.Select;
        _isTabletOpen = false;
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

    private void UpdateUsesRemaining(int maxClones, int activeClones)
    {
        UsesLeft = Mathf.Max(0, maxClones - activeClones);
        Button?.SetUsesRemaining(UsesLeft);
    }

    private static int FindMyPreviewCloneIndex(byte ownerId)
    {
        for (var i = 0; i < JokerCloneSystem.Clones.Count; i++)
        {
            var clone = JokerCloneSystem.Clones[i];
            if (clone.IsPreview && clone.JokerId == ownerId)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool CanPlaceCloneAt(PlayerControl player, Vector2 position)
    {
        if (player.Collider == null)
        {
            return false;
        }

        var blocked = Physics2D
            .OverlapBoxAll(position, Vector2.one * 0.55f, 0f, Constants.ShipAndAllObjectsMask)
            .Any(collider =>
                collider != null &&
                collider.gameObject.layer != 8 &&
                collider.gameObject.layer != 5 &&
                collider != player.Collider &&
                !collider.transform.IsChildOf(player.transform) &&
                (collider.name.Contains("Vent") || collider.name.Contains("Door") || !collider.isTrigger));

        if (blocked)
        {
            return false;
        }

        return !PhysicsHelpers.AnythingBetween(
                   player.Collider,
                   player.Collider.bounds.center,
                   position,
                   Constants.ShipAndAllObjectsMask,
                   false) &&
               !ModCompatibility.GetPlayerElevator(player).Item1;
    }
}
