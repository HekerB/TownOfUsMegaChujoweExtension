using System.Text;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.LocalSettings;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Patches.Joker;
using TownOfUs;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Roles.Neutral;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Modifiers;
using MiraAPI.Modifiers;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Neutral;

public sealed class JokerRole(IntPtr cppPtr) : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable, IContinuesGame
{
    public DoomableType DoomHintType => DoomableType.Fearmonger;
    public string LocaleKey => "Joker";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public bool MetWinCon { get; set; }
    public bool ContinuesGame => !Player.HasDied() && OptionGroupSingleton<JokerOptions>.Instance.WinMode == JokerWinOptions.WinWithWinners && MetWinCon;

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") +
               MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities
    {
        get
        {
            return new List<CustomButtonWikiDescription>
            {
                new(TouLocale.GetParsed("ExtensionRoleJokerPlaceCloneWiki", "Place Clone"),
                    TouLocale.GetParsed("ExtensionRoleJokerPlaceCloneWikiDescription",
                        "Place a clone of a player on the map. If killing roles kill enough clones, you win!"),
                    TouExtensionNeuAssets.JokerPlaceCloneButtonSprite)
            };
        }
    }

    // === PiP fields ===
    private Camera? _pipCam;
    private GameObject? _pipBorderObj;
    private SpriteRenderer? _pipBorderRenderer;
    private bool _pipDragging;
    private bool _pipManualMovedThisSession;
    private Vector2 _pipDragOffsetViewport;
    private bool _pipSnapping;
    private Rect _pipSnapFrom;
    private Rect _pipSnapTo;
    private float _pipSnapStartTime;
    private bool _pipSettingsDirty = true;

    private const float PipBaseMarginY = 0.04f;
    private const float PipBaseHeight = 0.3f;
    private const float PipBaseMarginXAspectFactor = 0.04f;
    private const float PipBaseWidthAspectFactor = 0.3f;
    private const float PipSnapDurationSeconds = 0.12f;

