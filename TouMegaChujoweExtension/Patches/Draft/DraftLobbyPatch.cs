using System.Collections.Generic;
using AmongUs.GameOptions;
using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options;
using TMPro;
using UnityEngine;
using Reactor.Utilities;

using Object = UnityEngine.Object;
using Il2CppInterop.Runtime;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;


namespace TouMegaChujoweExtension.Patches.Draft;

[HarmonyPatch]
public static class DraftLobbyPatch
{
    // === UI ===
    private static GameObject _draftContainer;
    private static GameObject _overlayBackground;
    private static TextMeshPro _playerListText;
    private static TextMeshPro _timerText;
    private static TextMeshPro _draftCompleteText;
    private static TextMeshPro _draftTitleText;
    private static float _titleRandomOffset;
    private static bool _isTitleAnimRunning;
    private static Sprite _cachedRoundedSprite;
    private static Sprite _cachedRandomIcon;
    private static readonly List<GameObject> _roleButtonObjects = new();
    private static readonly System.Text.StringBuilder _playerListBuilder = new();

    // === BUTTON REFS ===
    private class ButtonRefs
    {
        public SpriteRenderer BG;
        public SpriteRenderer Border;
        public TextMeshPro Label;
        public SpriteRenderer Icon;
        public SpriteRenderer RandomIcon;
        public float NormalizedScale;
    }
    private static readonly Dictionary<GameObject, ButtonRefs> _buttonRefs = new();
    private static readonly Dictionary<GameObject, System.Collections.IEnumerator> _hoverCoroutines = new();
    private static readonly Dictionary<GameObject, System.Collections.IEnumerator> _bounceCoroutines = new();
    private static readonly Dictionary<GameObject, float> _targetScales = new();

    // === STATE ===
    public static bool _draftInProgress;
    private static float _pickTimer;
    private static bool _countdownWasActive;
    public static bool _draftCompletedWaitingForStart;
    private static bool _alertPlayed;
    private static bool _isMusicMuted = false;
    private static GameObject _muteButtonObj;
    private static GameObject _cancelButtonObj;
    private static GameObject _forceStartButtonObj;
    private static bool _pickLocked = false;
    private static byte? _lastAlertedPicker;
    private static int _lastTimeLeftInt = -1;
    private static int _lastDotState = -1;
    private static bool _forceUpdatePlayerList = false;

    // === DANGER MUSIC (DUAL SOURCE CROSSFADE) ===
    private static AudioSource _draftMusicSourceA;
    private static AudioSource _draftMusicSourceB;
    private static bool _usingSourceA = true;

    private static float _crossfadeTimer = 0f;
    private static float _crossfadeDuration = 1.25f;
    private static bool _isCrossfading = false;

    private static float _musicVolume = 0.25f;
    private static float _musicPitch = 1f;

    private static float _crossfadeFromVol = 0.25f;
    private static float _crossfadeToVol = 0.25f;

    private static AudioClip[] _dangerClips;
    private static int _currentDangerLevel = -1;
    private static int _totalDraftPickers = 0;
    private static bool _clipsLoading = false;

    private static int _baseDangerClipIndex = 0;
    private static int _finalDangerClipIndex = 0;
    private static bool _finalDangerTriggered = false;

    // === HNS COMPLETE SOUND ===
    private static AudioClip _hnsTimeToHideClip;

    // === LOBBY LOCK ===
    private static bool _lobbyWasPublic;
    private static bool _lobbyLocked;

    // === FONT CACHE ===
    private static TMP_FontAsset _chewyFont;
    
    // === TOOLTIP ===
    private static TextMeshPro _tooltipText;

    // === COUNTDOWN SOUND ===
    private static float _countdownSoundTimer = 1f;
    private static AudioClip _countdownTickClip;
    private static AudioClip _hoverSoundClip;

    // === DISCONNECTED PLAYERS ===
    private static readonly HashSet<byte> _disconnectedDuringDraft = new();

    // === ORIGINAL ORDER TRACKING ===
    private static readonly List<byte> _originalPickOrder = new();

    // === CROSSFADE HELPERS ===
    private static AudioSource ActiveSource => _usingSourceA ? _draftMusicSourceA : _draftMusicSourceB;
    private static AudioSource InactiveSource => _usingSourceA ? _draftMusicSourceB : _draftMusicSourceA;

    private static TMP_FontAsset GetChewyFont()
    {
        if (_chewyFont != null) return _chewyFont;
        var allFonts = Resources.FindObjectsOfTypeAll(Il2CppType.Of<TMP_FontAsset>());
        foreach (var obj in allFonts)
        {
            var font = obj.Cast<TMP_FontAsset>();
            if (font.name.Contains("Chewy", System.StringComparison.OrdinalIgnoreCase))
            {
                _chewyFont = font;
                return _chewyFont;
            }
        }
        return null;
    }

    private static void ApplyFont(TextMeshPro tmp)
    {
        var font = TouExtensionFonts.ChewyFont;
        if (font != null) tmp.font = font;
    }

    private static bool AmIFirstPicker()
    {
        var lp = PlayerControl.LocalPlayer;
        if (lp == null) return false;

        if (DraftSystem.CurrentPicker.HasValue)
            return DraftSystem.CurrentPicker.Value == lp.PlayerId;

        try
        {
            if (DraftSystem.PickOrder != null && DraftSystem.PickOrder.Count > 0)
                return DraftSystem.PickOrder[0] == lp.PlayerId;
        }
        catch { }

        return false;
    }

    // === FIND DANGER CLIPS ===

    private static AudioClip[] FindDangerClips()
    {
        var allClips = Resources.FindObjectsOfTypeAll(Il2CppType.Of<AudioClip>());
        var found = new SortedDictionary<string, AudioClip>();

        foreach (var obj in allClips)
        {
            var clip = obj.Cast<AudioClip>();
            if (clip.name.IndexOf("danger", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                clip.name.IndexOf("hns_danger", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                found.TryAdd(clip.name, clip);
                // Info($"[Draft] Found danger clip in memory: {clip.name}");
            }
        }

        if (found.Count > 0)
        {
            var result = new AudioClip[found.Count];
            int idx = 0;
            foreach (var kvp in found)
                result[idx++] = kvp.Value;
            // Info($"[Draft] Loaded {result.Length} danger clips from memory.");
            return result;
        }

        return null;
    }

    private static AudioClip FindCountdownTickClip()
    {
        if (_countdownTickClip != null) return _countdownTickClip;

        var allClips = Resources.FindObjectsOfTypeAll(Il2CppType.Of<AudioClip>());
        foreach (var obj in allClips)
        {
            var clip = obj.Cast<AudioClip>();
            if (clip.name.IndexOf("hns", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                clip.name.IndexOf("countdown", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _countdownTickClip = clip;
                // Info($"[Draft] Found countdown clip: {clip.name}");
                return _countdownTickClip;
            }
        }

        return _countdownTickClip;
    }

    private static AudioClip FindTimeToHideClip()
    {
        if (_hnsTimeToHideClip != null) return _hnsTimeToHideClip;

        var allClips = Resources.FindObjectsOfTypeAll(Il2CppType.Of<AudioClip>());

        foreach (var obj in allClips)
        {
            var clip = obj.Cast<AudioClip>();
            string n = clip.name.ToLower();
            if (n.Contains("stinger") && (n.Contains("hns") || n.Contains("hide") || n.Contains("seek")))
            {
                _hnsTimeToHideClip = clip;
                // Info($"[Draft] Found stinger clip: {clip.name} ({clip.length:F1}s)");
                return _hnsTimeToHideClip;
            }
        }

        foreach (var obj in allClips)
        {
            var clip = obj.Cast<AudioClip>();
            string n = clip.name.ToLower();
            if ((n.Contains("time") && n.Contains("hide")) ||
                (n.Contains("go") && n.Contains("hide")) ||
                (n.Contains("hide") && n.Contains("start")) ||
                (n.Contains("round") && n.Contains("start") && n.Contains("hns")) ||
                (n.Contains("hns") && n.Contains("begin")))
            {
                _hnsTimeToHideClip = clip;
                // Info($"[Draft] Found hide clip: {clip.name} ({clip.length:F1}s)");
                return _hnsTimeToHideClip;
            }
        }

        foreach (var obj in allClips)
        {
            var clip = obj.Cast<AudioClip>();
            string n = clip.name.ToLower();
            if (n.Contains("hns") && clip.length >= 1.5f && clip.length <= 10f &&
                !n.Contains("danger") && !n.Contains("countdown") && !n.Contains("footstep"))
            {
                _hnsTimeToHideClip = clip;
                // Info($"[Draft] Found short HnS clip as stinger: {clip.name} ({clip.length:F1}s)");
                return _hnsTimeToHideClip;
            }
        }

        // Info("[Draft] No time-to-hide clip found. Available HnS clips:");
        /*
        foreach (var obj in allClips)
        {
            var clip = obj.Cast<AudioClip>();
            string n = clip.name.ToLower();
            if (n.Contains("hns") || n.Contains("hide") || n.Contains("seek"))
                // Info($"[Draft]   - {clip.name} ({clip.length:F1}s)");
        }
        */

        return null;
    }

    // === LOBBY LOCK/UNLOCK ===

    private static void LockLobby()
    {
        if (_lobbyLocked) return;
        if (!AmongUsClient.Instance.AmHost) return;

        try
        {
            var lockEnabled = OptionGroupSingleton<DraftModeOptions>.Instance.LockLobbyDuringDraft.Value;
            if (!lockEnabled) return;
        }
        catch { return; }

        _lobbyWasPublic = AmongUsClient.Instance.IsGamePublic;
        if (_lobbyWasPublic)
            AmongUsClient.Instance.ChangeGamePublic(false);

        _lobbyLocked = true;
        // Info("[Draft] Lobby locked - no new players can join during draft.");
    }

    private static void UnlockLobby()
    {
        if (!_lobbyLocked) return;
        if (!AmongUsClient.Instance.AmHost) { _lobbyLocked = false; return; }

        if (_lobbyWasPublic)
            AmongUsClient.Instance.ChangeGamePublic(true);

        _lobbyLocked = false;
        // Info("[Draft] Lobby unlocked.");
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.MakePublic))]
    [HarmonyPrefix]
    public static bool PreventMakePublicDuringDraft()
    {
        return !_lobbyLocked;
    }

    // === BLOCK JOINS DURING DRAFT ===

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
    [HarmonyPostfix]
    public static void BlockJoinDuringDraft(AmongUsClient __instance, [HarmonyArgument(0)] InnerNet.ClientData client)
    {
        if (!_draftInProgress && !_draftCompletedWaitingForStart) return;
        if (!__instance.AmHost) return;
        if (client == null) return;

        Info($"[Draft] Kicking player {client.PlayerName} (ID: {client.Id}) - draft/countdown in progress.");

        Coroutines.Start(CoKickNextFrame(__instance, client.Id));
    }

    private static System.Collections.IEnumerator CoKickNextFrame(AmongUsClient client, int clientId)
    {
        yield return null;
        try { client.KickPlayer(clientId, false); }
        catch (System.Exception ex) { Warning($"[Draft] Failed to kick: {ex.Message}"); }
    }

    // === SETUP ===

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
    [HarmonyPostfix]
    public static void GameStartManagerStart()
    {
        _draftInProgress = false;
        _countdownWasActive = false;
        _draftCompletedWaitingForStart = false;
        _alertPlayed = false;
        _lobbyLocked = false;
        _lastAlertedPicker = null;

        _currentDangerLevel = -1;
        _dangerClips = null;
        _countdownTickClip = null;
        _hnsTimeToHideClip = null;
        _clipsLoading = false;

        _isCrossfading = false;
        _crossfadeTimer = 0f;
        _usingSourceA = true;

        _finalDangerTriggered = false;
        _baseDangerClipIndex = 0;
        _finalDangerClipIndex = 0;

        _disconnectedDuringDraft.Clear();
        _originalPickOrder.Clear();
        _tooltipText = null;
        CleanupUI();

        Coroutines.Start(CoPreloadHnsSounds());
    }

    private static System.Collections.IEnumerator CoPreloadHnsSounds()
    {
        var dangerNames = new List<string>();
        for (int i = 1; i <= 6; i++)
        {
            dangerNames.Add($"hns_danger_lvl{i}");
            dangerNames.Add($"hns_danger_lvl{i}.mp3");
            dangerNames.Add($"hns_danger_lvl{i}.wav");
            dangerNames.Add($"hns_danger_lvl{i}.ogg");
            dangerNames.Add($"HnS_Danger_Lvl{i}");
            dangerNames.Add($"hns_danger_level_{i}");
            dangerNames.Add($"hns_danger_level{i}");
            dangerNames.Add($"Assets/Audio/HnS/hns_danger_lvl{i}");
            dangerNames.Add($"Assets/Audio/HnS/hns_danger_lvl{i}.mp3");
        }

        var hideNames = new List<string>
        {
            "hns_stinger", "hns_stinger.mp3", "hns_stinger.wav",
            "hns_time_to_hide", "hns_time_to_hide.mp3",
            "hns_round_start", "hns_round_start.mp3",
            "hns_go_hide", "hns_go_hide.mp3",
            "hns_hide_start", "hns_hide_start.mp3",
            "HnS_Stinger", "HnS_TimeToHide", "HnS_RoundStart",
            "Assets/Audio/HnS/hns_stinger",
            "Assets/Audio/HnS/hns_stinger.mp3",
            "Assets/Audio/HnS/hns_time_to_hide",
            "Assets/Audio/HnS/hns_time_to_hide.mp3",
            "hns_hiding_start", "hns_hiding_start.mp3",
            "hns_seek_start", "hns_seek_start.mp3",
            "hns_game_start", "hns_game_start.mp3",
            "hideandseek_stinger", "hideandseek_stinger.mp3",
            "hns_intro", "hns_intro.mp3",
            "hns_begin", "hns_begin.mp3",
        };

        var allNames = new List<string>();
        allNames.AddRange(dangerNames);
        allNames.AddRange(hideNames);

        int loaded = 0;
        foreach (var address in allNames)
        {
            var handle = Addressables.LoadAssetAsync<AudioClip>(address);
            yield return handle;

            try
            {
                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                {
                    loaded++;
                    // Info($"[Draft] Preloaded: {handle.Result.name}");
                }
            }
            catch { }
        }

        // Info($"[Draft] Preload complete. {loaded} clips loaded into memory.");

        FindTimeToHideClip();

        var allClips = Resources.FindObjectsOfTypeAll(Il2CppType.Of<AudioClip>());
        /*
        foreach (var obj in allClips)
        {
            var c = obj.Cast<AudioClip>();
            string n = c.name.ToLower();
            if (n.Contains("hns") || n.Contains("hide") || n.Contains("seek"))
                // Info($"[Draft] Available HnS clip: {c.name} ({c.length:F1}s)");
        }
        */
    }

    // === INTERCEPT COUNTDOWN ===

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
    [HarmonyPostfix]
    public static void GameStartManagerUpdate(GameStartManager __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!DraftSystem.IsEnabled) return;

        if (_draftCompletedWaitingForStart)
        {
            UpdateDraftUI(__instance);
            return;
        }

        if (__instance.startState == GameStartManager.StartingStates.Countdown && !_draftInProgress && !_countdownWasActive)
            _countdownWasActive = true;

        if (_countdownWasActive && !_draftInProgress && __instance.startState == GameStartManager.StartingStates.Countdown)
        {
            __instance.ResetStartState();
            _countdownWasActive = false;
            StartDraft(__instance);
        }

        if (_draftInProgress) UpdateDraftUI(__instance);
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Low)]
    public static void GameStartManagerUpdateClient(GameStartManager __instance)
    {
        if (AmongUsClient.Instance.AmHost) return;
        if (_draftCompletedWaitingForStart) { UpdateDraftUI(__instance); return; }
        if (_draftInProgress) UpdateDraftUI(__instance);
    }

