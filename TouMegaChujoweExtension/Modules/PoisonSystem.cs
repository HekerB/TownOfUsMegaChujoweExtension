using MiraAPI.GameOptions;
using TownOfUs.Networking;
using TownOfUs.Utilities;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Modules.Localization;
using System.Collections;
using MiraAPI.Hud;
using MiraAPI.Utilities;
using Reactor.Utilities;
using UnityEngine.UI;
using HarmonyLib;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TouMegaChujoweExtension.Modifiers.Crewmate;
using MiraAPI.Modifiers;
using BepInEx.Logging;
using TownOfUs;

namespace TouMegaChujoweExtension.Modules;

public static class PoisonSystem
{
    private struct PoisonEntry
    {
        public byte PoisonerId;
        public byte TargetId;
        public float TimeLeft;
        public bool IsVine;
    }

    private static readonly List<PoisonEntry> ActivePoisons = [];

    // Frame-level dedup
    private static int _lastExecuteFrame = -1;
    private static byte _lastExecuteTarget = byte.MaxValue;

    public static bool IsVineActive { get; private set; }
    public static byte VineTargetId { get; private set; }

    public static bool HasActivePoison { get; private set; }
    public static float PoisonTimeLeft { get; private set; }

    public static bool IsRemoteKill { get; set; }

    private static Vector3 _originalLightOffset;
    private static bool _lightMoved;

    public static bool IsSeeking { get; set; }
    public static int StartSeekingFrame { get; set; } = -1;

    private static GameObject? _poisonedNotificationObject;

    public static void StartPoison(byte poisonerId, byte targetId)
    {
        if (ActivePoisons.Any(e => e.TargetId == targetId && !e.IsVine)) return;

        var duration = OptionGroupSingleton<PoisonerOptions>.Instance.PoisonDuration;
        ActivePoisons.Add(new PoisonEntry
        {
            PoisonerId = poisonerId,
            TargetId = targetId,
            TimeLeft = duration,
            IsVine = false
        });

        var local = PlayerControl.LocalPlayer;
        if (local != null && local.PlayerId == poisonerId)
        {
            var target = MiscUtils.PlayerById(targetId);
            if (target != null)
            {
                ShowPoisonedNotification(target);
            }
        }
    }

    public static void StartVine(byte poisonerId, byte targetId)
    {
        if (ActivePoisons.Any(e => e.TargetId == targetId && e.IsVine)) return;

        var duration = OptionGroupSingleton<PoisonerOptions>.Instance.VineDuration;
        ActivePoisons.Add(new PoisonEntry
        {
            PoisonerId = poisonerId,
            TargetId = targetId,
            TimeLeft = duration,
            IsVine = true
        });

        var local = PlayerControl.LocalPlayer;
        if (local != null)
        {
            if (local.PlayerId == poisonerId)
            {
                IsVineActive = true;
                VineTargetId = targetId;
                local.NetTransform.Halt();

                var btn = CustomButtonSingleton<TouMegaChujoweExtension.Buttons.Classic.Impostor.PoisonerVineButton>.Instance;
                if (btn != null)
                {
                    btn.StartVining(duration);
                }
            }
            else if (local.PlayerId == targetId)
            {
                local.NetTransform.Halt();
            }
        }
    }