    public Color RoleColor => TouExtensionColors.Joker;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralEvil;
    public bool HasImpostorVision => false;

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = false,
        Icon = TouExtensionIcons.JokerRoleIcon,
        IntroSound = TouExtensionAudio.JokerLaugh,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>()
    };

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var sb = ITownOfUsRole.SetNewTabText(this);

        var killsNeeded = (int)OptionGroupSingleton<JokerOptions>.Instance.KillsToWin;
        var currentKills = JokerCloneSystem.KilledCloneCount;

        if (MetWinCon)
        {
            sb.AppendLine("<b>Objective Complete!</b>");
        }
        else
        {
            sb.AppendLine($"Clones Killed: {currentKills} / {killsNeeded}");
        }

        return sb;
    }

    public bool WinConditionMet()
    {
        var options = OptionGroupSingleton<JokerOptions>.Instance;

        if (options.WinMode == JokerWinOptions.WinWithWinners) return false;

        if (Player.HasDied() && !MetWinCon) return false;

        return MetWinCon || JokerCloneSystem.KilledCloneCount >= (int)options.KillsToWin;
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        JokerCloneSystem.ClearAll();
        MetWinCon = false;
        DestroyPiP();
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        JokerCloneSystem.RemoveClonesForJoker(targetPlayer.PlayerId);
        DestroyPiP();
    }

    public override bool CanUse(IUsable usable)
    {
        if (!GameManager.Instance.LogicUsables.CanUse(usable, Player)) return false;
        var console = usable.TryCast<Console>()!;
        return console == null || console.AllowImpostor;
    }

    public override bool DidWin(GameOverReason gameOverReason)
    {
        return MetWinCon || JokerCloneSystem.KilledCloneCount >= (int)OptionGroupSingleton<JokerOptions>.Instance.KillsToWin;
    }

    // === RPC Methods ===

    [MethodRpc((uint)Networking.ExtensionRpc.JokerPlaceClone)]
    public static void RpcJokerPlaceClone(PlayerControl joker, byte appearancePlayerId, float x, float y, float z)
    {
        var appearanceSource = MiscUtils.PlayerById(appearancePlayerId);
        if (appearanceSource == null) return;

        JokerCloneSystem.PlaceClone(joker.PlayerId, appearanceSource, new Vector3(x, y, z));

        if (joker.AmOwner && joker.Data.Role is JokerRole myRole)
            myRole.SetupPiP();
    }

	[MethodRpc((uint)Networking.ExtensionRpc.JokerCloneKilled)]
	public static void RpcJokerCloneKilled(PlayerControl killer, byte jokerId, float x, float y)
    {
    int foundIndex = -1;

    for (int i = 0; i < JokerCloneSystem.Clones.Count; i++)
    {
        var c = JokerCloneSystem.Clones[i];
        if (c.JokerId != jokerId) continue;

        var pos = new Vector2(c.WorldPosition.x, c.WorldPosition.y);
        if (Vector2.Distance(pos, new Vector2(x, y)) < 0.25f)
        {
            foundIndex = i;
            break;
        }
    }

    if (foundIndex < 0) return;

    JokerCloneSystem.AddKill();

    if (!JokerCloneSystem.TryRemoveClone(foundIndex, out _)) return;

    if (killer.AmOwner)
    {
        try
        {
            SoundManager.Instance.PlaySound(TouExtensionAudio.JokerLaugh.LoadAsset(), false, 1f);

            var notif = Helpers.CreateAndShowNotification(
                TouLocale.GetParsed("ExtensionRoleJokerFooledNotif", "You've been fooled!"),
                TouExtensionColors.Joker,
                new Vector3(0f, 1f, -20f),
                spr: TouExtensionIcons.JokerRoleIcon.LoadAsset());

            notif?.AdjustNotification();
        }
        catch { }
    }

    var jokerPlayer = MiscUtils.PlayerById(jokerId);
    if (jokerPlayer == null) return;

    var options = OptionGroupSingleton<JokerOptions>.Instance;
    var killsNeeded = (int)options.KillsToWin;
    var currentKills = JokerCloneSystem.KilledCloneCount;

    if (currentKills >= killsNeeded)
    {
        if (jokerPlayer.Data.Role is JokerRole role)
            role.MetWinCon = true;
    }

    if (jokerPlayer.AmOwner)
    {
        try
        {
            var notif = Helpers.CreateAndShowNotification(
                $"{TouLocale.GetParsed("ExtensionRoleJokerCloneKilledNotif", "Clone killed!")} ({currentKills}/{killsNeeded})",
                TouExtensionColors.Joker,
                new Vector3(0f, 1f, -20f),
                spr: TouExtensionIcons.JokerRoleIcon.LoadAsset());

            notif?.AdjustNotification();
        }
        catch { }
    }
}

    [MethodRpc((uint)Networking.ExtensionRpc.JokerDestroyClone)]
    public static void RpcJokerDestroyClone(PlayerControl joker, int cloneIndex)
    {
        if (JokerCloneSystem.TryRemoveClone(cloneIndex, out _) && joker.AmOwner && joker.Data.Role is JokerRole role)
            role.DestroyPiP();
    }

	[MethodRpc((uint)Networking.ExtensionRpc.JokerSyncClone)]
	public static void RpcJokerSyncClone(PlayerControl sender, Vector2 position, bool flipX, bool isMoving)
	{
		var jokerId = sender.PlayerId;
    
		if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == jokerId) return;

		var clone = JokerCloneSystem.Clones.FirstOrDefault(c => c.JokerId == jokerId);
		if (clone != null && clone.Fake?.body != null)
		{
			var comp = clone.Fake.body.GetComponent<JokerCloneControlComponent>();
			comp?.ReceiveSync(position, flipX, isMoving);
		}
	}

    // === PiP System ===

    public void MarkPiPSettingsDirty()
    {
        _pipSettingsDirty = true;
        _pipSnapping = false;
        _pipDragging = false;
        _pipManualMovedThisSession = false;
    }

    public void LateUpdate()
    {
        if (Player == null || !Player.AmOwner || _pipCam == null) return;

        var myClone = JokerCloneSystem.Clones.FirstOrDefault(c => c.JokerId == Player.PlayerId);
        if (myClone != null && myClone.Fake?.body != null)
        {
            var pos = myClone.Fake.body.transform.position;
            _pipCam.transform.position = new Vector3(pos.x, pos.y, _pipCam.transform.position.z);
        }
        else
        {
            DestroyPiP();
        }
    }

    public void TickPiP()
    {
        if (_pipCam == null || _pipBorderObj == null || _pipBorderRenderer == null || Camera.main == null) return;

        EnsureBorderCollider();

        if (_pipSettingsDirty)
        {
            ApplyPiPRectFromSettings(force: true);
            _pipSettingsDirty = false;
        }

        UpdateSnapAnimation();
        HandleDragInput();
        UpdateCameraBorderLayout();
    }

    private void EnsureBorderCollider()
    {
        if (_pipBorderObj == null) return;
        if (_pipBorderObj.GetComponent<BoxCollider2D>() != null) return;

        var col = _pipBorderObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2f, 2f);
    }

    private void UpdateCameraBorderLayout()
    {
        if (_pipCam == null || _pipBorderObj == null || _pipBorderRenderer == null || Camera.main == null) return;
        if (_pipBorderRenderer.sprite == null) return;

        var rect = _pipCam.rect;
        var hudCam = Camera.main;

        var worldBL = hudCam.ScreenToWorldPoint(new Vector3(rect.x * Screen.width, rect.y * Screen.height, hudCam.nearClipPlane));
        var worldTR = hudCam.ScreenToWorldPoint(new Vector3((rect.x + rect.width) * Screen.width, (rect.y + rect.height) * Screen.height, hudCam.nearClipPlane));

        _pipBorderObj.transform.position = new Vector3(
            (worldBL.x + worldTR.x) * 0.5f,
            (worldBL.y + worldTR.y) * 0.5f,
            _pipBorderObj.transform.position.z);

        var worldWidth = Mathf.Abs(worldTR.x - worldBL.x);
        var worldHeight = Mathf.Abs(worldTR.y - worldBL.y);
        var spriteSize = _pipBorderRenderer.sprite.bounds.size;

        if (spriteSize.x > 0f && spriteSize.y > 0f)
        {
            const float scaleMultiplier = 1.42f;
            _pipBorderObj.transform.localScale = new Vector3(
                (worldWidth * scaleMultiplier) / spriteSize.x,
                (worldHeight * scaleMultiplier) / spriteSize.y,
                1f);
        }

        _pipBorderRenderer.color = new Color(1f, 1f, 1f, 0.95f);
    }

    private void ApplyPiPRectFromSettings(bool force)
    {
        if (_pipCam == null) return;

        var locSetting = LocalSettingsTabSingleton<TouExtensionLocalSettings>.Instance.JokerPiPLocation.Value;

        JokerPiPLocation location;
        if (locSetting == JokerPiPLocation.Dynamic)
        {
            if (!_pipManualMovedThisSession || force)
                location = JokerPiPLocation.BottomLeft;
            else
                return;
        }
        else
        {
            location = locSetting;
        }

        _pipCam.rect = ClampRectToViewport(GetAnchorRect(location));
    }

    private Rect GetAnchorRect(JokerPiPLocation location)
    {
        var sizeMultiplier = GetSizeMultiplier();
        var aspect = (float)Screen.height / Screen.width;
        var width = aspect * PipBaseWidthAspectFactor * sizeMultiplier;
        var height = PipBaseHeight * sizeMultiplier;
        var marginX = aspect * PipBaseMarginXAspectFactor;
        var marginY = PipBaseMarginY;

        float x = marginX, y = marginY;

        switch (location)
        {
            case JokerPiPLocation.TopLeft:
                x = marginX; y = 1f - height - marginY; break;
            case JokerPiPLocation.MiddleLeft:
                x = marginX; y = (1f - height) * 0.5f; break;
            case JokerPiPLocation.Dynamic:
            case JokerPiPLocation.BottomLeft:
                x = marginX; y = marginY; break;
            case JokerPiPLocation.TopRight:
                x = 1f - width - marginX; y = 1f - height - marginY; break;
            case JokerPiPLocation.MiddleRight:
                x = 1f - width - marginX; y = (1f - height) * 0.5f; break;
            case JokerPiPLocation.BottomRight:
                x = 1f - width - marginX; y = marginY; break;
        }

        return new Rect(x, y, width, height);
    }

    private float GetSizeMultiplier()
    {
        var size = LocalSettingsTabSingleton<TouExtensionLocalSettings>.Instance.JokerPiPSize.Value;
        return size switch
        {
            JokerPiPSize.Small => 0.85f,
            JokerPiPSize.Large => 1.25f,
            _ => 1.0f
        };
    }

    private static Rect ClampRectToViewport(Rect r)
    {
        var w = Mathf.Clamp01(r.width);
        var h = Mathf.Clamp01(r.height);
        return new Rect(Mathf.Clamp(r.x, 0f, 1f - w), Mathf.Clamp(r.y, 0f, 1f - h), w, h);
    }

    private void StartSnapToNearestAnchor()
    {
        if (_pipCam == null) return;

        var current = _pipCam.rect;
        var currentCenter = new Vector2(current.x + current.width * 0.5f, current.y + current.height * 0.5f);

        var anchors = new[]
        {
            JokerPiPLocation.TopLeft, JokerPiPLocation.MiddleLeft, JokerPiPLocation.BottomLeft,
            JokerPiPLocation.TopRight, JokerPiPLocation.MiddleRight, JokerPiPLocation.BottomRight
        };

        var best = GetAnchorRect(anchors[0]);
        var bestDist = Vector2.Distance(currentCenter, new Vector2(best.x + best.width * 0.5f, best.y + best.height * 0.5f));

        foreach (var anchor in anchors)
        {
            var r = GetAnchorRect(anchor);
            var c = new Vector2(r.x + r.width * 0.5f, r.y + r.height * 0.5f);
            var d = Vector2.Distance(currentCenter, c);
            if (d < bestDist) { bestDist = d; best = r; }
        }

        _pipSnapping = true;
        _pipSnapFrom = current;
        _pipSnapTo = ClampRectToViewport(best);
        _pipSnapStartTime = Time.time;
    }

    private void UpdateSnapAnimation()
    {
        if (!_pipSnapping || _pipCam == null) return;

        var t = Mathf.Clamp01((Time.time - _pipSnapStartTime) / PipSnapDurationSeconds);
        t = 1f - Mathf.Pow(1f - t, 3f);

        _pipCam.rect = LerpRect(_pipSnapFrom, _pipSnapTo, t);

        if (t >= 1f)
        {
            _pipSnapping = false;
            _pipCam.rect = _pipSnapTo;
        }
    }

    private static Rect LerpRect(Rect a, Rect b, float t) =>
        new(Mathf.Lerp(a.x, b.x, t), Mathf.Lerp(a.y, b.y, t),
            Mathf.Lerp(a.width, b.width, t), Mathf.Lerp(a.height, b.height, t));

    private void HandleDragInput()
    {
        if (_pipCam == null || _pipBorderObj == null || Camera.main == null) return;

        var col = _pipBorderObj.GetComponent<BoxCollider2D>();
        if (col == null) return;

        bool down, held, up;
        Vector2 screenPos;

        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            screenPos = touch.position;
            down = touch.phase == TouchPhase.Began;
            held = touch.phase is TouchPhase.Moved or TouchPhase.Stationary;
            up = touch.phase is TouchPhase.Ended or TouchPhase.Canceled;
        }
        else
        {
            down = Input.GetMouseButtonDown(0);
            held = Input.GetMouseButton(0);
            up = Input.GetMouseButtonUp(0);
            screenPos = Input.mousePosition;
        }

        var world = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane));
        var hit = col.OverlapPoint(new Vector2(world.x, world.y));

        if (down && hit)
        {
            _pipDragging = true;
            _pipSnapping = false;
            _pipManualMovedThisSession = true;

            var rect = _pipCam.rect;
            var rectCenter = new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f);
            var pointerVP = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
            _pipDragOffsetViewport = rectCenter - pointerVP;
        }

        if (_pipDragging && held)
        {
            var rect = _pipCam.rect;
            var pointerVP = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
            var desiredCenter = pointerVP + _pipDragOffsetViewport;
            _pipCam.rect = ClampRectToViewport(new Rect(
                desiredCenter.x - rect.width * 0.5f,
                desiredCenter.y - rect.height * 0.5f,
                rect.width, rect.height));
        }

        if (_pipDragging && up)
        {
            _pipDragging = false;
            StartSnapToNearestAnchor();
        }
    }

    public void SetupPiP()
    {
        if (_pipCam != null) return;

        MarkPiPSettingsDirty();

        _pipCam = UnityEngine.Object.Instantiate(Camera.main);
        _pipCam.name = "JokerCloneCam";
        _pipCam.orthographicSize = 1.5f;
        _pipCam.transform.DestroyChildren();

        var follower = _pipCam.GetComponent<FollowerCamera>();
        if (follower != null) UnityEngine.Object.Destroy(follower);

        _pipCam.nearClipPlane = -1;
        _pipCam.depth = Camera.main.depth + 1;
        _pipCam.cullingMask = Camera.main.cullingMask;
        _pipCam.clearFlags = Camera.main.clearFlags;
        _pipCam.backgroundColor = Camera.main.backgroundColor;

        ApplyPiPRectFromSettings(force: true);
        _pipSettingsDirty = false;

        if (HudManager.InstanceExists && HudManager.Instance.FullScreen != null)
        {
            _pipBorderObj = new GameObject("JokerPiPBorder");
            _pipBorderObj.transform.SetParent(HudManager.Instance.FullScreen.transform.parent);
            _pipBorderObj.layer = HudManager.Instance.FullScreen.gameObject.layer;
            _pipBorderObj.transform.position = new Vector3(0f, 0f, HudManager.Instance.FullScreen.transform.position.z - 1f);

            _pipBorderRenderer = _pipBorderObj.AddComponent<SpriteRenderer>();
            _pipBorderRenderer.sortingOrder = 1000;
            _pipBorderRenderer.color = new Color(1f, 1f, 1f, 0.95f);

            var frames = TouExtensionAnims.JokerPiPBorderFrames;
            if (frames.Length > 0)
                _pipBorderRenderer.sprite = frames[0];

            var anim = _pipBorderObj.AddComponent<JokerPiPBorderAnimator>();
            anim.Frames = frames;
            anim.FramesPerSecond = 8f;

            EnsureBorderCollider();
            UpdateCameraBorderLayout();
        }
    }

    public void DestroyPiP()
    {
        if (_pipCam != null)
        {
            UnityEngine.Object.Destroy(_pipCam.gameObject);
            _pipCam = null;
        }
        if (_pipBorderObj != null)
        {
            UnityEngine.Object.Destroy(_pipBorderObj);
            _pipBorderObj = null;
            _pipBorderRenderer = null;
        }
    }
}
