using HarmonyLib;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using Reactor.Utilities.Extensions;
using Reactor.Utilities;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using TMPro;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Utilities;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace TouMegaChujoweExtension.Patches.Roles.Pirate;

[HarmonyPatch]
public static class PirateDuelMeetingPatch
{
    private static GameObject? _duelButton;
    private static SpriteRenderer? _duelButtonRenderer;
    private static int _localChoice;
    private static bool _isLocalPirate;
    private static bool _isLocalTarget;
    private static byte _piratePlayerId;
    private static byte _duelTargetId;
    private static bool _buttonCreated;
    private static bool _duelActiveThisMeeting;
    private static bool _choiceLocked;
    private static Sprite?[]? _sprites;

    private static readonly Color PirateTargetColor = Color.yellow;
    private static readonly List<(TMP_Text Text, string OriginalText, Color OriginalColor)> _modifiedNames = new();



    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPostfix]
    public static void MeetingStartPostfix(MeetingHud __instance)
    {
        try
        {
            _localChoice = 0;
            _isLocalPirate = false;
            _isLocalTarget = false;
            _piratePlayerId = byte.MaxValue;
            _duelTargetId = byte.MaxValue;
            _buttonCreated = false;
            _duelActiveThisMeeting = false;
            _choiceLocked = false;
            RestoreMarkedNames();
            CleanupButton();

            if (PlayerControl.LocalPlayer == null || PlayerControl.AllPlayerControls == null)
            {
                return;
            }

            PirateRole? activePirate = null;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player?.Data?.Role is not PirateRole pirate)
                {
                    continue;
                }

                if (pirate.DuelTargetId == byte.MaxValue)
                {
                    continue;
                }

                var target = MiscUtils.PlayerById(pirate.DuelTargetId);
                if (target != null && !target.HasDied())
                {
                    activePirate = pirate;
                    break;
                }

                pirate.DuelTargetId = byte.MaxValue;
            }

            if (activePirate == null)
            {
                return;
            }

            var piratePlayer = activePirate.Player;
            var modifierComponent = piratePlayer.GetComponent<ModifierComponent>();
            if (modifierComponent != null)
            {
                foreach (var mod in modifierComponent.ActiveModifiers)
                {
                    if (mod is JailedModifier)
                    {
                        activePirate.DuelTargetId = byte.MaxValue;
                        activePirate.ResetDuelState();

                        if (piratePlayer.AmOwner)
                        {
                            PirateDuelSystem.FlashScreen(Color.gray, 0.5f, 0.3f);
                            Coroutines.Start(ShowJailedNotification());
                        }

                        return;
                    }
                }
            }

            var duelTarget = MiscUtils.PlayerById(activePirate.DuelTargetId);
            if (duelTarget != null)
            {
                var targetModComponent = duelTarget.GetComponent<ModifierComponent>();
                if (targetModComponent != null)
                {
                    foreach (var mod in targetModComponent.ActiveModifiers)
                    {
                        if (mod is JailedModifier)
                        {
                            activePirate.DuelTargetId = byte.MaxValue;
                            activePirate.ResetDuelState();

                            if (piratePlayer.AmOwner)
                            {
                                PirateDuelSystem.FlashScreen(Color.gray, 0.5f, 0.3f);
                                Coroutines.Start(ShowTargetJailedNotification());
                            }

                            return;
                        }
                    }
                }
            }

            activePirate.DuelActive = true;
            activePirate.DuelResolved = false;
            activePirate.PirateChoice = 0;
            activePirate.TargetChoice = 0;

            _piratePlayerId = activePirate.Player.PlayerId;
            _duelTargetId = activePirate.DuelTargetId;
            _duelActiveThisMeeting = true;

            var local = PlayerControl.LocalPlayer;
            _isLocalPirate = local.PlayerId == activePirate.Player.PlayerId;
            _isLocalTarget = local.PlayerId == activePirate.DuelTargetId;

            if (_isLocalTarget)
            {
                PirateDuelSystem.FlashScreen(TouExtensionColors.Pirate, 0.5f, 0.3f);
                Coroutines.Start(ShowDuelTargetNotification());
            }

            if (_isLocalPirate || _isLocalTarget)
            {
                LoadSprites();
            }
        }
        catch (Exception)
        {
            /* Error during meeting start duel initialization */
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    [HarmonyPostfix]
    public static void MeetingUpdatePostfix(MeetingHud __instance)
    {
        if (_duelActiveThisMeeting && _isLocalPirate && _duelTargetId != byte.MaxValue)
        {
            ApplyPirateTargetMark(__instance, _duelTargetId);
        }

        if (!_duelActiveThisMeeting || _buttonCreated) return;
        if (!_isLocalPirate && !_isLocalTarget) return;

        try
        {
            CreateDuelButton(__instance);
            _buttonCreated = true;
        }
        catch (Exception)
        {
            /* Error creating duel button */
            _buttonCreated = true;
        }
    }

    private static void ApplyPirateTargetMark(MeetingHud meetingHud, byte targetId)
    {
        foreach (var area in meetingHud.playerStates)
        {
            if (area == null || area.TargetPlayerId != targetId) continue;
            if (area.NameText == null) continue;

            var alreadyTracked = _modifiedNames.Any(x => x.Text == area.NameText);
            if (!alreadyTracked)
            {
                _modifiedNames.Add((area.NameText, area.NameText.text, area.NameText.color));
            }

            area.NameText.color = PirateTargetColor;

            if (!area.NameText.text.StartsWith("☠ "))
            {
                area.NameText.text = "☠ " + area.NameText.text;
            }

            break;
        }
    }

    private static void RestoreMarkedNames()
    {
        foreach (var entry in _modifiedNames)
        {
            if (entry.Text == null) continue;
            entry.Text.text = entry.OriginalText;
            entry.Text.color = entry.OriginalColor;
        }

        _modifiedNames.Clear();
    }

    private static void LoadSprites()
    {
        try
        {
            _sprites = _isLocalPirate
                ? new Sprite?[]
                {
                    TouExtensionAssets.PirateDuelAttack1.LoadAsset(),
                    TouExtensionAssets.PirateDuelAttack2.LoadAsset(),
                    TouExtensionAssets.PirateDuelAttack3.LoadAsset()
                }
                : new Sprite?[]
                {
                    TouExtensionAssets.PirateDuelDefend1.LoadAsset(),
                    TouExtensionAssets.PirateDuelDefend2.LoadAsset(),
                    TouExtensionAssets.PirateDuelDefend3.LoadAsset()
                };
        }
        catch
        {
            _sprites = null;
            /* Failed to load duel sprites - duel will not be possible */
        }
    }

    private static bool ShouldShiftDuelButtonLeft()
    {
        var role = PlayerControl.LocalPlayer?.Data?.Role;
        if (role == null) return false;

        var n = role.GetType().Name;

        return n.Contains("Ambassador", StringComparison.OrdinalIgnoreCase)
               || n.Contains("Swapper", StringComparison.OrdinalIgnoreCase)
               || n.Contains("Politician", StringComparison.OrdinalIgnoreCase);
    }

    private static void CreateDuelButton(MeetingHud meetingHud)
    {
        if (_sprites == null || _sprites.Length < 3) return;

        var localId = PlayerControl.LocalPlayer.PlayerId;
        PlayerVoteArea? myVoteArea = null;
        foreach (var pva in meetingHud.playerStates)
        {
            if (pva.TargetPlayerId == localId)
            {
                myVoteArea = pva;
                break;
            }
        }

        if (myVoteArea == null) return;

        var cancelButton = myVoteArea.Buttons.transform.Find("CancelButton");
        if (cancelButton == null) return;

        _duelButton = UObject.Instantiate(cancelButton.gameObject, myVoteArea.transform);
        _duelButton.name = "PirateDuelButton";

        var x = -0.45f;
        if (ShouldShiftDuelButtonLeft())
            x -= 0.30f;

        _duelButton.transform.localPosition = new Vector3(x, 0.03f, -3f);

        _duelButtonRenderer = _duelButton.GetComponent<SpriteRenderer>();
        if (_duelButtonRenderer != null && _sprites[0] != null)
        {
            _duelButtonRenderer.sprite = _sprites[0];
        }

        var button = _duelButton.GetComponent<PassiveButton>();
        if (button != null)
        {
            button.OverrideOnClickListeners(OnDuelButtonClicked);
            button.OverrideOnMouseOverListeners(() =>
            {
                if (_duelButtonRenderer != null && !_choiceLocked)
                    _duelButtonRenderer.color = new Color(1f, 1f, 0.5f, 1f);
            });
            button.OverrideOnMouseOutListeners(() =>
            {
                if (_duelButtonRenderer != null && !_choiceLocked)
                    _duelButtonRenderer.color = Color.white;
            });
        }

        var collider = _duelButton.GetComponent<BoxCollider2D>();
        if (collider != null && _duelButtonRenderer?.sprite != null)
        {
            collider.size = _duelButtonRenderer.sprite.bounds.size;
            collider.offset = Vector2.zero;
        }

        if (_duelButton.transform.childCount > 0)
        {
            _duelButton.transform.GetChild(0).gameObject.Destroy();
        }

        _duelButton.SetActive(true);
    }

    private static void OnDuelButtonClicked()
    {
        if (_choiceLocked) return;

        _localChoice = (_localChoice + 1) % 3;

        if (_duelButtonRenderer != null && _sprites != null && _sprites[_localChoice] != null)
        {
            _duelButtonRenderer.sprite = _sprites[_localChoice];
        }

        try
        {
            PirateRole.RpcDuelChoice(PlayerControl.LocalPlayer, _piratePlayerId, _localChoice);
        }
        catch (Exception)
        {
            /* Error sending duel choice RPC */
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.VotingComplete))]
    [HarmonyPrefix]
    public static void VotingCompletePrefix()
    {
        try
        {
            _choiceLocked = true;

            if (_duelActiveThisMeeting && (_isLocalPirate || _isLocalTarget))
            {
                PirateRole.RpcDuelChoice(PlayerControl.LocalPlayer, _piratePlayerId, _localChoice);
            }

            if (_duelButton != null)
            {
                var button = _duelButton.GetComponent<PassiveButton>();
                if (button != null) button.enabled = false;
                if (_duelButtonRenderer != null)
                    _duelButtonRenderer.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            }

            ResolveDuel();
        }
        catch (Exception)
        {
            /* Error during voting complete duel handling */
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
    [HarmonyPostfix]
    public static void MeetingClosePostfix()
    {
        RestoreMarkedNames();
        CleanupButton();
        _duelActiveThisMeeting = false;
        _choiceLocked = false;
        _duelTargetId = byte.MaxValue;

        if (PlayerControl.AllPlayerControls == null) return;
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player?.Data?.Role is PirateRole pirate)
            {
                pirate.ResetDuelState();
            }
        }
    }

    private static void ResolveDuel()
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

        PirateRole? activePirate = null;
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player?.Data?.Role is PirateRole pirate && pirate.DuelActive && !pirate.DuelResolved)
            {
                activePirate = pirate;
                break;
            }
        }

        if (activePirate == null) return;

        if (!PirateDuelSystem.IsDuelValid(activePirate))
        {
            activePirate.DuelResolved = true;
            return;
        }

        var result = PirateDuelSystem.GetDuelResult(activePirate.PirateChoice, activePirate.TargetChoice);
        PirateRole.RpcDuelResult(activePirate.Player, activePirate.DuelTargetId, result);
    }

    private static IEnumerator ShowDuelTargetNotification()
    {
        yield return new WaitForSeconds(0.5f);
        try
        {
            var notif = Helpers.CreateAndShowNotification(
                "You are being dueled by the Pirate!",
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TouExtensionIcons.PirateRoleIcon.LoadAsset());
            notif?.AdjustNotification();
        }
        catch (Exception)
        {
            /* Failed to show duel notification */
        }
    }

    private static IEnumerator ShowJailedNotification()
    {
        yield return new WaitForSeconds(0.5f);
        try
        {
            var notif = Helpers.CreateAndShowNotification(
                "You are jailed! Your duel has been cancelled.",
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TouExtensionIcons.PirateRoleIcon.LoadAsset());
            notif?.AdjustNotification();
        }
        catch (Exception)
        {
            /* Failed to show jailed notification */
        }
    }

    private static IEnumerator ShowTargetJailedNotification()
    {
        yield return new WaitForSeconds(0.5f);
        try
        {
            var notif = Helpers.CreateAndShowNotification(
                "Your duel target is jailed! Duel cancelled.",
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TouExtensionIcons.PirateRoleIcon.LoadAsset());
            notif?.AdjustNotification();
        }
        catch (Exception)
        {
            /* Failed to show target jailed notification */
        }
    }

    private static void CleanupButton()
    {
        if (_duelButton != null)
        {
            UObject.Destroy(_duelButton);
            _duelButton = null;
        }

        _duelButtonRenderer = null;
        _sprites = null;
    }
}


