    public static void Update()
    {
        HasActivePoison = false;
        PoisonTimeLeft = 0f;

        var localPlayer = PlayerControl.LocalPlayer;

        if (localPlayer == null || localPlayer.Data.IsDead || MeetingHud.Instance != null)
        {
            if (IsSeeking)
            {
                var btn = CustomButtonSingleton<PoisonerVineButton>.Instance;
                if (btn != null) btn.EndSeeking(false);
                else IsSeeking = false;
            }
            if (IsVineActive)
            {
                var btn = CustomButtonSingleton<PoisonerVineButton>.Instance;
                if (btn != null) btn.EndVining();
                else EndVineCamera();
            }
        }

        if (IsSeeking && localPlayer != null && !localPlayer.Data.IsDead)
        {
            if (UnityEngine.Time.frameCount > StartSeekingFrame && Input.GetMouseButtonDown(0))
            {
                if (Camera.main != null)
                {
                    var mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    PlayerControl? clickedTarget = null;
                    var minClickDist = 0.8f;

                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (pc == null || pc.Data.IsDead || pc.PlayerId == localPlayer.PlayerId) continue;
                        if (pc.IsImpostorAligned()) continue;

                        var distToClick = Vector2.Distance(mouseWorldPos, pc.transform.position);
                        if (distToClick < minClickDist)
                        {
                            clickedTarget = pc;
                            break;
                        }
                    }

                    if (clickedTarget != null)
                    {
                        if (CheckAndTriggerShields(localPlayer, clickedTarget))
                        {
                            var vineBtn = CustomButtonSingleton<PoisonerVineButton>.Instance;
                            if (vineBtn != null)
                            {
                                vineBtn.EndSeeking(true);
                            }
                        }
                        else
                        {
                            PoisonerRole.RpcVineTarget(localPlayer, clickedTarget.PlayerId);
                            var vineBtn = CustomButtonSingleton<PoisonerVineButton>.Instance;
                            if (vineBtn != null)
                            {
                                vineBtn.EndSeeking(false);
                            }
                        }
                    }
                }
            }
        }

        if (ActivePoisons.Count == 0)
        {
            if (IsVineActive) EndVineCamera();
            UpdateShadowQuad(localPlayer);
            return;
        }

        List<PoisonEntry>? expiredEntries = null;

        for (var i = ActivePoisons.Count - 1; i >= 0; i--)
        {
            var entry = ActivePoisons[i];

            if (PelicanSystem.IsSwallowed(entry.PoisonerId))
            {
                ActivePoisons.RemoveAt(i);
                if (entry.IsVine && localPlayer != null && localPlayer.PlayerId == entry.PoisonerId)
                    EndVineCamera();
                continue;
            }

            var target = MiscUtils.PlayerById(entry.TargetId);
            if (target == null || target.Data.IsDead)
            {
                ActivePoisons.RemoveAt(i);
                if (entry.IsVine && localPlayer != null && localPlayer.PlayerId == entry.PoisonerId)
                    EndVineCamera();
                continue;
            }

            if (PelicanSystem.IsSwallowed(entry.TargetId))
            {
                ActivePoisons.RemoveAt(i);
                if (entry.IsVine && localPlayer != null && localPlayer.PlayerId == entry.PoisonerId)
                    EndVineCamera();
                continue;
            }

            entry.TimeLeft -= Time.deltaTime;
            ActivePoisons[i] = entry;

            if (entry.TimeLeft > 0f && localPlayer != null && entry.PoisonerId == localPlayer.PlayerId && !entry.IsVine)
            {
                HasActivePoison = true;
                PoisonTimeLeft = Mathf.Max(0f, entry.TimeLeft);
            }

            if (entry.TimeLeft <= 0f)
            {
                ActivePoisons.RemoveAt(i);
                expiredEntries ??= new List<PoisonEntry>();
                expiredEntries.Add(entry);
            }
        }

        if (expiredEntries != null)
        {
            foreach (var entry in expiredEntries)
            {
                ExecuteKill(entry);
            }
        }

        if (localPlayer != null && (HasActivePoison || IsVineActive))
        {
            localPlayer.killTimer = Mathf.Max(localPlayer.killTimer, 10f);
        }

        if (IsVineActive && localPlayer != null)
        {
            var vineTarget = MiscUtils.PlayerById(VineTargetId);
            if (vineTarget == null || vineTarget.Data.IsDead)
            {
                EndVineCamera();
            }
        }

