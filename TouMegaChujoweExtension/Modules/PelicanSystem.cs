using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Networking;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Utilities;
using System.Collections;
using TownOfUs.Events;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Modifiers.Game.Crewmate;
using TownOfUs.Modifiers.Game;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules;
using TownOfUs.Networking;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modules;

public static class PelicanSystem
{
    private static readonly Dictionary<byte, HashSet<byte>> SwallowedPlayers = new();
    private static readonly HashSet<byte> AllSwallowed = new();
    private static readonly Dictionary<byte, Vector2> OriginalPositions = new();
    private static IEnumerator? _spectateCoroutine;
    private static GameObject? _swallowedNotificationObject;
    private static readonly Dictionary<byte, Vector2> LastPelicanPositions = new();

    private static readonly HashSet<byte> PendingDigestVictims = new();
    private static readonly Dictionary<byte, byte> PendingDigestKillers = new();
    private static readonly HashSet<byte> DigestKillVictims = new();

    private static bool _preWinDigestDone;

    public static bool IsPendingDigest(byte victimId) => PendingDigestVictims.Contains(victimId);
    public static byte? GetDigestKiller(byte victimId) =>
        PendingDigestKillers.TryGetValue(victimId, out var id) ? id : null;

    public static void ClearPendingDigest(byte victimId)
    {
        PendingDigestVictims.Remove(victimId);
        PendingDigestKillers.Remove(victimId);
    }

    public static bool IsDigestKillVictim(byte victimId) => DigestKillVictims.Contains(victimId);
    public static void ClearDigestKillVictim(byte victimId) => DigestKillVictims.Remove(victimId);

    public static bool IsSwallowed(byte playerId) => AllSwallowed.Contains(playerId);

    public static HashSet<byte> GetSwallowedByPelican(byte pelicanId)
    {
        return SwallowedPlayers.TryGetValue(pelicanId, out var set) ? set : new HashSet<byte>();
    }

    private static readonly Dictionary<byte, byte> SwallowTracker = new();
    private static readonly Dictionary<byte, DateTime> SwallowTimes = new();

    public static DateTime? GetSwallowTime(byte victimId)
    {
        return SwallowTimes.TryGetValue(victimId, out var time) ? time : null;
    }

    public static byte? GetPelicanOf(byte victimId)
    {
        return SwallowTracker.TryGetValue(victimId, out var pelicanId) ? pelicanId : null;
    }

    public static void UpdatePelicanPosition(byte pelicanId, Vector2 position)
    {
        LastPelicanPositions[pelicanId] = position;
    }

    // ==================== FOOTSTEPS HELPERS ====================