    // === BLOCK START DURING DRAFT ===

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.BeginGame))]
    [HarmonyPrefix]
    public static bool BeginGamePrefix()
    {
        if (!DraftSystem.IsEnabled) return true;
        if (_draftInProgress) return false;
        if (_draftCompletedWaitingForStart) return true;
        return true;
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.BeginGame))]
    [HarmonyPostfix]
    public static void BeginGamePostfix(GameStartManager __instance)
    {
        if (DraftSystem.IsEnabled && _draftCompletedWaitingForStart)
        {
            // Inspired by DraftModeTOUM: Zero out the timer for an instant start
            __instance.countDownTimer = 0f;
            CleanupUI();
        }
    }

    // === START DRAFT (HOST) ===

    public static void StartDraft(GameStartManager gsm = null)
    {
        if (gsm == null) gsm = Object.FindObjectOfType<GameStartManager>();
        if (gsm == null) return;
        _draftInProgress = true;
        _draftCompletedWaitingForStart = false;
        _alertPlayed = false;
        _pickTimer = 0f;
        _lastAlertedPicker = null;
        _lastTimeLeftInt = -1;
        _forceUpdatePlayerList = true;

        DraftSystem.Reset();

        var options = OptionGroupSingleton<DraftModeOptions>.Instance;

        var impostorCount = GameOptionsManager.Instance.currentNormalGameOptions.NumImpostors;

        var allPlayers = new List<byte>();
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null && player.Data != null && !player.Data.Disconnected)
            {
                if (!TownOfUs.Roles.Other.SpectatorRole.TrackedSpectators.Contains(player.Data.PlayerName))
                {
                    allPlayers.Add(player.PlayerId);
                }
            }
        }

        if (allPlayers.Count == 0)
        {
            impostorCount = 0;
        }
        else
        {
            impostorCount = Mathf.Min(impostorCount, allPlayers.Count - 1);
            impostorCount = Mathf.Max(impostorCount, 1);
        }

        var shuffled = new List<byte>(allPlayers);
        shuffled.Shuffle();
        var impostors = new HashSet<byte>();
        for (int i = 0; i < impostorCount; i++)
            impostors.Add(shuffled[i]);

        DraftSystem.ImpostorPlayerIds = impostors;
        DraftSystem.GeneratePickOrder(allPlayers);
        _originalPickOrder.Clear();
        _originalPickOrder.AddRange(DraftSystem.PickOrder);
        DraftSystem.AssignFactions(allPlayers, impostors);
        // Info($"[Draft] PickOrder generated: {string.Join(",", DraftSystem.PickOrder)}");

        DraftNetworking.SendDraftStart(impostors);

        CreateDraftUI();
        LockLobby();
        UpdatePlayerList();
        ShowRoleButtonsForCurrentPicker();
    }

    public static void OnDraftStartedAsClient()
    {
        if (AmongUsClient.Instance.AmHost) return;

        _draftInProgress = true;
        _draftCompletedWaitingForStart = false;
        _alertPlayed = false;
        _pickTimer = 0f;
        _lastAlertedPicker = null;
        _lastTimeLeftInt = -1;
        _forceUpdatePlayerList = true;

        DraftSystem.DraftPicks.Clear();
        DraftSystem.AlreadyPicked.Clear();
        DraftSystem.LocalPlayerPicked = false;
        DraftSystem.CurrentOfferedRoles = null;
        DraftSystem.SelectedAlignment = null;

        CreateDraftUI();
        _originalPickOrder.Clear();
        _originalPickOrder.AddRange(DraftSystem.PickOrder);
        UpdatePlayerList();
        ShowRoleButtonsForCurrentPicker();
    }

    // === MUSIC ===

    private static bool TryExtractDangerLevel(string clipName, out int level)
    {
        level = 0;
        if (string.IsNullOrEmpty(clipName)) return false;

        string n = clipName.ToLowerInvariant();

        int idx = n.IndexOf("lvl", System.StringComparison.OrdinalIgnoreCase);
        int offset = 3;
        if (idx < 0)
        {
            idx = n.IndexOf("level", System.StringComparison.OrdinalIgnoreCase);
            offset = 5;
        }
        if (idx < 0) return false;

        idx += offset;

        int start = idx;
        while (idx < n.Length && System.Char.IsDigit(n[idx])) idx++;

        if (idx <= start) return false;

        var s = n.Substring(start, idx - start);
        return int.TryParse(s, out level);
    }

    private static void SortDangerClipsByLevel(AudioClip[] clips)
    {
    if (clips == null || clips.Length <= 1) return;

    System.Array.Sort(clips, (a, b) =>
    {
        int la = int.MaxValue;
        int lb = int.MaxValue;

        if (a != null && TryExtractDangerLevel(a.name, out var tla)) la = tla;
        if (b != null && TryExtractDangerLevel(b.name, out var tlb)) lb = tlb;

        int cmp = la.CompareTo(lb);
        if (cmp != 0) return cmp;

        return string.Compare(a?.name, b?.name, System.StringComparison.OrdinalIgnoreCase);
    });
    }

    private static int FindCrewmateCloseClipIndex(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return 0;
        for (int i = 0; i < clips.Length; i++)
        {
            var c = clips[i];
            if (c == null) continue;
            var n = c.name.ToLowerInvariant();
            if (n.Contains("close") && (n.Contains("hns") || n.Contains("danger") || n.Contains("impostor")))
                return i;
        }
        for (int i = 0; i < clips.Length; i++)
        {
            var c = clips[i];
            if (c == null) continue;
            if (TryExtractDangerLevel(c.name, out int lvl) && lvl == 1)
                return i;
        }
        return 0;
    }

    private static int FindHighestDangerLevelIndex(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return 0;

        int bestIdx = clips.Length - 1;
        int bestLvl = -1;

        for (int i = 0; i < clips.Length; i++)
        {
            var c = clips[i];
            if (c == null) continue;

            if (TryExtractDangerLevel(c.name, out int lvl))
            {
                if (lvl > bestLvl)
                {
                    bestLvl = lvl;
                    bestIdx = i;
                }
            }
        }

        return bestIdx;
    }

    private static void StartDraftMusic()
    {
        StopDraftMusic();

        _dangerClips = FindDangerClips();

        if (_dangerClips != null && _dangerClips.Length > 0)
        {
            StartMusicWithClips();
            return;
        }

        // Info("[Draft] Danger clips not in memory, loading from Addressables...");
        _clipsLoading = true;
        Coroutines.Start(CoLoadDangerClipsAndPlay());
    }

    private static System.Collections.IEnumerator CoLoadDangerClipsAndPlay()
    {
        var loaded = new List<AudioClip>();

        var namesToTry = new List<string>();
        for (int i = 1; i <= 6; i++)
        {
            namesToTry.Add($"hns_danger_lvl{i}");
            namesToTry.Add($"hns_danger_lvl{i}.mp3");
            namesToTry.Add($"hns_danger_lvl{i}.wav");
            namesToTry.Add($"hns_danger_lvl{i}.ogg");
            namesToTry.Add($"HnS_Danger_Lvl{i}");
            namesToTry.Add($"hns_danger_level_{i}");
            namesToTry.Add($"hns_danger_level{i}");
            namesToTry.Add($"Assets/Audio/HnS/hns_danger_lvl{i}");
            namesToTry.Add($"Assets/Audio/HnS/hns_danger_lvl{i}.mp3");
        }

        foreach (var address in namesToTry)
        {
            var handle = Addressables.LoadAssetAsync<AudioClip>(address);
            yield return handle;

            try
            {
                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                {
                    var clip = handle.Result;
                    if (!loaded.Exists(c => c.name == clip.name))
                    {
                        loaded.Add(clip);
                        // Info($"[Draft] Loaded via address '{address}': {clip.name}");
                    }
                }
            }
            catch { }
        }

        if (loaded.Count == 0)
        {
            // Info("[Draft] Addressables failed. Checking Resources again...");
            yield return null;

            var allClips = Resources.FindObjectsOfTypeAll(Il2CppType.Of<AudioClip>());
            foreach (var obj in allClips)
            {
                var c = obj.Cast<AudioClip>();
                if (c.name.IndexOf("danger", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    loaded.Add(c);
                    // Info($"[Draft] Found in Resources (retry): {c.name}");
                }
            }
        }

        _clipsLoading = false;

        if (loaded.Count == 0)
        {
            Warning("[Draft] Absolutely no music clips found. Draft will play without music.");
            yield break;
        }

        loaded.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
        _dangerClips = loaded.ToArray();
        // Info($"[Draft] Final music clips ({_dangerClips.Length}): {string.Join(", ", loaded.ConvertAll(c => c.name))}");

        StartMusicWithClips();
    }

    private static void StartMusicWithClips()
    {
        if (_dangerClips == null || _dangerClips.Length == 0) return;

        try
        {
            SortDangerClipsByLevel(_dangerClips);

            _baseDangerClipIndex = FindCrewmateCloseClipIndex(_dangerClips);
            _finalDangerClipIndex = FindHighestDangerLevelIndex(_dangerClips);
            _finalDangerTriggered = false;

            _totalDraftPickers = _originalPickOrder.Count;
            if (_totalDraftPickers == 0)
            {
                foreach (var p in PlayerControl.AllPlayerControls)
                    if (p != null && !p.Data.Disconnected &&
                        !TownOfUs.Roles.Other.SpectatorRole.TrackedSpectators.Contains(p.Data.PlayerName))
                        _totalDraftPickers++;
            }

            _currentDangerLevel = _baseDangerClipIndex;
            _usingSourceA = true;
            _isCrossfading = false;

            var obj = new GameObject("DraftMusic");
            Object.DontDestroyOnLoad(obj);

            _draftMusicSourceA = obj.AddComponent<AudioSource>();
            _draftMusicSourceA.loop = true;
            _draftMusicSourceA.playOnAwake = false;
            _draftMusicSourceA.spatialBlend = 0f;
            _draftMusicSourceA.volume = _musicVolume;
            _draftMusicSourceA.pitch = _musicPitch;
            _draftMusicSourceA.mute = _isMusicMuted;
            _draftMusicSourceA.clip = _dangerClips[_baseDangerClipIndex];
            _draftMusicSourceA.Play();

            _draftMusicSourceB = obj.AddComponent<AudioSource>();
            _draftMusicSourceB.loop = true;
            _draftMusicSourceB.playOnAwake = false;
            _draftMusicSourceB.spatialBlend = 0f;
            _draftMusicSourceB.volume = 0f;
            _draftMusicSourceB.pitch = _musicPitch;
            _draftMusicSourceB.mute = _isMusicMuted;

            // Info($"[Draft] Music started: base='{_dangerClips[_baseDangerClipIndex]?.name}', final='{_dangerClips[_finalDangerClipIndex]?.name}', clips={_dangerClips.Length}");
        }
        catch (System.Exception ex)
        {
            Warning($"[Draft] Failed to start music: {ex.Message}");
        }
    }

    private static void CrossfadeToClip(AudioClip newClip)
    {
        if (newClip == null) return;

        var current = ActiveSource;
        var next = InactiveSource;
        if (next == null) return;

        try { next.Stop(); } catch { }

        float syncTime = 0f;
        if (current != null && current.clip != null && current.isPlaying && current.clip.length > 0.01f)
        {
            float progress = current.time / current.clip.length; // 0..1
            syncTime = progress * newClip.length;
            syncTime = Mathf.Clamp(syncTime, 0f, Mathf.Max(0f, newClip.length - 0.01f));
        }

        next.clip = newClip;
        next.pitch = _musicPitch;
        next.volume = 0f;
        next.mute = _isMusicMuted;

        try { next.time = syncTime; } catch { }
        next.Play();

        _crossfadeFromVol = current != null ? current.volume : _musicVolume;
        _crossfadeToVol = _musicVolume;

        _isCrossfading = true;
        _crossfadeTimer = 0f;

        _usingSourceA = !_usingSourceA;

        // Info($"[Draft] Crossfade -> {newClip.name} (sync {syncTime:F2}s)");
    }

    private static void UpdateCrossfade()
    {
        if (!_isCrossfading) return;
        if (_draftMusicSourceA == null || _draftMusicSourceB == null) return;

        _crossfadeTimer += Time.deltaTime;
        float t = Mathf.Clamp01(_crossfadeTimer / _crossfadeDuration);
        float eased = t * t * (3f - 2f * t);

        var active = ActiveSource;
        var inactive = InactiveSource;

        if (active != null)
            active.volume = Mathf.Lerp(0f, _crossfadeToVol, eased);

        if (inactive != null)
            inactive.volume = Mathf.Lerp(_crossfadeFromVol, 0f, eased);

        if (t >= 1f)
        {
            _isCrossfading = false;

            if (inactive != null)
            {
                inactive.Stop();
                inactive.volume = 0f;
                inactive.clip = null;
            }

            if (active != null)
            {
                active.volume = _musicVolume;
                active.pitch = _musicPitch;
            }
        }
    }

    private static void UpdateDraftMusicIntensity()
    {
        if (_draftMusicSourceA == null || _dangerClips == null || _dangerClips.Length == 0) return;
        if (!_draftInProgress) return;

        UpdateCrossfade();
        if (_draftMusicSourceA != null) _draftMusicSourceA.pitch = _musicPitch;
        if (_draftMusicSourceB != null) _draftMusicSourceB.pitch = _musicPitch;

        if (_finalDangerTriggered) return;

        int remaining = DraftSystem.PickOrder.Count;

        if (remaining <= 2 && !_isCrossfading && _finalDangerClipIndex != _baseDangerClipIndex)
        {
            _finalDangerTriggered = true;
            _currentDangerLevel = _finalDangerClipIndex;
            CrossfadeToClip(_dangerClips[_finalDangerClipIndex]);

            // Info($"[Draft] Final danger music triggered (remaining={remaining})");
        }
    }

    private static void StopDraftMusic()
    {
        if (_draftMusicSourceA != null || _draftMusicSourceB != null)
        {
            GameObject musicObj = null;
            if (_draftMusicSourceA != null) musicObj = _draftMusicSourceA.gameObject;
            else if (_draftMusicSourceB != null) musicObj = _draftMusicSourceB.gameObject;

            try { if (_draftMusicSourceA != null) { _draftMusicSourceA.Stop(); _draftMusicSourceA.clip = null; } } catch { }
            try { if (_draftMusicSourceB != null) { _draftMusicSourceB.Stop(); _draftMusicSourceB.clip = null; } } catch { }

            if (musicObj != null) Object.Destroy(musicObj);

            _draftMusicSourceA = null;
            _draftMusicSourceB = null;
        }

        _isCrossfading = false;
        _crossfadeTimer = 0f;

        _currentDangerLevel = -1;
        _clipsLoading = false;

        _finalDangerTriggered = false;
    }

    private static void PlayStartAlert()
    {
        var clip = FindTimeToHideClip();
        if (clip != null)
        {
            try
            {
                SoundManager.Instance.PlaySoundImmediate(clip, false, 0.8f, 1f, SoundManager.Instance.SfxChannel);
                return;
            }
            catch { }
        }

        try
        {
            var fallback = TouExtensionAudio.DraftStartAlert.LoadAsset();
            if (fallback != null)
                SoundManager.Instance.PlaySoundImmediate(fallback, false, 0.5f, 1f, SoundManager.Instance.SfxChannel);
        }
        catch { }
    }

    private static void PlayDraftCompleteSound()
    {
        var clip = FindTimeToHideClip();
        if (clip != null)
        {
            try
            {
                SoundManager.Instance.PlaySoundImmediate(clip, false, 0.8f, 1f, SoundManager.Instance.SfxChannel);
                // Info($"[Draft] Played complete sound: {clip.name}");
                return;
            }
            catch { }
        }

        // Info("[Draft] No HnS stinger found, using draft alert.");
        PlayStartAlert();
    }

    private static void TryPlayPickerAlert()
    {
        if (!_draftInProgress) return;
        if (DraftSystem.PickOrder.Count == 0) return;

        var picker = DraftSystem.CurrentPicker;
        if (!picker.HasValue) return;

        if (_lastAlertedPicker.HasValue && _lastAlertedPicker.Value == picker.Value)
            return;

        _lastAlertedPicker = picker.Value;

        try
        {
            var clip = TouExtensionAudio.DraftAlertSound.LoadAsset();
            if (clip == null) return;

            bool amPicker = PlayerControl.LocalPlayer != null &&
                            PlayerControl.LocalPlayer.PlayerId == picker.Value;

            if (amPicker)
                SoundManager.Instance.PlaySoundImmediate(clip, false, 1f, 1f, SoundManager.Instance.SfxChannel);
            else
                SoundManager.Instance.PlaySoundImmediate(clip, false, 0.25f, 1f, SoundManager.Instance.SfxChannel);
        }
        catch { }
    }

    // === UI CREATION ===

    private static void CreateDraftUI()
    {
        CleanupUI();

        _overlayBackground = new GameObject("DraftOverlay");
        _overlayBackground.transform.SetParent(HudManager.Instance.transform);
        _overlayBackground.transform.localPosition = new Vector3(0f, 0f, -500f);
        _overlayBackground.layer = LayerMask.NameToLayer("UI");

        var overlayRenderer = _overlayBackground.AddComponent<SpriteRenderer>();
        overlayRenderer.sprite = TouExtensionAssets.DraftBackground.LoadAsset();
        overlayRenderer.color = Color.white; 
        overlayRenderer.sortingOrder = 5;

        var cam = Camera.main;
        if (cam != null)
        {
            float camH = cam.orthographicSize * 2f;
            float camW = camH * cam.aspect;
            var bounds = overlayRenderer.sprite.bounds.size;
            float scale = Mathf.Max(camW / bounds.x, camH / bounds.y) * 1.05f;
            _overlayBackground.transform.localScale = new Vector3(scale, scale, 1f);
        }
        else
        {
            _overlayBackground.transform.localScale = new Vector3(12f, 12f, 1f);
        }

        try { SoundManager.Instance.StopAllSound(); } catch { }

        if (!AmIFirstPicker())
            PlayStartAlert();

        StartDraftMusic();
        HideLobby(true);

        _draftContainer = new GameObject("DraftContainer");
        _draftContainer.transform.SetParent(HudManager.Instance.transform);
        _draftContainer.transform.localPosition = Vector3.zero;
        _draftContainer.layer = LayerMask.NameToLayer("UI");

        var titleText = CreateTMP("DraftTitle", _draftContainer.transform,
            new Vector3(2.23f, 2.15f, -510f), 1.8f, TextAlignmentOptions.Center, true);
        
        _titleRandomOffset = UnityEngine.Random.Range(0f, 1000f);
        bool startWithHeker = ((int)((Time.time + _titleRandomOffset) / 20f) % 2) == 0;
        titleText.text = $"<size=130%><b>DRAFT MODE</b></size>\nBY {(startWithHeker ? "HEKER" : "MARZECOOO")}";
        _draftTitleText = titleText;
        if (!_isTitleAnimRunning) Coroutines.Start(CoAnimateDraftTitle());

        _timerText = CreateTMP("DraftTimer", _draftContainer.transform,
            new Vector3(2.3f, 1.55f, -510f), 2.1f, TextAlignmentOptions.Center, true);

        _draftCompleteText = CreateTMP("DraftCompleteText", _draftContainer.transform,
            new Vector3(2.23f, 0.5f, -510f), 2.4f, TextAlignmentOptions.Center, true);
        _draftCompleteText.gameObject.SetActive(false);

        _playerListText = CreateTMP("DraftPlayerList", _draftContainer.transform,
            new Vector3(-2.4f, -0.3f, -510f), 1.8f, TextAlignmentOptions.TopLeft, true);
        _playerListText.rectTransform.sizeDelta = new Vector2(3.5f, 6f);
        _playerListText.enableWordWrapping = false;
        _playerListText.overflowMode = TextOverflowModes.Truncate;

        CreateMuteButton();
        CreateCancelButton();
        CreateForceStartButton();

        _tooltipText = CreateTMP("DraftTooltip", _draftContainer.transform,
            new Vector3(2.23f, -2.1f, -510f), 1.0f, TextAlignmentOptions.Top, true);
        _tooltipText.rectTransform.sizeDelta = new Vector2(3.8f, 2f);
        _tooltipText.enableWordWrapping = true;
        _tooltipText.text = "";
        _tooltipText.color = new Color(0.9f, 0.9f, 0.9f, 1f);

        Coroutines.Start(FadeInUI());
        Coroutines.Start(FadeInBackground());
    }

    private static string GetRoleDescription(RoleBehaviour role)
    {
        if (role == null) return null;
        try
        {
            var prop = role.GetType().GetProperty("RoleDescription");
            if (prop != null)
            {
                var val = prop.GetValue(role);
                if (val is string str) return str;
            }
        }
        catch { }
        return null;
    }

    private static void OpenWikiForRole(RoleBehaviour role)
    {
    }

    // === FADE ANIMATIONS ===

    private static System.Collections.IEnumerator FadeInUI()
    {
        if (_draftContainer == null) yield break;

        float duration = 1.1f;
        float elapsed = 0f;

        var renderers = _draftContainer.GetComponentsInChildren<SpriteRenderer>(true);
        var tmpComponents = _draftContainer.GetComponentsInChildren<TextMeshPro>(true);

        var rendererColors = new Color[renderers.Length];
        var tmpColors = new Color[tmpComponents.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            rendererColors[i] = renderers[i].color;
            renderers[i].color = new Color(rendererColors[i].r, rendererColors[i].g, rendererColors[i].b, 0f);
        }

        for (int i = 0; i < tmpComponents.Length; i++)
        {
            tmpColors[i] = tmpComponents[i].color;
            tmpComponents[i].color = new Color(tmpColors[i].r, tmpColors[i].g, tmpColors[i].b, 0f);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - (1f - t) * (1f - t);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].color = new Color(
                    rendererColors[i].r,
                    rendererColors[i].g,
                    rendererColors[i].b,
                    rendererColors[i].a * eased);
            }

            for (int i = 0; i < tmpComponents.Length; i++)
            {
                if (tmpComponents[i] == null) continue;
                tmpComponents[i].color = new Color(
                    tmpColors[i].r,
                    tmpColors[i].g,
                    tmpColors[i].b,
                    tmpColors[i].a * eased);
            }

            yield return null;
        }

        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].color = rendererColors[i];
        for (int i = 0; i < tmpComponents.Length; i++)
            if (tmpComponents[i] != null) tmpComponents[i].color = tmpColors[i];
    }

    private static System.Collections.IEnumerator FadeInBackground()
    {
        if (_overlayBackground == null) yield break;
        var sr = _overlayBackground.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        var targetColor = sr.color;
        sr.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);

        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - (1f - t) * (1f - t);
            sr.color = new Color(targetColor.r, targetColor.g, targetColor.b, targetColor.a * eased);
            yield return null;
        }

        sr.color = targetColor;
    }

    private static System.Collections.IEnumerator CoFadeOutRoleButtons(float duration = 1.0f)
    {
        float elapsed = 0f;

        var buttonData = new List<(GameObject obj, Vector3 velocity, float rotSpeed, List<(SpriteRenderer sr, Color orig)> srs, List<(TextMeshPro tmp, Color orig)> tmps)>();

        foreach (var obj in _roleButtonObjects)
        {
            if (obj == null) continue;
            
            var srs = new List<(SpriteRenderer, Color)>();
            foreach (var sr in obj.GetComponentsInChildren<SpriteRenderer>(true)) srs.Add((sr, sr.color));
            
            var tmps = new List<(TextMeshPro, Color)>();
            foreach (var tmp in obj.GetComponentsInChildren<TextMeshPro>(true)) tmps.Add((tmp, tmp.color));

            // Random velocity away from the right side center
            Vector2 dir = (new Vector2(obj.transform.localPosition.x, obj.transform.localPosition.y) - new Vector2(2.2f, 0f)).normalized;
            if (dir.magnitude < 0.1f) dir = Vector2.right;
            Vector3 vel = (Vector3)dir * UnityEngine.Random.Range(2f, 4f);
            float rot = UnityEngine.Random.Range(-90f, 90f);

            buttonData.Add((obj, vel, rot, srs, tmps));
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f); // Out Cubic
            float alpha = 1f - t;

            foreach (var data in buttonData)
            {
                if (data.obj == null) continue;
                
                data.obj.transform.localPosition += data.velocity * Time.deltaTime;
                data.obj.transform.localRotation *= Quaternion.Euler(0, 0, data.rotSpeed * Time.deltaTime);
                data.obj.transform.localScale = Vector3.one * (1f - t * 0.3f);

                foreach (var (sr, orig) in data.srs)
                    if (sr != null) sr.color = new Color(orig.r, orig.g, orig.b, orig.a * alpha);
                foreach (var (tmp, orig) in data.tmps)
                    if (tmp != null) tmp.color = new Color(orig.r, orig.g, orig.b, orig.a * alpha);
            }

            yield return null;
        }

        foreach (var data in buttonData)
            if (data.obj != null) Object.Destroy(data.obj);
            
        _roleButtonObjects.Clear();
        _buttonRefs.Clear();
        _pickLocked = false;
    }

    private static System.Collections.IEnumerator CoAnimateDraftComplete()
    {
        if (_draftCompleteText == null) yield break;

        _draftCompleteText.gameObject.SetActive(true);

        float duration = 0.6f;
        float elapsed = 0f;

        var origColor = _draftCompleteText.color;
        _draftCompleteText.color = new Color(origColor.r, origColor.g, origColor.b, 0f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Smooth overshoot curve
            float eased;
            if (t < 0.5f)
            {
                float t2 = t / 0.5f;
                eased = t2 * t2 * (3f - 2f * t2) * 1.15f;
            }
            else
            {
                float t2 = (t - 0.5f) / 0.5f;
                eased = Mathf.Lerp(1.15f, 1f, 1f - Mathf.Pow(1f - t2, 3f));
            }

            float scale = Mathf.Lerp(0.5f, 1f, eased); // Start from small
            float alpha = Mathf.Clamp01(t * 2f);

            _draftCompleteText.transform.localScale = new Vector3(scale, scale, 1f);
            _draftCompleteText.color = new Color(origColor.r, origColor.g, origColor.b, alpha);

            yield return null;
        }

        _draftCompleteText.transform.localScale = Vector3.one;
        _draftCompleteText.color = origColor;
    }

    private static System.Collections.IEnumerator CoAnimateTimerIn()
    {
        if (_timerText == null) yield break;

        float duration = 0.45f;
        float elapsed = 0f;

        var origColor = _timerText.color;
        _timerText.color = new Color(origColor.r, origColor.g, origColor.b, 0f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Overshoot bounce
            float eased = t < 0.6f 
                ? Mathf.Lerp(0f, 1.15f, 1f - Mathf.Pow(1f - (t / 0.6f), 3f))
                : Mathf.Lerp(1.15f, 1f, (t - 0.6f) / 0.4f);

            _timerText.transform.localScale = new Vector3(eased, eased, 1f);
            _timerText.color = new Color(origColor.r, origColor.g, origColor.b, Mathf.Clamp01(t * 3f));

            yield return null;
        }

        _timerText.transform.localScale = Vector3.one;
        _timerText.color = origColor;
    }

    private static System.Collections.IEnumerator CoFadeOutMusicAndPlayComplete()
    {
        float fadeDuration = 1.5f;
        float elapsed = 0f;

        float startVolumeA = _draftMusicSourceA != null ? _draftMusicSourceA.volume : 0f;
        float startVolumeB = _draftMusicSourceB != null ? _draftMusicSourceB.volume : 0f;
        float startPitchA = _draftMusicSourceA != null ? _draftMusicSourceA.pitch : 1f;
        float startPitchB = _draftMusicSourceB != null ? _draftMusicSourceB.pitch : 1f;

        bool hasMusic = _draftMusicSourceA != null || _draftMusicSourceB != null;

        if (hasMusic)
        {
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                float eased = 1f - (1f - t) * (1f - t);
                float fadeMultiplier = 1f - eased;

                if (_draftMusicSourceA != null)
                {
                    _draftMusicSourceA.volume = startVolumeA * fadeMultiplier;
                    _draftMusicSourceA.pitch = Mathf.Lerp(startPitchA, startPitchA * 0.85f, eased);
                }
                if (_draftMusicSourceB != null)
                {
                    _draftMusicSourceB.volume = startVolumeB * fadeMultiplier;
                    _draftMusicSourceB.pitch = Mathf.Lerp(startPitchB, startPitchB * 0.85f, eased);
                }

                yield return null;
            }
        }

        StopDraftMusic();

        yield return new WaitForSeconds(0.15f);

        PlayDraftCompleteSound();
    }

    // === MUTE BUTTON ===

    private static void CreateMuteButton()
    {
        if (_draftContainer == null) return;

        _muteButtonObj = new GameObject("MuteButton");
        _muteButtonObj.transform.SetParent(_draftContainer.transform);
        _muteButtonObj.transform.localPosition = new Vector3(1.02f, 2.4f, -510f);
        _muteButtonObj.layer = LayerMask.NameToLayer("UI");

        var bgObj = new GameObject("MuteBG");
        bgObj.transform.SetParent(_muteButtonObj.transform);
        bgObj.transform.localPosition = new Vector3(0f, 0f, 0f);
        bgObj.layer = LayerMask.NameToLayer("UI");

        var bgRenderer = bgObj.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = CreateRoundedSprite();
        bgRenderer.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        bgRenderer.sortingOrder = 15;
        bgObj.transform.localScale = new Vector3(0.28f, 0.22f, 1f);

        var textObj = new GameObject("MuteText");
        textObj.transform.SetParent(_muteButtonObj.transform);
        textObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        textObj.layer = LayerMask.NameToLayer("UI");

        var text = textObj.AddComponent<TextMeshPro>();
        text.text = _isMusicMuted ? "UNMUTE" : "MUTE";
        text.fontSize = 1.2f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = _isMusicMuted ? Color.red : Color.green;
        text.sortingOrder = 20;
        ApplyFont(text);

        var collider = _muteButtonObj.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.9f, 0.45f);

        var btn = _muteButtonObj.AddComponent<PassiveButton>();
        btn.Colliders = new[] { (Collider2D)collider };
        btn.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();

        btn.OnClick.AddListener((System.Action)(() =>
        {
            _isMusicMuted = !_isMusicMuted;
            if (_draftMusicSourceA != null) _draftMusicSourceA.mute = _isMusicMuted;
            if (_draftMusicSourceB != null) _draftMusicSourceB.mute = _isMusicMuted;
            text.text = _isMusicMuted ? "UNMUTE" : "MUTE";
            text.color = _isMusicMuted ? Color.red : Color.green;
        }));

        btn.OnMouseOver = new UnityEngine.Events.UnityEvent();
        btn.OnMouseOver.AddListener((System.Action)(() => 
        { 
            bgRenderer.color = new Color(0.4f, 0.4f, 0.4f, 0.8f); 
            PlayHoverSound();
        }));

        btn.OnMouseOut = new UnityEngine.Events.UnityEvent();
        btn.OnMouseOut.AddListener((System.Action)(() => { bgRenderer.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); }));
    }

    private static void CreateCancelButton()
    {
        if (_draftContainer == null) return;
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

        _cancelButtonObj = new GameObject("CancelDraftButton");
        _cancelButtonObj.transform.SetParent(_draftContainer.transform);
        _cancelButtonObj.transform.localPosition = new Vector3(3.58f, 2.40f, -510f);
        _cancelButtonObj.layer = LayerMask.NameToLayer("UI");

        var bgObj = new GameObject("CancelBG");
        bgObj.transform.SetParent(_cancelButtonObj.transform);
        bgObj.transform.localPosition = new Vector3(0f, 0f, 0f);
        bgObj.layer = LayerMask.NameToLayer("UI");

        var bgRenderer = bgObj.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = CreateRoundedSprite();
        bgRenderer.color = new Color(0.48f, 0.12f, 0.12f, 0.95f);
        bgRenderer.sortingOrder = 15;
        bgObj.transform.localScale = new Vector3(0.24f, 0.19f, 1f); // krĂłtszy

        var textObj = new GameObject("CancelText");
        textObj.transform.SetParent(_cancelButtonObj.transform);
        textObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        textObj.layer = LayerMask.NameToLayer("UI");

        var text = textObj.AddComponent<TextMeshPro>();
        text.text = "CANCEL";
        text.fontSize = 0.9f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.sortingOrder = 20;
        ApplyFont(text);

        var collider = _cancelButtonObj.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.74f, 0.36f); // krĂłtszy hitbox

        var btn = _cancelButtonObj.AddComponent<PassiveButton>();
        btn.Colliders = new[] { (Collider2D)collider };
        btn.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();

        var normalColor = new Color(0.48f, 0.12f, 0.12f, 0.95f);
        var hoverColor = new Color(0.78f, 0.55f, 0.50f, 1f);

        btn.OnClick.AddListener((System.Action)(() =>
        {
            try
            {
                DraftNetworking.SendDraftCancel();
            }
            catch (System.Exception ex)
            {
                Warning($"[Draft] Failed to cancel draft: {ex.Message}");
            }
        }));

        btn.OnMouseOver = new UnityEngine.Events.UnityEvent();
        btn.OnMouseOver.AddListener((System.Action)(() =>
        {
            if (bgRenderer != null)
                bgRenderer.color = hoverColor;
            PlayHoverSound();
        }));

        btn.OnMouseOut = new UnityEngine.Events.UnityEvent();
        btn.OnMouseOut.AddListener((System.Action)(() =>
        {
            if (bgRenderer != null)
                bgRenderer.color = normalColor;
        }));
    }

    private static void CreateForceStartButton()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (_draftContainer == null) return;

        _forceStartButtonObj = new GameObject("ForceStartButton");
        _forceStartButtonObj.transform.SetParent(_draftContainer.transform);
        _forceStartButtonObj.transform.localPosition = new Vector3(3.58f, 2.00f, -510f);
        _forceStartButtonObj.layer = LayerMask.NameToLayer("UI");

        var bgObj = new GameObject("ForceStartBG");
        bgObj.transform.SetParent(_forceStartButtonObj.transform);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.layer = LayerMask.NameToLayer("UI");

        var bgRenderer = bgObj.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = CreateRoundedSprite();
        bgRenderer.color = new Color(0.1f, 0.4f, 0.1f, 0.95f);
        bgRenderer.sortingOrder = 15;
        bgObj.transform.localScale = new Vector3(0.24f, 0.19f, 1f);

        var textObj = new GameObject("ForceStartText");
        textObj.transform.SetParent(_forceStartButtonObj.transform);
        textObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        textObj.layer = LayerMask.NameToLayer("UI");

        var text = textObj.AddComponent<TextMeshPro>();
        text.text = "START";
        text.fontSize = 0.9f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.sortingOrder = 20;
        ApplyFont(text);

        var collider = _forceStartButtonObj.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.74f, 0.36f);

        var btn = _forceStartButtonObj.AddComponent<PassiveButton>();
        btn.Colliders = new[] { (Collider2D)collider };
        btn.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();

        var normalColor = new Color(0.1f, 0.4f, 0.1f, 0.95f);
        var hoverColor = new Color(0.2f, 0.7f, 0.2f, 1f);

        btn.OnClick.AddListener((System.Action)(() =>
        {
            var gsm = Object.FindObjectOfType<GameStartManager>();
            if (gsm == null) return;
            if (gsm.StartButton != null) gsm.StartButton.gameObject.SetActive(true);
            if (gsm.GameStartText != null) gsm.GameStartText.gameObject.SetActive(true);
            gsm.ReallyBegin(false);
        }));

        btn.OnMouseOver = new UnityEngine.Events.UnityEvent();
        btn.OnMouseOver.AddListener((System.Action)(() => 
        { 
            bgRenderer.color = hoverColor; 
            PlayHoverSound();
        }));

        btn.OnMouseOut = new UnityEngine.Events.UnityEvent();
        btn.OnMouseOut.AddListener((System.Action)(() => { bgRenderer.color = normalColor; }));

        _forceStartButtonObj.SetActive(false);
    }

    public static void ForceCancelDraft()
    {
        CleanupUI();
        UnlockLobby();
        HideLobby(false);

        _draftInProgress = false;
        _draftCompletedWaitingForStart = false;
        _countdownWasActive = false;
        _pickTimer = 0f;
        _alertPlayed = false;
        _pickLocked = false;
        _lastAlertedPicker = null;

        _disconnectedDuringDraft.Clear();
        _originalPickOrder.Clear();

        var gsm = Object.FindObjectOfType<GameStartManager>();
        if (gsm != null)
        {
            if (gsm.StartButton != null) gsm.StartButton.gameObject.SetActive(true);
            if (gsm.GameStartText != null) gsm.GameStartText.gameObject.SetActive(true);
        }
    }

    public static void ShowSystemMessage(string text)
    {
        if (HudManager.Instance == null || HudManager.Instance.Chat == null) return;
        var chat = HudManager.Instance.Chat;
        
        // Use a local player as base for cosmetics but we will override the name
        var player = PlayerControl.LocalPlayer;
        if (player == null) return;

        var pooledBubble = chat.GetPooledBubble();
        if (pooledBubble == null) return;

        pooledBubble.transform.SetParent(chat.scroller.Inner);
        pooledBubble.transform.localScale = Vector3.one;
        pooledBubble.SetLeft();
        pooledBubble.SetCosmetics(player.Data);
        
        pooledBubble.NameText.text = "<color=#FFD700>SYSTEM</color>";
        pooledBubble.NameText.color = Color.white;
        pooledBubble.votedMark.enabled = false;
        pooledBubble.Xmark.enabled = false;
        
        pooledBubble.TextArea.text = text;
        pooledBubble.TextArea.ForceMeshUpdate(true, true);
        
        float h = pooledBubble.NameText.GetNotDumbRenderedHeight() + pooledBubble.TextArea.GetNotDumbRenderedHeight() + 0.4f;
        pooledBubble.Background.size = new Vector2(5.52f, h);
        pooledBubble.MaskArea.size = new Vector2(5.52f, h - 0.05f);
        pooledBubble.AlignChildren();
        chat.AlignAllBubbles();

        if (chat is { IsOpenOrOpening: false, notificationRoutine: null })
            chat.notificationRoutine = chat.StartCoroutine(chat.BounceDot());
    }

    private static TextMeshPro CreateTMP(string name, Transform parent, Vector3 pos, float fontSize, TextAlignmentOptions align, bool withOutline)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.localPosition = pos;
        obj.layer = LayerMask.NameToLayer("UI");

        var tmp = obj.AddComponent<TextMeshPro>();
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.sortingOrder = 20;

        if (withOutline)
        {
            tmp.outlineWidth = 0.15f;
            tmp.outlineColor = Color.black;
        }

        ApplyFont(tmp);
        return tmp;
    }

    // === HIDE/SHOW LOBBY ===

    private static void HideLobby(bool hide)
    {
        var lobby = Object.FindObjectOfType<LobbyBehaviour>();
        if (lobby != null)
        {
            foreach (var r in lobby.GetComponentsInChildren<Renderer>(true))
                r.enabled = !hide;
        }

        var gsm = Object.FindObjectOfType<GameStartManager>();
        if (gsm != null)
        {
            if (gsm.StartButton != null) gsm.StartButton.gameObject.SetActive(!hide);
            if (gsm.GameStartText != null) gsm.GameStartText.gameObject.SetActive(!hide);
        }

        if (HudManager.Instance != null)
        {
            // Use transform.Find to avoid compilation errors with missing fields
            var namesToHide = new[] { "GameSettings", "RoleListRegion", "SetRoleList", "PlayerCounter", "LobbyButtons" };
            foreach (var name in namesToHide)
            {
                var t = HudManager.Instance.transform.Find(name);
                if (t != null) t.gameObject.SetActive(!hide);
            }
        }
    }

    // === PLAYER LIST ===

    private static void UpdatePlayerList()
    {
        if (_playerListText == null) return;

        var picker = DraftSystem.CurrentPicker;
        int timeLeftInt = -1;
        if (picker.HasValue)
        {
            float timeLeft = Mathf.Max(0, DraftSystem.TimeToChoose - _pickTimer);
            timeLeftInt = (int)timeLeft;
        }

        float dotTime = Time.time * 2.5f;
        int dotState = (int)dotTime % 3;
        if (!_forceUpdatePlayerList && picker.HasValue && timeLeftInt == _lastTimeLeftInt && dotState == _lastDotState)
            return;
        
        _playerListBuilder.Clear();
        int num = 1;

        foreach (var pid in _originalPickOrder)
        {
            bool isMe = pid == PlayerControl.LocalPlayer?.PlayerId;
            bool isPicker = DraftSystem.CurrentPicker.HasValue && DraftSystem.CurrentPicker.Value == pid;
            bool hasPicked = DraftSystem.DraftPicks.ContainsKey(pid);
            bool isDisconnected = _disconnectedDuringDraft.Contains(pid);

            string name = $"<b>Player {num}</b>";
            string prefix = isMe ? "<color=#AAAAFF>(YOU)</color>" : "";
            
            if (isDisconnected)
            {
                _playerListBuilder.Append(prefix).Append("<pos=12%><color=#FF4444><s>").Append(name).Append("</s></color><pos=35%>: <color=#FF4444>Disconnected</color>\n");
            }
            else if (hasPicked)
            {
                _playerListBuilder.Append(prefix).Append("<pos=12%><color=#888888>").Append(name).Append("</color><pos=35%>: <color=#44AA44>READY</color>\n");
            }
            else if (isPicker)
            {
                var timeLeft = Mathf.Max(0, DraftSystem.TimeToChoose - _pickTimer);
                var timerColor = timeLeft < 5f ? "#FF4444" : "#FFFF00";
                
                int dotCount = (int)(Time.time * 2.5f) % 3 + 1;
                string dots = new string('.', dotCount);
                
                float jump = Mathf.Abs(Mathf.Sin(Time.time * 10f)) * 0.15f;
                string voffset = $"<voffset={jump:F2}em>";

                _playerListBuilder.Append("<color=").Append(timerColor).Append(">").Append(voffset).Append(">>").Append((int)timeLeft).Append("</voffset></color>")
                                 .Append("<pos=12%><color=#FFFFFF>").Append(name).Append("</color><pos=35%>: <color=").Append(timerColor).Append(">PICKING").Append(dots).Append("</color>\n");
            }
            else
            {
                _playerListBuilder.Append(prefix).Append("<pos=12%><color=#BBBBBB>").Append(name).Append("</color><pos=35%>:\n");
            }
            num++;
        }

        _playerListText.text = _playerListBuilder.ToString();
    }

    private static string GetPlayerName(byte id)
    {
        foreach (var p in PlayerControl.AllPlayerControls)
            if (p != null && p.PlayerId == id) return p.Data.PlayerName;
        return $"Player {id}";
    }

    // === LOCK ROLE BUTTONS ===
    private static void LockRoleButtons(GameObject selected)
    {
        _pickLocked = true;
        
        // Immediate visual feedback: gray out all non-selected buttons instantly
        foreach (var obj in _roleButtonObjects)
        {
            if (obj != selected && obj != null && _buttonRefs.TryGetValue(obj, out var refs))
            {
                if (refs.BG != null) refs.BG.color = new Color(0.4f, 0.4f, 0.4f, 0.7f);
                if (refs.Border != null) refs.Border.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                if (refs.Label != null) refs.Label.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                if (refs.Icon != null) refs.Icon.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                if (refs.RandomIcon != null) refs.RandomIcon.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                
                if (refs.Icon != null) StartIconWobble(refs.Icon.gameObject, false);
                if (refs.RandomIcon != null) StartIconWobble(refs.RandomIcon.gameObject, false);
            }
        }

        Coroutines.Start(CoAnimateSelection(selected));
        
        if (_tooltipText != null)
        {
            _tooltipText.text = "";
        }
    }

    private static System.Collections.IEnumerator CoAnimateSelection(GameObject selected)
    {
        float duration = 0.5f;
        float elapsed = 0f;

        var others = new List<GameObject>();
        foreach (var obj in _roleButtonObjects)
        {
            if (obj != selected && obj != null) others.Add(obj);
        }

        // Store original data for the selected button
        ButtonRefs selectedRefs = null;
        Color selectedOrigBG = Color.white;
        if (selected != null && _buttonRefs.TryGetValue(selected, out selectedRefs))
        {
            selectedOrigBG = selectedRefs.BG.color;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t); // Smoothstep

            // 1. Animate Selected Button (Punch + Glow)
            if (selected != null && selectedRefs != null)
            {
                // Punch scale: 1.0 -> 1.2 -> 1.05
                float punch;
                if (t < 0.3f) punch = Mathf.Lerp(1.0f, 1.22f, t / 0.3f);
                else punch = Mathf.Lerp(1.22f, 1.06f, (t - 0.3f) / 0.7f);
                
                selected.transform.localScale = new Vector3(punch, punch, 1f);

                // Flash white at start
                if (t < 0.2f)
                {
                    float flash = 1f - (t / 0.2f);
                    selectedRefs.BG.color = Color.Lerp(selectedOrigBG, Color.white, flash);
                }
                else
                {
                    selectedRefs.BG.color = selectedOrigBG;
                }
                
                // Border pulse transition
                if (selectedRefs.Border != null)
                {
                    // Transition from white glow to solid gold
                    selectedRefs.Border.color = Color.Lerp(Color.white, new Color(1f, 0.9f, 0.2f, 1f), eased);
                }
            }

            // 2. Animate Others (Fade + Retreat + Gray)
            float otherAlpha = Mathf.Lerp(1f, 0.45f, t * 2f); // Fade to 45% alpha (was 20%)
            float otherScale = Mathf.Lerp(1f, 0.92f, t);    // Shrink slightly less
            
            foreach (var obj in others)
            {
                if (obj == null) continue;
                if (!_buttonRefs.TryGetValue(obj, out var refs)) continue;

                obj.transform.localScale = new Vector3(otherScale, otherScale, 1f);
                
                // Gray out (but keep it more readable)
                Color grayBase = new Color(0.25f, 0.25f, 0.25f, 1f);
                if (refs.BG != null) 
                {
                    Color targetBG = Color.Lerp(refs.BG.color, grayBase, t * 1.5f);
                    refs.BG.color = new Color(targetBG.r, targetBG.g, targetBG.b, otherAlpha);
                }
                
                if (refs.Border != null) refs.Border.color = new Color(0.15f, 0.15f, 0.15f, otherAlpha * 0.6f);
                if (refs.Label != null) refs.Label.color = new Color(0.4f, 0.4f, 0.4f, otherAlpha);
                if (refs.Icon != null) refs.Icon.color = new Color(0.4f, 0.4f, 0.4f, otherAlpha);
                if (refs.RandomIcon != null) refs.RandomIcon.color = new Color(0.4f, 0.4f, 0.4f, otherAlpha);
            }

            yield return null;
        }

        // Final state reinforcement
        if (selected != null && selectedRefs != null)
        {
            selected.transform.localScale = new Vector3(1.06f, 1.06f, 1f);
            if (selectedRefs.Border != null) selectedRefs.Border.color = new Color(1f, 0.9f, 0.2f, 1f);
        }
    }

    // === ROLE BUTTONS ===

    private static void ShowRoleButtonsForCurrentPicker()
    {
        ClearRoleButtons();
        _alertPlayed = false;
        _countdownSoundTimer = 1f;

        if (!DraftSystem.IsMyTurn) return;

        var myId = PlayerControl.LocalPlayer.PlayerId;
        var isImp = DraftSystem.ImpostorPlayerIds.Contains(myId);

        DraftSystem.CurrentOfferedRoles = DraftSystem.SelectRolesToOffer(isImp);
        var roles = DraftSystem.CurrentOfferedRoles;

        if (roles == null || roles.Count == 0) return;

        float xPos = 2.2f, startY = 1.0f, spacing = 0.8f;

        for (int i = 0; i < roles.Count; i++)
            CreateRoleButton(xPos, startY - i * spacing, roles[i], false, isImp);

        CreateRoleButton(xPos, startY - roles.Count * spacing - 0.4f, null, true, isImp);

        Coroutines.Start(AnimateButtonsIn());

        if (_timerText != null)
        {
            _timerText.gameObject.SetActive(true);
            Coroutines.Start(CoAnimateTimerIn());
        }
    }

    private static System.Collections.IEnumerator AnimateButtonsIn()
    {
        float staggerDelay = 0.07f;
        
        for (int i = 0; i < _roleButtonObjects.Count; i++)
        {
            var obj = _roleButtonObjects[i];
            if (obj == null) continue;
            
            obj.transform.localScale = Vector3.zero;
            Coroutines.Start(CoAnimateSingleButtonIn(obj, i * staggerDelay));
        }
        yield break;
    }

    private static System.Collections.IEnumerator CoAnimateSingleButtonIn(GameObject obj, float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);
        if (obj == null) yield break;

        float duration = 0.5f;
        float elapsed = 0f;

        // Find initial colors for fading
        var renderers = obj.GetComponentsInChildren<SpriteRenderer>(true);
        var tmps = obj.GetComponentsInChildren<TextMeshPro>(true);
        
        var rendererOrigColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) rendererOrigColors[i] = renderers[i].color;
        
        var tmpOrigColors = new Color[tmps.Length];
        for (int i = 0; i < tmps.Length; i++) tmpOrigColors[i] = tmps[i].color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Elastic out / Overshoot curve
            float eased;
            if (t < 0.6f)
            {
                float t2 = t / 0.6f;
                eased = 1.12f * (1f - Mathf.Pow(1f - t2, 3f));
            }
            else
            {
                float t2 = (t - 0.6f) / 0.4f;
                eased = Mathf.Lerp(1.12f, 1f, t2 * t2 * (3f - 2f * t2));
            }

            if (obj == null) yield break;
            obj.transform.localScale = new Vector3(eased, eased, 1f);
            
            float alpha = Mathf.Clamp01(t * 2.5f);
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].color = new Color(rendererOrigColors[i].r, rendererOrigColors[i].g, rendererOrigColors[i].b, rendererOrigColors[i].a * alpha);
            for (int i = 0; i < tmps.Length; i++)
                if (tmps[i] != null) tmps[i].color = new Color(tmpOrigColors[i].r, tmpOrigColors[i].g, tmpOrigColors[i].b, tmpOrigColors[i].a * alpha);

            yield return null;
        }

        if (obj != null)
        {
            obj.transform.localScale = Vector3.one;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].color = rendererOrigColors[i];
            for (int i = 0; i < tmps.Length; i++)
                if (tmps[i] != null) tmps[i].color = tmpOrigColors[i];
        }
    }

    private static void StartHoverAnimation(GameObject obj, float targetScale, float duration)
    {
        if (obj == null) return;
        
        // Prevent restarting the same animation every frame if OnMouseOver is called continuously
        if (_targetScales.TryGetValue(obj, out var currentTarget) && Mathf.Approximately(currentTarget, targetScale))
            return;

        _targetScales[obj] = targetScale;

        if (_hoverCoroutines.TryGetValue(obj, out var active))
        {
            if (active != null) Coroutines.Stop(active);
            _hoverCoroutines.Remove(obj);
        }
        _hoverCoroutines[obj] = Coroutines.Start(CoAnimateButtonHover(obj, targetScale, duration));
    }

    private static System.Collections.IEnumerator CoAnimateButtonHover(GameObject obj, float targetScale, float duration)
    {
        if (obj == null) yield break;
        Vector3 startScale = obj.transform.localScale;
        Vector3 endScale = new Vector3(targetScale, targetScale, 1f);
        float elapsed = 0f;

        // Find border for glow pulse
        SpriteRenderer border = null;
        if (_buttonRefs.TryGetValue(obj, out var refs)) border = refs.Border;

        while (elapsed < duration || (targetScale > 1.01f && !_pickLocked))
        {
            if (obj == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t); // Smoothstep
            
            if (elapsed <= duration)
                obj.transform.localScale = Vector3.Lerp(startScale, endScale, eased);
            
            // Pulsating glow if hovered
            if (targetScale > 1.01f && border != null && !_pickLocked)
            {
                float pulse = 0.7f + Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 0.3f;
                border.color = new Color(1f, 1f, 1f, pulse);
            }

            yield return null;
            if (elapsed > duration && targetScale <= 1.01f) break; 
        }
        if (obj != null) 
        {
            obj.transform.localScale = endScale;
            if (_targetScales.TryGetValue(obj, out var ts) && Mathf.Approximately(ts, targetScale))
                _targetScales.Remove(obj);
        }
        _hoverCoroutines.Remove(obj);
    }

    private static Texture2D _roundedButtonTex;

    private static Texture2D GetRoundedButtonTexture()
    {
        if (_roundedButtonTex != null) return _roundedButtonTex;

        int w = 1024, h = 256;
        int radius = 80;
        _roundedButtonTex = new Texture2D(w, h, TextureFormat.ARGB32, false);
        _roundedButtonTex.filterMode = FilterMode.Bilinear;
        _roundedButtonTex.wrapMode = TextureWrapMode.Clamp;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                float alpha = 1f;
                float cx = (x < radius) ? radius : (x > w - radius - 1) ? w - radius - 1 : x;
                float cy = (y < radius) ? radius : (y > h - radius - 1) ? h - radius - 1 : y;
                
                if (x < radius || x > w - radius - 1 || y < radius || y > h - radius - 1)
                {
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (dist > radius) alpha = 0f;
                    else if (dist > radius - 2.5f) alpha = Mathf.Clamp01(1f - (dist - (radius - 2.5f)) / 2.5f);
                }

                _roundedButtonTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        _roundedButtonTex.Apply();
        return _roundedButtonTex;
    }

    private static Sprite CreateRoundedSprite()
    {
        if (_cachedRoundedSprite != null) return _cachedRoundedSprite;
        var tex = GetRoundedButtonTexture();
        _cachedRoundedSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 400f);
        return _cachedRoundedSprite;
    }

    private static Sprite GetRoleIcon(RoleBehaviour role)
    {
        try
        {
            if (role is MiraAPI.Roles.ICustomRole cr && cr.Configuration.Icon != null)
                return cr.Configuration.Icon.LoadAsset();
            if (role?.RoleIconSolid != null)
                return role.RoleIconSolid;
        }
        catch { }
        return null;
    }

    private static void CreateRoleButton(float x, float y, RoleBehaviour role, bool isRandom, bool isImp)
    {
        if (_draftContainer == null) return;

        Color btnColor;
        if (isRandom) btnColor = new Color(0.4f, 0.4f, 0.5f);
        else if (role is MiraAPI.Roles.ICustomRole cr) btnColor = cr.RoleColor;
        else if (role.TeamType == RoleTeamTypes.Impostor) btnColor = Palette.ImpostorRed;
        else btnColor = Palette.CrewmateBlue;

        var container = new GameObject(isRandom ? "RandomButton" : "RoleButton");
        container.transform.SetParent(_draftContainer.transform);
        container.transform.localPosition = new Vector3(x, y, -510f);
        container.layer = LayerMask.NameToLayer("UI");

        var borderObj = new GameObject("Border");
        borderObj.transform.SetParent(container.transform);
        borderObj.transform.localPosition = Vector3.zero;
        borderObj.layer = LayerMask.NameToLayer("UI");

        var borderRenderer = borderObj.AddComponent<SpriteRenderer>();
        borderRenderer.sprite = CreateRoundedSprite();
        if (role != null && role.TeamType == RoleTeamTypes.Impostor)
            borderRenderer.color = new Color(0.3f, 0.02f, 0.02f, 1f); // Even darker blood red
        else
            borderRenderer.color = new Color(
                Mathf.Max(btnColor.r - 0.15f, 0f),
                Mathf.Max(btnColor.g - 0.15f, 0f),
                Mathf.Max(btnColor.b - 0.15f, 0f), 1f);
        borderRenderer.sortingOrder = 14;
        borderObj.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

        var bgObj = new GameObject("BG");
        bgObj.transform.SetParent(container.transform);
        bgObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        bgObj.layer = LayerMask.NameToLayer("UI");

        var bgRenderer = bgObj.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = CreateRoundedSprite();
        bgRenderer.color = btnColor;
        bgRenderer.sortingOrder = 15;
        bgObj.transform.localScale = new Vector3(1.0f, 0.95f, 1f);

        float normalizedScale = 0.45f;
        SpriteRenderer iconRenderer = null;
        if (!isRandom && role != null)
        {
            var iconSprite = GetRoleIcon(role);
            if (iconSprite != null)
            {
                var iconObj = new GameObject("RoleIcon");
                iconObj.transform.SetParent(container.transform);
                iconObj.transform.localPosition = new Vector3(-1.05f, 0f, -0.03f);
                iconObj.layer = LayerMask.NameToLayer("UI");

                iconRenderer = iconObj.AddComponent<SpriteRenderer>();
                iconRenderer.sprite = iconSprite;
                iconRenderer.sortingOrder = 26;

                float targetSize = 0.45f;
                var bounds = iconSprite.bounds.size;
                float maxDim = Mathf.Max(bounds.x, bounds.y);
                if (maxDim > 0f)
                {
                    normalizedScale = targetSize / maxDim;
                    iconObj.transform.localScale = new Vector3(normalizedScale, normalizedScale, 1f);
                }
            }
        }

        SpriteRenderer randomIconRenderer = null;
        if (isRandom)
        {
            try
            {
                if (_cachedRandomIcon == null)
                    _cachedRandomIcon = TouExtensionAssets.DraftRandomIcon.LoadAsset();
                
                var randomIcon = _cachedRandomIcon;
                if (randomIcon != null)
                {
                    var riObj = new GameObject("RandomIcon");
                    riObj.transform.SetParent(container.transform);
                    riObj.transform.localPosition = new Vector3(-1.05f, 0f, -0.03f);
                    riObj.layer = LayerMask.NameToLayer("UI");

                    randomIconRenderer = riObj.AddComponent<SpriteRenderer>();
                    randomIconRenderer.sprite = randomIcon;
                    randomIconRenderer.sortingOrder = 26;

                    float targetSize = 0.45f;
                    var bounds = randomIcon.bounds.size;
                    float maxDim = Mathf.Max(bounds.x, bounds.y);
                    if (maxDim > 0f)
                    {
                        normalizedScale = targetSize / maxDim;
                        riObj.transform.localScale = new Vector3(normalizedScale, normalizedScale, 1f);
                    }
                }
            }
            catch { }
        }

        Color labelColor;
        if (isRandom)
            labelColor = new Color(0.15f, 0.15f, 0.2f);
        else
            labelColor = new Color(btnColor.r * 0.2f, btnColor.g * 0.2f, btnColor.b * 0.2f, 1f);

        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(container.transform);
        labelObj.transform.localPosition = new Vector3(0.15f, 0f, -0.02f);
        labelObj.layer = LayerMask.NameToLayer("UI");

        var label = labelObj.AddComponent<TextMeshPro>();
        label.text = isRandom ? "RANDOM" : role.GetRoleName().ToUpper();
        label.fontSize = 2.2f;
        label.alignment = TextAlignmentOptions.Center;
        label.sortingOrder = 25;
        label.color = labelColor;
        label.fontStyle = FontStyles.Bold;
        label.outlineWidth = 0.1f;
        label.outlineColor = new Color32(0, 0, 0, 120);
        label.rectTransform.sizeDelta = new Vector2(2.4f, 0.65f);
        ApplyFont(label);

        _buttonRefs[container] = new ButtonRefs
        {
            BG = bgRenderer,
            Border = borderRenderer,
            Label = label,
            Icon = iconRenderer,
            RandomIcon = randomIconRenderer,
            NormalizedScale = normalizedScale
        };

        var collider = container.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(2.6f, 0.64f);

        var btn = container.AddComponent<PassiveButton>();
        btn.Colliders = new[] { (Collider2D)collider };
        btn.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();

        var normalColor = btnColor;
        var hoverColor = new Color(
            Mathf.Min(btnColor.r + 0.25f, 1f),
            Mathf.Min(btnColor.g + 0.25f, 1f),
            Mathf.Min(btnColor.b + 0.25f, 1f));

        btn.OnMouseOver = new UnityEngine.Events.UnityEvent();
        btn.OnMouseOver.AddListener((System.Action)(() =>
        {
            if (_pickLocked) return;
            if (bgRenderer) bgRenderer.color = hoverColor;
            if (borderRenderer) borderRenderer.color = Color.white;
            PlayHoverSound();
            StartHoverAnimation(container, 1.15f, 0.12f);
            
            if (iconRenderer != null) StartIconWobble(iconRenderer.gameObject, true);
            if (randomIconRenderer != null) StartIconWobble(randomIconRenderer.gameObject, true);

            if (_tooltipText != null)
            {
                if (isRandom)
                {
                    _tooltipText.text = "Gives you a completely random role from the allowed pool.";
                }
                else
                {
                    var desc = GetRoleDescription(role);
                    _tooltipText.text = !string.IsNullOrEmpty(desc) ? desc : role.GetRoleName();
                }
            }
        }));

        btn.OnMouseOut = new UnityEngine.Events.UnityEvent();
        btn.OnMouseOut.AddListener((System.Action)(() =>
        {
            if (iconRenderer != null) StartIconWobble(iconRenderer.gameObject, false);
            if (randomIconRenderer != null) StartIconWobble(randomIconRenderer.gameObject, false);

            if (_pickLocked) return;
            if (bgRenderer) bgRenderer.color = normalColor;
            if (borderRenderer)
            {
                if (role != null && role.TeamType == RoleTeamTypes.Impostor)
                    borderRenderer.color = new Color(0.3f, 0.02f, 0.02f, 1f);
                else
                    borderRenderer.color = new Color(Mathf.Max(normalColor.r - 0.15f, 0f), Mathf.Max(normalColor.g - 0.15f, 0f), Mathf.Max(normalColor.b - 0.15f, 0f), 1f);
            }
            StartHoverAnimation(container, 1.0f, 0.15f);
            if (_tooltipText != null)
            {
                _tooltipText.text = "";
            }
        }));
        if (isRandom)
        {
            var offered = DraftSystem.CurrentOfferedRoles;
            var capContainer = container;
            btn.OnClick.AddListener((System.Action)(() =>
            {
                if (!DraftSystem.IsMyTurn || _pickLocked) return;
                PlayPickSound();
                var rr = DraftSystem.PickRandomRole(isImp, offered);
                if (rr != null)
                {
                    LockRoleButtons(capContainer);
                    OnLocalPlayerPick((ushort)rr.Role);
                }
            }));
        }
        else
        {
            var cap = role;
            var capContainer = container;
            btn.OnClick.AddListener((System.Action)(() =>
            {
                if (_pickLocked || !DraftSystem.IsMyTurn) return;

                PlayPickSound();
                LockRoleButtons(capContainer);
                OnLocalPlayerPick((ushort)cap.Role);
            }));
        }

        _roleButtonObjects.Add(container);
    }

    // === SOUNDS ===

    private static void StartIconWobble(GameObject icon, bool active)
    {
        if (icon == null) return;
        if (_bounceCoroutines.TryGetValue(icon, out var current)) Coroutines.Stop(current);
        
        if (active)
            _bounceCoroutines[icon] = Coroutines.Start(CoAnimateIconWobble(icon));
        else
            _bounceCoroutines[icon] = Coroutines.Start(CoResetIconWobble(icon));
    }
 
    private static System.Collections.IEnumerator CoAnimateIconWobble(GameObject icon)
    {
        if (icon == null) yield break;
        
        // Find normalized scale from button refs
        float normScale = 0.45f;
        foreach (var refs in _buttonRefs.Values)
        {
            if ((refs.Icon != null && refs.Icon.gameObject == icon) || (refs.RandomIcon != null && refs.RandomIcon.gameObject == icon))
            {
                normScale = refs.NormalizedScale;
                break;
            }
        }
 
        Vector3 targetScale = new Vector3(normScale * 1.15f, normScale * 1.15f, 1f);
        float elapsed = 0f;
        
        while (icon != null)
        {
            elapsed += Time.deltaTime;
            icon.transform.localScale = Vector3.Lerp(icon.transform.localScale, targetScale, Time.deltaTime * 10f);
            
            float rot = Mathf.Sin(Time.time * 8f) * 6f;
            icon.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
            yield return null;
        }
    }
 
    private static System.Collections.IEnumerator CoResetIconWobble(GameObject icon)
    {
        if (icon == null) yield break;
 
        float normScale = 0.45f;
        foreach (var refs in _buttonRefs.Values)
        {
            if ((refs.Icon != null && refs.Icon.gameObject == icon) || (refs.RandomIcon != null && refs.RandomIcon.gameObject == icon))
            {
                normScale = refs.NormalizedScale;
                break;
            }
        }
 
        Vector3 targetScale = new Vector3(normScale, normScale, 1f);
        float elapsed = 0f;
        float duration = 0.2f;
        Vector3 startScale = icon.transform.localScale;
        Quaternion startRot = icon.transform.localRotation;
 
        while (elapsed < duration)
        {
            if (icon == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);
            icon.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            icon.transform.localRotation = Quaternion.Slerp(startRot, Quaternion.identity, t);
            yield return null;
        }
        if (icon != null)
        {
            icon.transform.localScale = targetScale;
            icon.transform.localRotation = Quaternion.identity;
        }
        _bounceCoroutines.Remove(icon);
    }
 
    private static void PlayPickSound()
    {
        try { SoundManager.Instance.PlaySound(TouExtensionAudio.DraftPickSound.LoadAsset(), false); } catch { }
    }

    private static AudioClip FindHoverSound()
    {
        if (_hoverSoundClip != null) return _hoverSoundClip;

        var allClips = Resources.FindObjectsOfTypeAll(Il2CppType.Of<AudioClip>());
        foreach (var obj in allClips)
        {
            var clip = obj.Cast<AudioClip>();
            string n = clip.name.ToLower();
            
            // "rollover" is the standard name for hover sounds in AU
            if (n.Contains("rollover") || n.Contains("buttonhover") || n.Contains("ui_hover"))
            {
                _hoverSoundClip = clip;
                return _hoverSoundClip;
            }
        }
        return null;
    }

    private static void PlayHoverSound()
    {
        try 
        {
            var clip = FindHoverSound();
            if (clip != null)
                SoundManager.Instance.PlaySound(clip, false, 0.5f);
        }
        catch { }
    }

    // === PICK HANDLING ===

    private static void OnLocalPlayerPick(ushort roleId)
    {
        DraftNetworking.SendPick(PlayerControl.LocalPlayer.PlayerId, roleId);
    }

    public static void OnPickReceived(byte playerId, ushort roleId)
    {
        _pickTimer = 0f;
        _alertPlayed = false;
        _countdownSoundTimer = 1f;
        _forceUpdatePlayerList = true;
        UpdatePlayerList();

        if (DraftSystem.PickOrder.Count == 0)
        {
            OnDraftComplete();
            return;
        }

        if (DraftSystem.IsMyTurn)
            ShowRoleButtonsForCurrentPicker();
    }

    // === DRAFT COMPLETE ===

    private static void OnDraftComplete()
    {
        PlayPickSound();
        _draftInProgress = false;
        _draftCompletedWaitingForStart = true;
        DraftSystem.DraftComplete = true;
        _pickTimer = 0f;

        Coroutines.Start(CoDraftCompleteSequence());
        Coroutines.Start(CoFadeOutMusicAndPlayComplete());

        if (_timerText != null)
            _timerText.gameObject.SetActive(false);

        UpdatePlayerList();

        if (_forceStartButtonObj != null) _forceStartButtonObj.SetActive(true);

        if (AmongUsClient.Instance.AmHost)
            Coroutines.Start(CoStartAfterDelay());
    }

    private static System.Collections.IEnumerator CoDraftCompleteSequence()
    {
        yield return CoFadeOutRoleButtons(1.25f);
        yield return new WaitForSeconds(0.15f);

        if (_draftCompleteText != null)
        {
            _draftCompleteText.text =
                "<size=170%><color=#00FF00><b>DRAFT COMPLETE!</b></color></size>\n" +
                "<size=140%><color=#FFFFFF><b>GAME IS STARTING!1!</b></size>";
        }
        yield return CoAnimateDraftComplete();
    }

    private static System.Collections.IEnumerator CoAnimateDraftTitle()
    {
        if (_isTitleAnimRunning) yield break;
        _isTitleAnimRunning = true;

        float interval = 20f;
        while (_draftInProgress || _draftCompletedWaitingForStart)
        {
            if (_draftTitleText == null) yield break;

            // Check what name SHOULD be displayed now
            bool isHekerTime = ((int)((Time.time + _titleRandomOffset) / interval) % 2) == 0;
            string currentText = _draftTitleText.text;
            string targetAuthor = isHekerTime ? "HEKER" : "MARZECOOO";

            // If the current name doesn't match the desired one, trigger transition
            if (!currentText.Contains(targetAuthor))
            {
                float duration = 0.8f;
                float elapsed = 0f;
                var origColor = _draftTitleText.color;

                // Fade Out
                while (elapsed < duration)
                {
                    if (_draftTitleText == null) yield break;
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    _draftTitleText.color = new Color(origColor.r, origColor.g, origColor.b, 1f - t);
                    yield return null;
                }

                if (_draftTitleText == null) yield break;
                _draftTitleText.text = $"<size=130%><b>DRAFT MODE</b></size>\nBY {targetAuthor}";
                elapsed = 0f;

                // Fade In + Punch
                while (elapsed < duration)
                {
                    if (_draftTitleText == null) yield break;
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    float punch = 1f + Mathf.Sin(t * Mathf.PI) * 0.12f;
                    
                    _draftTitleText.color = new Color(origColor.r, origColor.g, origColor.b, t);
                    _draftTitleText.transform.localScale = new Vector3(punch, punch, 1f);
                    yield return null;
                }

                if (_draftTitleText != null)
                {
                    _draftTitleText.color = origColor;
                    _draftTitleText.transform.localScale = Vector3.one;
                }
            }

            yield return new WaitForSeconds(1f); // Check every second for transition
        }
        _isTitleAnimRunning = false;
    }

    private static System.Collections.IEnumerator CoStartAfterDelay()
    {
        // Shorter delay for a snappier feel
        yield return new WaitForSeconds(1.8f);

        var gsm = Object.FindObjectOfType<GameStartManager>();
        if (gsm != null)
        {
            // Trigger the start process - BeginGamePostfix will take care of the instant transition
            gsm.BeginGame();
        }
    }

    // === UPDATE ===

    private static void UpdateDraftUI(GameStartManager gsm)
    {
        if (_draftCompletedWaitingForStart)
        {
            if (_timerText != null)
                _timerText.gameObject.SetActive(false);

            return;
        }

        if (!_draftInProgress) return;
        if (DraftSystem.PickOrder.Count == 0) return;

        _pickTimer += Time.deltaTime;
        var maxTime = DraftSystem.TimeToChoose;
        var timeLeft = Mathf.Max(0, maxTime - _pickTimer);
        var picker = DraftSystem.CurrentPicker;
        if (!picker.HasValue) return;

        TryPlayPickerAlert();

        var myId = PlayerControl.LocalPlayer != null ? PlayerControl.LocalPlayer.PlayerId : byte.MaxValue;
        bool iAlreadyPicked = DraftSystem.DraftPicks.ContainsKey(myId);
        bool iAmPickingNow = DraftSystem.IsMyTurn && !iAlreadyPicked;
        bool iAmWaiting = !DraftSystem.IsMyTurn && !iAlreadyPicked;

        if (_timerText != null)
        {
            _timerText.gameObject.SetActive(iAmPickingNow);
            if (iAmPickingNow)
            {
                var color = timeLeft < 5f ? "#FF4444" : "#ffffff";
                _timerText.text = $"<color={color}>Time Left: {(int)timeLeft}s</color>";

                if (timeLeft < 5f)
                {
                    float pulse = 1f + Mathf.Abs(Mathf.Sin(Time.time * 12f)) * 0.12f;
                    _timerText.transform.localScale = new Vector3(pulse, pulse, 1f);
                }
                else
                {
                    _timerText.transform.localScale = Vector3.one;
                }
            }
        }

        if (_draftCompleteText != null)
        {
            if (iAmWaiting)
            {
                int totalPicks = _originalPickOrder.Count;
                int remainingPicks = DraftSystem.PickOrder.Count;
                int currentPickNumber = Mathf.Clamp(totalPicks - remainingPicks + 1, 1, Mathf.Max(1, totalPicks));

                _draftCompleteText.text =
                    "<size=120%><color=#AAAAAA><b>WAITING FOR YOUR TURN</b></color></size>\n" +
                    $"<size=105%><color=#FFFFFF>{currentPickNumber}/{totalPicks}</color></size>";

                _draftCompleteText.gameObject.SetActive(true);
            }
            else
            {
                _draftCompleteText.gameObject.SetActive(false);
            }
        }

        UpdateDraftMusicIntensity();

        if (DraftSystem.IsMyTurn && timeLeft <= 11f && timeLeft > 0f)
        {
            _countdownSoundTimer -= Time.deltaTime;
            if (_countdownSoundTimer <= 0f)
            {
                try
                {
                    var pitch = 1.5f - (timeLeft / 10f) / 2f;
                    SoundManager.Instance.PlaySoundImmediate(
                        GameManagerCreator.Instance.HideAndSeekManagerPrefab.FinalHideCountdownSFX,
                        false, 1f, pitch, SoundManager.Instance.SfxChannel);
                }
                catch { }

                _countdownSoundTimer = 1f;
            }
        }
        else
        {
            _countdownSoundTimer = 1f;
        }

        if (DraftSystem.IsMyTurn && !_pickLocked && _pickTimer >= maxTime)
        {
            LockRoleButtons(null);
            var localId = PlayerControl.LocalPlayer.PlayerId;
            var isImp = DraftSystem.ImpostorPlayerIds.Contains(localId);
            var rr = DraftSystem.PickRandomRole(isImp, DraftSystem.CurrentOfferedRoles);
            if (rr != null) OnLocalPlayerPick((ushort)rr.Role);
        }

        UpdatePlayerList();

        if (gsm != null && gsm.StartButton != null && gsm.StartButton.gameObject.activeSelf)
            gsm.StartButton.gameObject.SetActive(false);
    }

    // === CLEANUP ===

    private static void ClearRoleButtons()
    {
        foreach (var obj in _roleButtonObjects)
            if (obj != null) Object.Destroy(obj);
        _roleButtonObjects.Clear();
        _buttonRefs.Clear();
        _pickLocked = false;
    }

    private static void CleanupUI()
    {
        ClearRoleButtons();
        StopDraftMusic();
        HideLobby(false); // Restore lobby UI
        if (_muteButtonObj != null) { Object.Destroy(_muteButtonObj); _muteButtonObj = null; }
        if (_cancelButtonObj != null) { Object.Destroy(_cancelButtonObj); _cancelButtonObj = null; }
        if (_forceStartButtonObj != null) { Object.Destroy(_forceStartButtonObj); _forceStartButtonObj = null; }
        if (_overlayBackground != null) { Object.Destroy(_overlayBackground); _overlayBackground = null; }
        if (_draftContainer != null) { Object.Destroy(_draftContainer); _draftContainer = null; }
        _playerListText = null;
        _timerText = null;
        _draftCompleteText = null;
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
    [HarmonyPostfix]
    public static void OnPlayerLeft(AmongUsClient __instance, [HarmonyArgument(0)] InnerNet.ClientData client, [HarmonyArgument(1)] DisconnectReasons reason)
    {
        if (!_draftInProgress && !_draftCompletedWaitingForStart) return;

        byte? leavingPlayerId = null;
        if (client?.Character != null)
            leavingPlayerId = client.Character.PlayerId;

        if (!leavingPlayerId.HasValue)
        {
            return;
        }

        var pid = leavingPlayerId.Value;

        if (!DraftSystem.PlayerFactions.ContainsKey(pid))
        {
            return;
        }

        if (!_draftInProgress && _draftCompletedWaitingForStart)
        {
            return;
        }

        bool wasCurrentPicker = DraftSystem.CurrentPicker.HasValue && DraftSystem.CurrentPicker.Value == pid;

        _disconnectedDuringDraft.Add(pid);
        DraftSystem.PickOrder.Remove(pid);
        DraftSystem.PlayerFactions.Remove(pid);

        if (DraftSystem.ImpostorPlayerIds.Contains(pid) && !DraftSystem.DraftPicks.ContainsKey(pid))
        {
            byte? newImpostor = null;
            foreach (var remainingPid in DraftSystem.PickOrder)
            {
                if (!DraftSystem.ImpostorPlayerIds.Contains(remainingPid))
                {
                    newImpostor = remainingPid;
                    break;
                }
            }

            if (newImpostor.HasValue)
            {
                DraftSystem.ImpostorPlayerIds.Add(newImpostor.Value);
                DraftSystem.PlayerFactions[newImpostor.Value] = DraftFaction.Impostor;
                // Info($"[Draft] Player {newImpostor.Value} promoted to Impostor (replacing {pid}).");
            }
            else
            {
                // Info($"[Draft] No replacement found for Impostor {pid}. One less impostor this game.");
            }

            DraftSystem.ImpostorPlayerIds.Remove(pid);
        }

        if (DraftSystem.PickOrder.Count == 0)
        {
            // Info("[Draft] No more players to pick. Completing draft.");
            OnDraftComplete();
            return;
        }

        if (wasCurrentPicker)
        {
            _pickTimer = 0f;
            _countdownSoundTimer = 1f;
            _lastAlertedPicker = null;
            _forceUpdatePlayerList = true;
            UpdatePlayerList();
            ShowRoleButtonsForCurrentPicker();
        }
        else
        {
            _forceUpdatePlayerList = true;
            UpdatePlayerList();
        }
    }

    // === CLEANUP ON GAME START ===

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
    [HarmonyPostfix]
    public static void IntroCutsceneCleanup()
    {
        CleanupUI();
        UnlockLobby();
        HideLobby(false);
        _draftInProgress = false;
        _draftCompletedWaitingForStart = false;
        _lastAlertedPicker = null;
        _disconnectedDuringDraft.Clear();
        _originalPickOrder.Clear();
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
    [HarmonyPostfix]
    public static void ShipStatusStartCleanup()
    {
        CleanupUI();
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnDisconnected))]
    [HarmonyPostfix]
    public static void OnDisconnected()
    {
        CleanupUI();
        UnlockLobby();
        _draftInProgress = false;
        _draftCompletedWaitingForStart = false;
        _lastAlertedPicker = null;
        _disconnectedDuringDraft.Clear();
        _originalPickOrder.Clear();
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    [HarmonyPostfix]
    public static void MainMenuCleanup()
    {
        StopDraftMusic();
        _draftInProgress = false;
        _draftCompletedWaitingForStart = false;
        _lastAlertedPicker = null;
        _disconnectedDuringDraft.Clear();
        _originalPickOrder.Clear();
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
    [HarmonyPrefix]
    public static void ChatControllerUpdatePrefix(ChatController __instance)
    {
        if (_draftInProgress && Input.GetKeyDown(KeyCode.Return))
        {
            if (!__instance.IsOpenOrOpening)
            {
                __instance.SetVisible(true);
            }
        }
    }
}