        UpdateShadowQuad(localPlayer);
    }

    private static void ExecuteKill(PoisonEntry entry)
    {
        try
        {
            var poisoner = MiscUtils.PlayerById(entry.PoisonerId);
            var target = MiscUtils.PlayerById(entry.TargetId);

            if (poisoner == null || target == null || target.Data.IsDead) return;

            if (PelicanSystem.IsSwallowed(entry.PoisonerId)) return;
            if (PelicanSystem.IsSwallowed(entry.TargetId)) return;

            var localPlayer = PlayerControl.LocalPlayer;
            if (localPlayer != null && localPlayer.PlayerId == entry.PoisonerId)
            {
                ShowTargetDiedNotification(target.Data.PlayerName);
            }

            if (PlayerControl.LocalPlayer == null ||
                PlayerControl.LocalPlayer.PlayerId != entry.PoisonerId) return;

            var currentFrame = Time.frameCount;
            if (_lastExecuteFrame == currentFrame && _lastExecuteTarget == entry.TargetId) return;
            _lastExecuteFrame = currentFrame;
            _lastExecuteTarget = entry.TargetId;

            var causeOfDeath = entry.IsVine ? "PoisonerVine" : "PoisonerPoison";

            bool inMeeting = MeetingHud.Instance != null;

            IsRemoteKill = true;
            poisoner.RpcSpecialMurder(target,
                resetKillTimer: false,
                createDeadBody: !inMeeting,
                teleportMurderer: false,
                showKillAnim: !inMeeting,
                causeOfDeath: causeOfDeath);
            IsRemoteKill = false;

            if (!inMeeting)
            {
                PoisonerRole.RpcPlayDeathAnim(poisoner, entry.TargetId);
            }
        }
        finally
        {
            IsRemoteKill = false;
            if (entry.IsVine && PlayerControl.LocalPlayer != null &&
                PlayerControl.LocalPlayer.PlayerId == entry.PoisonerId)
            {
                EndVineCamera();
            }
        }
    }

    public static void MeetingKillsAndReset()
    {
        RoundReset();
    }

    public static void EndVineCamera()
    {
        if (!IsVineActive) return;
        IsVineActive = false;
    }

    private static bool _shadowDisabledByUs;

    private static void UpdateShadowQuad(PlayerControl? localPlayer)
    {
        if (localPlayer == null) return;

        var falconBtn = CustomButtonSingleton<FalconZoomButton>.Instance;
        bool isFalconZoomed = falconBtn != null && falconBtn.IsZoomed;

        bool isCameraZoomed = Camera.main != null && Camera.main.orthographicSize > 3.1f;
        bool isDead = localPlayer.Data != null && localPlayer.Data.IsDead;

        bool shouldDisableShadows = isDead || IsSeeking || IsVineActive || HasActivePoison
            || SniperSystem.IsAiming || isFalconZoomed || isCameraZoomed;

        if (shouldDisableShadows)
        {
            if (HudManager.Instance != null && HudManager.Instance.ShadowQuad != null && HudManager.Instance.ShadowQuad.gameObject.activeSelf)
            {
                HudManager.Instance.ShadowQuad.gameObject.SetActive(false);
                _shadowDisabledByUs = true;
            }
        }
        else if (_shadowDisabledByUs)
        {
            if (!PelicanSystem.IsSwallowed(localPlayer.PlayerId))
            {
                if (HudManager.Instance != null && HudManager.Instance.ShadowQuad != null && !HudManager.Instance.ShadowQuad.gameObject.activeSelf)
                {
                    HudManager.Instance.ShadowQuad.gameObject.SetActive(true);
                }
            }
            _shadowDisabledByUs = false;
        }
    }

    public static bool IsTargetPoisonedByPoison(byte targetId)
    {
        return ActivePoisons.Any(e => e.TargetId == targetId && !e.IsVine);
    }

    public static bool IsTargetPoisoned(byte targetId)
    {
        return ActivePoisons.Any(e => e.TargetId == targetId && !e.IsVine);
    }

    public static bool IsTargetVined(byte targetId)
    {
        return ActivePoisons.Any(e => e.TargetId == targetId && e.IsVine);
    }

    public static void CleanseTarget(byte targetId)
    {
        for (var i = ActivePoisons.Count - 1; i >= 0; i--)
        {
            var entry = ActivePoisons[i];
            if (entry.TargetId == targetId)
            {
                ActivePoisons.RemoveAt(i);
                if (entry.IsVine)
                {
                    var local = PlayerControl.LocalPlayer;
                    if (local != null)
                    {
                        if (local.PlayerId == entry.PoisonerId)
                        {
                            EndVineCamera();
                            var btn = CustomButtonSingleton<TouMegaChujoweExtension.Buttons.Classic.Impostor.PoisonerVineButton>.Instance;
                            if (btn != null)
                            {
                                btn.EndVining();
                                btn.Timer = btn.Cooldown;
                                local.SetKillTimer(btn.Cooldown);
                                TouMegaChujoweExtension.Buttons.Classic.Impostor.PoisonerPoisonButton.SetOwnCooldown();
                            }
                        }
                        else if (local.PlayerId == entry.TargetId)
                        {
                            EndVineCamera();
                        }
                    }
                }
            }
        }
    }

    public static bool IsPlayerInStasis(byte playerId)
    {
        if (ActivePoisons.Any(e => e.IsVine && (e.PoisonerId == playerId || e.TargetId == playerId)))
        {
            return true;
        }

        if (IsSeeking && PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == playerId)
        {
            return true;
        }

        return false;
    }

    public static bool CheckAndTriggerShields(PlayerControl poisoner, PlayerControl target)
    {
        if (target == null || poisoner == null) return false;

        // 1. Medic Shield
        if (target.TryGetModifier<TownOfUs.Modifiers.Crewmate.MedicShieldModifier>(out var medMod))
        {
            var medic = medMod.Medic?.GetRole<MedicRole>();
            if (medic != null)
            {
                MedicRole.RpcMedicShieldAttacked(poisoner, medic.Player, target);
            }
            return true;
        }

        // 2. Bodyguard Shield
        if (target.TryGetModifier<BodyguardShieldModifier>(out var bgMod))
        {
            if (bgMod.Bodyguard != null)
            {
                BodyguardRole.RpcBodyguardShieldAttacked(bgMod.Bodyguard, poisoner, target);
            }
            return true;
        }

        // 3. Doctor Shield
        if (target.TryGetModifier<DoctorShieldModifier>(out var docMod))
        {
            if (docMod.Doctor != null)
            {
                DoctorRole.RpcDoctorShieldAttacked(docMod.Doctor, target, poisoner);
            }
            return true;
        }

        // 4. Other modifiers
        if (target.HasModifier<TownOfUs.Modifiers.Crewmate.WardenFortifiedModifier>() ||
            target.HasModifier<TownOfUs.Modifiers.Crewmate.MagicMirrorModifier>() ||
            target.HasModifier<TownOfUs.Modifiers.FirstDeadShield>() ||
            target.HasModifier<TownOfUs.Modifiers.Neutral.GuardianAngelProtectModifier>() ||
            target.HasModifier<TownOfUs.Modifiers.Crewmate.ClericBarrierModifier>())
        {
            return true;
        }

        return false;
    }

    public static void ShowPoisonedNotification(PlayerControl target)
    {
        HidePoisonedNotification();
        if (HudManager.Instance == null || target == null) return;

        try
        {
            var format = TouLocale.Get("ExtensionPoisonerNotificationPoisoned", "You have poisoned {0}!");
            var message = string.Format(format, target.Data.PlayerName);
            Coroutines.Start(CoShowPoisonedNotificationThreeTimes(message));
        }
        catch (System.Exception ex)
        {
            Logger<TouMegaChujoweExtensionPlugin>.Error($"[PoisonSystem] Failed to show poison notification: {ex.Message}");
        }
    }

    private static IEnumerator CoShowPoisonedNotificationThreeTimes(string message)
    {
        for (int i = 0; i < 3; i++)
        {
            HidePoisonedNotification();
            try
            {
                var notif = Helpers.CreateAndShowNotification(
                    $"<b>{Palette.ImpostorRed.ToTextColor()}{message}</color></b>",
                    Palette.ImpostorRed,
                    new Vector3(0f, 1f, -20f),
                    spr: TouMegaChujoweExtension.Assets.TouExtensionIcons.PoisonerRole.LoadAsset());

                if (notif != null)
                {
                    _poisonedNotificationObject = notif.gameObject;
                    try { notif.AdjustNotification(); } catch { }
                    try
                    {
                        var canvasGroup = notif.GetComponent<CanvasGroup>();
                        if (canvasGroup != null) canvasGroup.alpha = 1f;
                    }
                    catch { }
                }
            }
            catch (System.Exception ex)
            {
                Logger<TouMegaChujoweExtensionPlugin>.Error($"[PoisonSystem] Failed to show notification attempt {i}: {ex.Message}");
            }
            yield return new WaitForSeconds(1.0f);
        }
    }

    public static void ShowTargetDiedNotification(string targetName)
    {
        HidePoisonedNotification();
        if (HudManager.Instance == null) return;

        try
        {
            var format = TouLocale.Get("ExtensionPoisonerNotificationTargetDied", "Your poisoned target {0} has died!");
            var message = string.Format(format, targetName);
            Coroutines.Start(CoShowTargetDiedNotificationThreeTimes(message));
        }
        catch (System.Exception ex)
        {
            Logger<TouMegaChujoweExtensionPlugin>.Error($"[PoisonSystem] Failed to show target died notification: {ex.Message}");
        }
    }

    private static IEnumerator CoShowTargetDiedNotificationThreeTimes(string message)
    {
        for (int i = 0; i < 3; i++)
        {
            HidePoisonedNotification();
            try
            {
                var notif = Helpers.CreateAndShowNotification(
                    $"<b>{Palette.ImpostorRed.ToTextColor()}{message}</color></b>",
                    Palette.ImpostorRed,
                    new Vector3(0f, 1f, -20f),
                    spr: TouMegaChujoweExtension.Assets.TouExtensionIcons.PoisonerRole.LoadAsset());

                if (notif != null)
                {
                    _poisonedNotificationObject = notif.gameObject;
                    try { notif.AdjustNotification(); } catch { }
                    try
                    {
                        var canvasGroup = notif.GetComponent<CanvasGroup>();
                        if (canvasGroup != null) canvasGroup.alpha = 1f;
                    }
                    catch { }
                }
            }
            catch (System.Exception ex)
            {
                Logger<TouMegaChujoweExtensionPlugin>.Error($"[PoisonSystem] Failed to show notification attempt {i}: {ex.Message}");
            }
            yield return new WaitForSeconds(1.0f);
        }
    }

    public static void HidePoisonedNotification()
    {
        if (_poisonedNotificationObject != null)
        {
            UnityEngine.Object.Destroy(_poisonedNotificationObject);
            _poisonedNotificationObject = null;
        }
    }

    public static void RoundReset()
    {
        ActivePoisons.Clear();
        EndVineCamera();
        HidePoisonedNotification();
        IsSeeking = false;
        HasActivePoison = false;
        PoisonTimeLeft = 0f;
        IsRemoteKill = false;
        _shadowDisabledByUs = false;
        _lastExecuteFrame = -1;
        _lastExecuteTarget = byte.MaxValue;
        
        foreach (var playerId in TouMegaChujoweExtension.Patches.Roles.Poisoner.PoisonerStasisFixedUpdatePatch.FrozenPlayers)
        {
            var pc = MiscUtils.PlayerById(playerId);
            if (pc != null) pc.moveable = true;
        }
        TouMegaChujoweExtension.Patches.Roles.Poisoner.PoisonerStasisFixedUpdatePatch.FrozenPlayers.Clear();

        TouMegaChujoweExtension.Patches.Roles.Crewmate.ClericCleanseOnMeetingStartPatch.CleansedPoisonPlayers.Clear();
    }

    public static void FullReset()
    {
        RoundReset();
        _lightMoved = false;
    }
}