    private static bool LocalInvestigatorActive()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data == null || local.Data.IsDead) return false;

        return local.HasModifier<InvestigatorModifier>();
    }

    private static void RemoveFootstepsIfPresent(PlayerControl player)
    {
        try
        {
            if (player != null && player.TryGetModifier<FootstepsModifier>(out var footsteps))
                player.RemoveModifier(footsteps);
        }
        catch { /* ignore modifier removal errors */ }
    }

    private static void RestoreFootstepsIfNeeded(PlayerControl player)
    {
        try
        {
            if (player == null || player.Data == null || player.HasDied()) return;
            if (IsSwallowed(player.PlayerId)) return;
            if (!LocalInvestigatorActive()) return;

            if (!player.HasModifier<FootstepsModifier>())
                player.AddModifier<FootstepsModifier>();
        }
        catch { /* ignore modifier restoration errors */ }
    }

    // ==================== PRE-WIN DIGEST ====================

    public static bool CheckAndDigestForWin(PlayerControl pelican)
    {
        if (_preWinDigestDone) return false;
        if (pelican == null || pelican.HasDied()) return false;
        if (!pelican.AmOwner) return false;

        var swallowed = GetSwallowedByPelican(pelican.PlayerId);
        if (swallowed.Count == 0) return false;

        var nonSwallowedAliveOthers = 0;
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.Data == null || player.HasDied()) continue;
            if (player.PlayerId == pelican.PlayerId) continue;
            if (IsSwallowed(player.PlayerId)) continue;
            nonSwallowedAliveOthers++;
        }

        if (nonSwallowedAliveOthers > 0) return false;

        // Win condition met, triggering digest for win!
        return false;
    }

    // ==================== SHIELD CHECKING ====================

    public static ShieldType CheckAllShields(PlayerControl pelican, PlayerControl target)
    {
        return target.GetShieldType();
    }

    public static bool HandleShieldCheck(PlayerControl pelican, PlayerControl target)
    {
        var shieldType = CheckAllShields(pelican, target);
        if (shieldType == ShieldType.None) return false;

        switch (shieldType)
        {
            case ShieldType.FirstDead:
                /* No special handling needed for first dead shield beyond flash */
                break;

            case ShieldType.Bodyguard:
                HandleBodyguardShieldHit(pelican, target);
                break;

            case ShieldType.Child:
                /* Child shield is handled by flash and blocking swallow */
                break;

            case ShieldType.Medic:
                HandleMedicShieldHit(pelican, target);
                break;

            case ShieldType.Warden:
                HandleWardenFortifiedHit(pelican, target);
                break;

            case ShieldType.Mirrorcaster:
                HandleMagicMirrorHit(pelican, target);
                break;

            case ShieldType.Fairy:
                return true;

            case ShieldType.Mercenary:
                return true;

            case ShieldType.DeadlyQuota:
                return true;

            case ShieldType.Oracle:
                HandleOracleBlessHit(pelican, target);
                break;
        }

        if (pelican.AmOwner)
        {
            ShieldUtils.TriggerShieldFlash(pelican, shieldType);
        }

        Logger<TouMegaChujoweExtensionPlugin>.Info(
            $"[PelicanSystem] Swallow blocked by {shieldType} on player {target.PlayerId}");
        return true;
    }

    private static void HandleBodyguardShieldHit(PlayerControl pelican, PlayerControl target)
    {
        try
        {
            if (target.TryGetModifier<BodyguardShieldModifier>(out var shieldMod))
            {
                var bodyguard = shieldMod.Bodyguard;
                if (bodyguard != null)
                {
                    BodyguardRole.RpcBodyguardShieldAttacked(bodyguard, pelican, target);
                    return;
                }
            }
        }
        catch (System.Exception ex)
        {
            Logger<TouMegaChujoweExtensionPlugin>.Error(
                $"[PelicanSystem] Error handling Bodyguard shield: {ex.Message}");
        }
    }

    private static void HandleMedicShieldHit(PlayerControl pelican, PlayerControl target)
    {
        try
        {
            if (target.TryGetModifier<MedicShieldModifier>(out var medicMod))
                MedicRole.RpcMedicShieldAttacked(medicMod.Medic, pelican, target);
        }
        catch (System.Exception ex)
        {
            Logger<TouMegaChujoweExtensionPlugin>.Error($"[PelicanSystem] Error handling Medic shield: {ex.Message}");
        }
    }

    private static void HandleWardenFortifiedHit(PlayerControl pelican, PlayerControl target)
    {
        try
        {
            if (target.TryGetModifier<WardenFortifiedModifier>(out var wardenMod))
                WardenRole.RpcWardenNotify(wardenMod.Warden, pelican, target);
        }
        catch (System.Exception ex)
        {
            Logger<TouMegaChujoweExtensionPlugin>.Error($"[PelicanSystem] Error handling Warden shield: {ex.Message}");
        }
    }

    private static void HandleMagicMirrorHit(PlayerControl pelican, PlayerControl target)
    {
        try
        {
            if (target.TryGetModifier<MagicMirrorModifier>(out var mirrorMod))
                MirrorcasterRole.RpcMagicMirrorAttacked(mirrorMod.Mirrorcaster, pelican, target);
        }
        catch (System.Exception ex)
        {
            Logger<TouMegaChujoweExtensionPlugin>.Error($"[PelicanSystem] Error handling Mirrorcaster shield: {ex.Message}");
        }
    }

    private static void HandleOracleBlessHit(PlayerControl pelican, PlayerControl target)
    {
        try
        {
            if (target.TryGetModifier<TownOfUs.Modifiers.Crewmate.OracleBlessedModifier>(out var oracleMod))
                OracleRole.RpcOracleBlessNotify(pelican, oracleMod.Oracle, target);
        }
        catch (System.Exception ex)
        {
            Logger<TouMegaChujoweExtensionPlugin>.Error($"[PelicanSystem] Error handling Oracle blessing: {ex.Message}");
        }
    }

    // ==================== SWALLOW / DIGEST / RELEASE ====================

    public static void SwallowPlayer(byte pelicanId, byte victimId)
    {
        if (!SwallowedPlayers.TryGetValue(pelicanId, out var set))
        {
            set = new HashSet<byte>();
            SwallowedPlayers[pelicanId] = set;
        }

        set.Add(victimId);
        AllSwallowed.Add(victimId);
        SwallowTracker[victimId] = pelicanId;
        SwallowTimes[victimId] = DateTime.UtcNow;

        var victim = MiscUtils.PlayerById(victimId);
        if (victim != null)
        {
            OriginalPositions[victimId] = victim.GetTruePosition();
            EndExternalControl(victim);
            ClearBodyguardState(victim);

            victim.NetTransform.Halt();
            victim.moveable = false;
            victim.Visible = false;

            var pelican = MiscUtils.PlayerById(pelicanId);
            if (pelican != null)
            {
                var pelicanPos = pelican.GetTruePosition();
                victim.transform.position = new Vector3(pelicanPos.x, pelicanPos.y, victim.transform.position.z);
                victim.NetTransform.SnapTo(pelicanPos);
            }

            if (!victim.HasModifier<PelicanSwallowedModifier>())
            {
                try
                {
                    victim.AddModifier<PelicanSwallowedModifier>();
                    if (victim.TryGetModifier<PelicanSwallowedModifier>(out var mod))
                        mod.PelicanId = pelicanId;
                }
                catch (System.Exception ex)
                {
                    Logger<TouMegaChujoweExtensionPlugin>.Error($"[PelicanSystem] Failed to add modifier: {ex.Message}");
                }
            }

            RemoveFootstepsIfPresent(victim);

            if (victim.AmOwner)
            {
                ShowSwallowedNotification();
                StartSpectatingPelican(pelicanId);
            }
        }
    }

    private static void ClearBodyguardState(PlayerControl player)
    {
        try
        {
            if (player.Data?.Role is BodyguardRole bgRole)
            {
                bgRole.BacklashReady = false;
                bgRole.KillModeActive = false;
                bgRole.KillModeTimer = 0f;
                bgRole.LastAttacker = null;
                bgRole.MarkedAttackerDot = false;
            }
        }
        catch { /* ignore role-specific state cleanup errors */ }
    }

    private static void EndExternalControl(PlayerControl victim)
    {
        try
        {
            if (TownOfUs.Modules.ControlSystem.ParasiteControlState.IsControlled(victim.PlayerId, out var parasiteId))
            {
                var parasite = MiscUtils.PlayerById(parasiteId);
                if (parasite?.Data?.Role is TownOfUs.Roles.Impostor.ParasiteRole)
                    TownOfUs.Roles.Impostor.ParasiteRole.RpcParasiteEndControl(parasite, victim);
            }
        }
        catch { /* ignore Parasite/Puppeteer state errors */ }

        try
        {
            if (TownOfUs.Modules.ControlSystem.PuppeteerControlState.IsControlled(victim.PlayerId, out var puppeteerId))
            {
                var puppeteer = MiscUtils.PlayerById(puppeteerId);
                if (puppeteer?.Data?.Role is TownOfUs.Roles.Impostor.PuppeteerRole)
                    TownOfUs.Roles.Impostor.PuppeteerRole.RpcPuppeteerEndControl(puppeteer, victim);
            }
        }
        catch { /* ignore Puppeteer state errors */ }

        try
        {
            if (victim.Data?.Role is TownOfUs.Roles.Impostor.ParasiteRole parasiteRole && parasiteRole.Controlled != null)
                TownOfUs.Roles.Impostor.ParasiteRole.RpcParasiteEndControl(victim, parasiteRole.Controlled);
        }
        catch { /* ignore Parasite role state errors */ }

        try
        {
            if (victim.Data?.Role is TownOfUs.Roles.Impostor.PuppeteerRole puppeteerRole && puppeteerRole.Controlled != null)
                TownOfUs.Roles.Impostor.PuppeteerRole.RpcPuppeteerEndControl(victim, puppeteerRole.Controlled);
        }
        catch { /* ignore Puppeteer role state errors */ }
    }

    public static void DigestAll(byte pelicanId)
    {
        if (!SwallowedPlayers.TryGetValue(pelicanId, out var victims)) return;

        foreach (var victimId in victims.ToList())
        {
            var victim = MiscUtils.PlayerById(victimId);

            if (victim != null && !victim.HasDied())
            {
                AllSwallowed.Remove(victimId);
                SwallowTracker.Remove(victimId);
            SwallowTimes.Remove(victimId);
                OriginalPositions.Remove(victimId);

                DigestKillVictims.Add(victimId);
                victim.Die(DeathReason.Kill, false);

                var pelican = MiscUtils.PlayerById(pelicanId);
                
                // Trigger AfterMurderEvent so it counts as a real kill for the system
                if (pelican != null)
                {
                    var afterMurderEvent = new AfterMurderEvent(pelican, victim, null);
                    MiraEventManager.InvokeEvent(afterMurderEvent);
                }

                var localPlayer = PlayerControl.LocalPlayer;
                bool isGhostOrPelican = localPlayer != null && (localPlayer.Data.IsDead || localPlayer.PlayerId == pelicanId);

                if (!victim.HasModifier<DeathHandlerModifier>())
                {
                    try { victim.AddModifier<DeathHandlerModifier>(); } catch { /* ignore death handler addition failure */ }
                }

                try
                {
                    string killerText = (isGhostOrPelican && pelican != null) 
                        ? TouLocale.GetParsed("ExtensionDiedByPelican", "by <player>").Replace("<player>", pelican.Data.PlayerName) 
                        : "";

                    DeathHandlerModifier.UpdateDeathHandlerImmediate(
                        victim,
                        causeOfDeath: TouLocale.Get("ExtensionDiedToPelican", "Digested"),
                        roundOfDeath: DeathEventHandlers.CurrentRound,
                        diedThisRound: DeathHandlerOverride.SetTrue,
                        killedBy: killerText,
                        lockInfo: DeathHandlerOverride.SetTrue
                    );
                }
                catch { /* ignore death handler update errors */ }
            }

            if (victim != null)
            {
                RemoveSwallowedModifier(victim);
                victim.Visible = true;
                victim.moveable = true;

                if (victim.AmOwner)
                {
                    StopSpectatingPelican();
                    HideSwallowedNotification();
                    Coroutines.Start(CoRefreshHud());
                }
            }

            AllSwallowed.Remove(victimId);
            SwallowTracker.Remove(victimId);
            SwallowTimes.Remove(victimId);
            OriginalPositions.Remove(victimId);

            // Host assigns the proper Ghost role so the player gets ghost abilities (like Haunt)
            if (AmongUsClient.Instance.AmHost && victim != null)
            {
                try
                {
                    var role = victim.Data?.Role;
                    if (role is TownOfUs.Roles.ITownOfUsRole touRole)
                    {
                        var ghostRole = touRole.Configuration.GhostRole;
                        if ((int)ghostRole != -1)
                        {
                            victim.RpcSetRole(ghostRole);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Logger<TouMegaChujoweExtensionPlugin>.Error($"[PelicanSystem] Role set error: {ex.Message}");
                }
            }
        }

        SwallowedPlayers.Remove(pelicanId);
    }

    private static IEnumerator CoRefreshHud()
    {
        yield return new WaitForSeconds(0.1f);
        if (HudManager.Instance != null)
        {
            try
            {
                HudManager.Instance.SetHudActive(false);
                HudManager.Instance.SetHudActive(true);
            }
            catch { /* ignore HUD refresh errors */ }
        }
    }

    public static void ReleaseAllAtPosition(byte pelicanId, Vector2 releasePosition)
    {
        if (!SwallowedPlayers.TryGetValue(pelicanId, out var victims) || victims.Count == 0) return;

        var safePosition = FindSafePosition(releasePosition);
        DoRelease(pelicanId, victims, safePosition);
    }

    public static void ReleaseAll(byte pelicanId)
    {
        if (!SwallowedPlayers.TryGetValue(pelicanId, out var victims) || victims.Count == 0) return;

        Vector2 releasePosition;

        if (LastPelicanPositions.TryGetValue(pelicanId, out var trackedPos))
        {
            releasePosition = trackedPos;
        }
        else
        {
            var pelican = MiscUtils.PlayerById(pelicanId);
            if (pelican != null)
            {
                releasePosition = pelican.GetTruePosition();
                if (releasePosition.x < -500f || releasePosition.y < -500f)
                    releasePosition = GetFallbackPosition(victims);
            }
            else
            {
                releasePosition = GetFallbackPosition(victims);
            }
        }

        var safePosition = FindSafePosition(releasePosition);
        DoRelease(pelicanId, victims, safePosition);
    }

    private static Vector2 GetFallbackPosition(HashSet<byte> victims)
    {
        foreach (var victimId in victims)
        {
            if (OriginalPositions.TryGetValue(victimId, out var origPos)) return origPos;
        }
        return Vector2.zero;
    }

    private static void DoRelease(byte pelicanId, HashSet<byte> victims, Vector2 safePosition)
    {
        foreach (var victimId in victims.ToList())
        {
            var victim = MiscUtils.PlayerById(victimId);
            if (victim != null)
            {
                RemoveSwallowedModifier(victim);

                try
                {
                    if (victim.TryGetModifier<DeathHandlerModifier>(out var dhMod))
                        victim.RemoveModifier(dhMod);
                }
                catch { /* ignore death handler removal errors */ }

                if (!victim.HasDied())
                {
                    victim.Visible = true;
                    victim.moveable = true;
                    victim.transform.position = new Vector3(safePosition.x, safePosition.y, 0f);
                    victim.NetTransform.SnapTo(safePosition);

                    RestoreFootstepsIfNeeded(victim);
                }

                if (victim.AmOwner)
                {
                    StopSpectatingPelican();
                    HideSwallowedNotification();
                    if (!victim.HasDied()) ShowReleaseNotification();
                }

                Logger<TouMegaChujoweExtensionPlugin>.Info(
                    $"[PelicanSystem] Released player {victimId} at ({safePosition.x}, {safePosition.y})");
            }

            AllSwallowed.Remove(victimId);
            SwallowTracker.Remove(victimId);
            SwallowTimes.Remove(victimId);
            OriginalPositions.Remove(victimId);
        }

        victims.Clear();
        SwallowedPlayers.Remove(pelicanId);
        LastPelicanPositions.Remove(pelicanId);
    }

    public static void ReleaseSinglePlayer(byte victimId)
    {
        if (!SwallowTracker.TryGetValue(victimId, out var pelicanId)) return;
        if (!SwallowedPlayers.TryGetValue(pelicanId, out var victims)) return;

        var victim = MiscUtils.PlayerById(victimId);
        if (victim != null)
        {
            RemoveSwallowedModifier(victim);
            try
            {
                if (victim.TryGetModifier<DeathHandlerModifier>(out var dhMod))
                    victim.RemoveModifier(dhMod);
            }
            catch { }

            if (!victim.HasDied())
            {
                victim.Visible = true;
                victim.moveable = true;

                if (OriginalPositions.TryGetValue(victimId, out var origPos))
                {
                    victim.transform.position = new Vector3(origPos.x, origPos.y, 0f);
                    victim.NetTransform.SnapTo(origPos);
                }

                RestoreFootstepsIfNeeded(victim);
            }

            if (victim.AmOwner)
            {
                StopSpectatingPelican();
                HideSwallowedNotification();
                if (!victim.HasDied()) ShowReleaseNotification();
            }
        }

        victims.Remove(victimId);
        AllSwallowed.Remove(victimId);
        SwallowTracker.Remove(victimId);
        SwallowTimes.Remove(victimId);
        OriginalPositions.Remove(victimId);

        if (victims.Count == 0)
        {
            SwallowedPlayers.Remove(pelicanId);
            LastPelicanPositions.Remove(pelicanId);
        }
    }

    private static Vector2 FindSafePosition(Vector2 targetPosition)
    {
        if (targetPosition.x < -500f || targetPosition.y < -500f)
        {
            foreach (var kvp in OriginalPositions)
            {
                if (IsPositionSafe(kvp.Value, kvp.Value)) return kvp.Value;
            }
            return Vector2.zero;
        }

        if (IsPositionSafe(targetPosition, targetPosition)) return targetPosition;

        float[] distances = { 0.3f, 0.5f, 0.7f, 1.0f, 1.3f, 1.5f, 2.0f };
        int directions = 8;

        foreach (var dist in distances)
        {
            for (int i = 0; i < directions; i++)
            {
                var angle = (2f * Mathf.PI * i) / directions;
                var candidate = targetPosition + new Vector2(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist);

                if (IsPositionSafe(candidate, targetPosition)) return candidate;
            }
        }

        foreach (var kvp in OriginalPositions)
        {
            if (IsPositionSafe(kvp.Value, kvp.Value)) return kvp.Value;
        }

        try
        {
            if (ShipStatus.Instance != null)
            {
                var spawn = ShipStatus.Instance.InitialSpawnCenter;
                if (IsPositionSafe(spawn, spawn)) return spawn;
            }
        }
        catch { /* ignore ship status center errors */ }

        return targetPosition;
    }

    private static bool IsPositionSafe(Vector2 candidate, Vector2 fromPosition)
    {
        try
        {
            if (ShipStatus.Instance == null) return true;

            var hit = Physics2D.OverlapCircle(candidate, 0.22f, Constants.ShipAndAllObjectsMask);
            if (hit != null) return false;

            if (Vector2.Distance(candidate, fromPosition) > 0.1f)
            {
                var wallHit = Physics2D.Linecast(fromPosition, candidate, Constants.ShipAndAllObjectsMask);
                if (wallHit.collider != null) return false;
            }

            return true;
        }
        catch { /* fallback to safe default on error */ return true; }
    }

    private static void RemoveSwallowedModifier(PlayerControl player)
    {
        if (player.TryGetModifier<PelicanSwallowedModifier>(out var mod))
            player.RemoveModifier(mod);
    }

    public static void StartSpectatingPelican(byte pelicanId)
    {
        StopSpectatingPelican();
        _spectateCoroutine = CoSpectatePelican(pelicanId);
        Coroutines.Start(_spectateCoroutine);
    }

    public static void StopSpectatingPelican()
    {
        if (_spectateCoroutine != null)
        {
            Coroutines.Stop(_spectateCoroutine);
            _spectateCoroutine = null;
        }

        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer != null && Camera.main != null)
        {
            var follower = Camera.main.GetComponent<FollowerCamera>();
            if (follower != null) follower.SetTarget(localPlayer);
        }
    }

    private static IEnumerator CoSpectatePelican(byte pelicanId)
    {
        yield return new WaitForSeconds(0.1f);

        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null) yield break;

        while (true)
        {
            if (LobbyBehaviour.Instance != null || AmongUsClient.Instance == null ||
                !AmongUsClient.Instance.IsGameStarted)
                yield break;

            if (!IsSwallowed(localPlayer.PlayerId)) yield break;

            var pelican = MiscUtils.PlayerById(pelicanId);
            if (pelican == null || pelican.HasDied()) yield break;

            if (Camera.main != null)
            {
                var follower = Camera.main.GetComponent<FollowerCamera>();
                if (follower != null) follower.SetTarget(pelican);
            }

            yield return null;
        }
    }

    public static void ShowSwallowedNotification()
    {
        HideSwallowedNotification();
        if (HudManager.Instance == null) return;

        try
        {
            var line1 = TouLocale.Get("ExtensionPelicanSwallowedNotification", "You have been swallowed by Pelican!");
            var message = line1.Replace("/n", "\n").Replace("\\n", "\n");

            var notif = Helpers.CreateAndShowNotification(
                $"<b>{TouExtensionColors.Pelican.ToTextColor()}{message}</color></b>",
                Color.white, new Vector3(0f, 2f, -20f));

            if (notif != null)
            {
                _swallowedNotificationObject = notif.gameObject;
                try { notif.AdjustNotification(); } catch { /* ignore adjustment errors */ }
                try
                {
                    var canvasGroup = notif.GetComponent<CanvasGroup>();
                    if (canvasGroup != null) canvasGroup.alpha = 1f;
                }
                catch { /* ignore alpha setting errors */ }
            }
        }
        catch (System.Exception ex)
        {
            Logger<TouMegaChujoweExtensionPlugin>.Error($"[PelicanSystem] Failed to show notification: {ex.Message}");
        }
    }

    public static void ShowReleaseNotification()
    {
        if (HudManager.Instance == null) return;

        var message = TouLocale.Get("ExtensionPelicanReleasedNotification", "You have been released from the Pelican!");
        Helpers.CreateAndShowNotification(
            $"<b>{TouExtensionColors.Pelican.ToTextColor()}{message}</color></b>",
            Color.white, new Vector3(0f, 2f, -20f));
    }

    public static void HideSwallowedNotification()
    {
        if (_swallowedNotificationObject != null)
        {
            UnityEngine.Object.Destroy(_swallowedNotificationObject);
            _swallowedNotificationObject = null;
        }
    }

    public static void ClearForPelican(byte pelicanId)
    {
        if (SwallowedPlayers.TryGetValue(pelicanId, out var victims))
        {
            foreach (var victimId in victims.ToList())
            {
                var victim = MiscUtils.PlayerById(victimId);
                if (victim != null)
                {
                    RemoveSwallowedModifier(victim);
                    RestoreFootstepsIfNeeded(victim);

                    if (victim.AmOwner)
                    {
                        StopSpectatingPelican();
                        HideSwallowedNotification();
                    }
                }

                AllSwallowed.Remove(victimId);
                SwallowTracker.Remove(victimId);
            SwallowTimes.Remove(victimId);
                OriginalPositions.Remove(victimId);
            }

            SwallowedPlayers.Remove(pelicanId);
        }

        LastPelicanPositions.Remove(pelicanId);
    }

    public static void ClearAll()
    {
        StopSpectatingPelican();
        HideSwallowedNotification();

        foreach (var pelicanId in SwallowedPlayers.Keys.ToList())
        {
            if (SwallowedPlayers.TryGetValue(pelicanId, out var victims))
            {
                foreach (var victimId in victims)
                {
                    var victim = MiscUtils.PlayerById(victimId);
                    if (victim != null)
                    {
                        RemoveSwallowedModifier(victim);
                        victim.Visible = true;
                        victim.moveable = true;
                        RestoreFootstepsIfNeeded(victim);
                    }
                }
            }
        }

        SwallowedPlayers.Clear();
        AllSwallowed.Clear();
        SwallowTracker.Clear();
        SwallowTimes.Clear();
        OriginalPositions.Clear();
        LastPelicanPositions.Clear();
        PendingDigestVictims.Clear();
        PendingDigestKillers.Clear();
        DigestKillVictims.Clear();
        _preWinDigestDone = false;
    }

    public static void ForceResetAllPlayers()
    {
        StopSpectatingPelican();
        HideSwallowedNotification();

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null) continue;
            try { RemoveSwallowedModifier(player); } catch { }
            player.Visible = true;
            player.moveable = true;
            RestoreFootstepsIfNeeded(player);
            try { player.NetTransform.Halt(); } catch { }
        }

        SwallowedPlayers.Clear();
        AllSwallowed.Clear();
        SwallowTracker.Clear();
        SwallowTimes.Clear();
        OriginalPositions.Clear();
        LastPelicanPositions.Clear();
        PendingDigestVictims.Clear();
        PendingDigestKillers.Clear();
        DigestKillVictims.Clear();
        _preWinDigestDone = false;
    }
}



















