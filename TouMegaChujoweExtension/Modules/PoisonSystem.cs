// Modules/PoisonSystem.cs
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Networking;
using TownOfUs.Utilities;
using UnityEngine;

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
        if (local != null && local.PlayerId == poisonerId)
        {
            IsVineActive = true;
            VineTargetId = targetId;

            var follower = Camera.main?.GetComponent<FollowerCamera>();
            if (follower != null) follower.enabled = false;

            local.moveable = false;
            local.NetTransform.Halt();

            if (local.lightSource != null)
            {
                _originalLightOffset = local.lightSource.transform.localPosition;
                _lightMoved = true;
            }
        }
    }

    public static void Update()
    {
        HasActivePoison = false;
        PoisonTimeLeft = 0f;

        var localPlayer = PlayerControl.LocalPlayer;

        if (ActivePoisons.Count == 0)
        {
            if (IsVineActive) EndVineCamera();
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

        if (IsVineActive && Camera.main != null && localPlayer != null)
        {
            var vineTarget = MiscUtils.PlayerById(VineTargetId);
            if (vineTarget == null || vineTarget.Data.IsDead)
            {
                EndVineCamera();
                return;
            }

            var cam = Camera.main.transform;
            var targetPos = vineTarget.transform.position;
            targetPos.z = cam.position.z;
            cam.position = Vector3.Lerp(cam.position, targetPos, Time.deltaTime * 8f);

            if (localPlayer.lightSource != null)
            {
                var lightTransform = localPlayer.lightSource.transform;
                lightTransform.position = new Vector3(
                    cam.position.x, cam.position.y,
                    lightTransform.position.z);
            }
        }
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
        var localPlayer = PlayerControl.LocalPlayer;

        if (localPlayer != null)
        {
            foreach (var entry in ActivePoisons)
            {
                if (entry.PoisonerId != localPlayer.PlayerId) continue;
                
                if (PelicanSystem.IsSwallowed(entry.PoisonerId)) continue;
                if (PelicanSystem.IsSwallowed(entry.TargetId)) continue;

                var target = MiscUtils.PlayerById(entry.TargetId);
                var poisoner = MiscUtils.PlayerById(entry.PoisonerId);

                if (target != null && !target.Data.IsDead && poisoner != null)
                {
                    var causeOfDeath = entry.IsVine ? "PoisonerVine" : "PoisonerPoison";

                    IsRemoteKill = true;
                    poisoner.RpcSpecialMurder(target,
                        resetKillTimer: false,
                        createDeadBody: false,
                        teleportMurderer: false,
                        showKillAnim: false,
                        causeOfDeath: causeOfDeath);
                    IsRemoteKill = false;
                }
            }
        }
        RoundReset();
    }

    public static void EndVineCamera()
    {
        if (!IsVineActive) return;
        IsVineActive = false;

        var local = PlayerControl.LocalPlayer;

        if (_lightMoved && local?.lightSource != null)
        {
            local.lightSource.transform.localPosition = _originalLightOffset;
            _lightMoved = false;
        }

        var follower = Camera.main?.GetComponent<FollowerCamera>();
        if (follower != null) follower.enabled = true;

        if (local != null && Camera.main != null)
        {
            var playerPos = local.transform.position;
            Camera.main.transform.position = new Vector3(
                playerPos.x, playerPos.y,
                Camera.main.transform.position.z);
        }

        if (local != null) local.moveable = true;
    }

    public static bool IsTargetPoisonedByPoison(byte targetId)
    {
        return ActivePoisons.Any(e => e.TargetId == targetId && !e.IsVine);
    }

    public static bool IsTargetPoisoned(byte targetId)
    {
        return ActivePoisons.Any(e => e.TargetId == targetId);
    }

    public static void RoundReset()
    {
        ActivePoisons.Clear();
        EndVineCamera();
        HasActivePoison = false;
        PoisonTimeLeft = 0f;
        IsRemoteKill = false;
        _lastExecuteFrame = -1;
        _lastExecuteTarget = byte.MaxValue;
    }

    public static void FullReset()
    {
        RoundReset();
        _lightMoved = false;
    }
}
