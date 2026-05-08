using System.Collections;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Modifiers;
using TownOfUs.Interfaces;
using TownOfUs.Modifiers.Game;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

public sealed class DeathNoteModifier : TouGameModifier, IWikiDiscoverable, IButtonModifier, IColoredModifier, IGuessable
{
    public Color ModifierColor => TouExtensionColors.DeathNote;
    public override string LocaleKey => "DeathNote";
    public override string ModifierName => TouLocale.Get($"ExtensionModifier{LocaleKey}");
    public override string IntroInfo => TouLocale.GetParsed($"ExtensionModifier{LocaleKey}IntroBlurb");
    public override LoadableAsset<Sprite>? ModifierIcon => TouExtensionModifierIcons.DeathNoteModifierIcon;
    public override Color FreeplayFileColor => new Color32(42, 10, 42, 255);
    public override ModifierFaction FactionType => ModifierFaction.NeutralUtility;
    public override bool ShowInFreeplay => true;
    public string GuesserName => ModifierName;
    public Color GuesserColor => ModifierColor;
    public Sprite? GuesserIcon => ModifierIcon?.LoadAsset();
    public bool CanBeGuessed => GetAmountPerGame() > 0;

	[HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities
    {
        get
        {
            return new List<CustomButtonWikiDescription>
            {
                new(TouLocale.Get($"ExtensionModifierDeathNoteAbility"),
                    TouLocale.GetParsed($"ExtensionModifierDeathNoteAbilityWikiDescription"),
                    TouExtensionAssets.DeathNoteUISprite)
            };
        }
    }

    private bool _used;
    private PlayerControl? _cursedTarget;
    private float _killTimer;
    private bool _timerActive;
    private GameObject? _notePickup;
    private bool _soundPlayed;

    public bool IsUsed => _used;
    public PlayerControl? CursedTarget => _cursedTarget;
    public float KillTimer => _killTimer;
    public bool TimerActive => _timerActive;
    public bool SoundPlayed
    {
        get => _soundPlayed;
        set => _soundPlayed = value;
    }

    public override string GetDescription()
    {
        if (_timerActive && _cursedTarget != null && _cursedTarget.Data != null && !_cursedTarget.Data.IsDead)
        {
            var usedDesc = TouLocale.GetParsed($"ExtensionModifier{LocaleKey}BaseDesc");
            var timeLeft = Mathf.CeilToInt(_killTimer);
            var targetName = _cursedTarget.Data.PlayerName;

            usedDesc += $"\n<color=#8B00FF> Target: {targetName}</color>";
            usedDesc += $"\n<color=#FF0000>Time: {timeLeft}s</color>";
            return usedDesc;
        }

        if (_used)
        {
            return TouLocale.GetParsed($"ExtensionModifier{LocaleKey}BaseDesc");
        }

        return TouLocale.GetParsed($"ExtensionModifier{LocaleKey}TabDescription");
    }

    public string GetAdvancedDescription()
    {
        var description = TouLocale.GetParsed($"ExtensionModifier{LocaleKey}WikiDescription");
        description += MiscUtils.AppendOptionsText(GetType());
        return description;
    }

    public override int GetAssignmentChance()
    {
        return (int)OptionGroupSingleton<NeutralModifierOptions>.Instance.DeathNoteChance;
    }

    public override int GetAmountPerGame()
    {
        return (int)OptionGroupSingleton<NeutralModifierOptions>.Instance.DeathNoteAmount;
    }

    public override bool IsModifierValidOn(RoleBehaviour role)
    {
        if (!base.IsModifierValidOn(role))
            return false;

        return role is ITownOfUsRole { RoleAlignment: RoleAlignment.NeutralKilling };
    }

    public override void OnActivate()
    {
        base.OnActivate();
        _used = false;
        _cursedTarget = null;
        _killTimer = 0f;
        _timerActive = false;
        _soundPlayed = false;
        _notePickup = null;

        if (Player != null && Player.AmOwner)
        {
            Coroutines.Start(CoSpawnNotePickup());
        }
    }

    [HideFromIl2Cpp]
    private System.Collections.IEnumerator CoSpawnNotePickup()
    {
        yield return new WaitForSeconds(2f);

        if (_used) yield break;
        if (ShipStatus.Instance == null) yield break;

        var vents = ShipStatus.Instance.AllVents;
        if (vents == null || vents.Count == 0) yield break;

        var randomVent = vents[UnityEngine.Random.Range(0, vents.Count)];
        var spawnPos = randomVent.transform.position + new Vector3(0f, -0.1f, -0.5f);

        var sprite = TouExtensionModifierIcons.DeathNoteModifierIcon.LoadAsset();

        _notePickup = new GameObject("DeathNotePickup");
        _notePickup.transform.position = spawnPos;

        var sr = _notePickup.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 5;
        _notePickup.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

        var behaviour = _notePickup.AddComponent<DeathNotePickupBehaviour>();
        behaviour.Initialize(this);

    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (Player == null || !Player.AmOwner) return;

        if (_timerActive && _cursedTarget != null)
        {
            _killTimer -= Time.fixedDeltaTime;
            if (_killTimer <= 0f)
            {
                ExecuteCursedTarget();
            }
        }
    }

    [HideFromIl2Cpp]
    public DeathNoteSubmitResult OnNameSubmitted(string enteredName)
    {
        if (_used) return DeathNoteSubmitResult.NotFound;
        if (Player == null || Player.Data == null) return DeathNoteSubmitResult.NotFound;

        var trimmed = enteredName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return DeathNoteSubmitResult.NotFound;

        if (Player.Data.PlayerName.Trim().StartsWith(trimmed, StringComparison.OrdinalIgnoreCase))
        {
            var selfOnly = true;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc == null || pc.Data == null || pc.Data.IsDead || pc.Data.Disconnected) continue;
                if (pc.PlayerId == Player.PlayerId) continue;
                if (pc.Data.PlayerName.Trim().StartsWith(trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    selfOnly = false;
                    break;
                }
            }

            if (selfOnly) return DeathNoteSubmitResult.SelfTarget;
        }

        PlayerControl? target = null;

        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc == null || pc.Data == null || pc.Data.IsDead || pc.Data.Disconnected) continue;
            if (pc.PlayerId == Player.PlayerId) continue;

            if (string.Equals(pc.Data.PlayerName.Trim(), trimmed, StringComparison.OrdinalIgnoreCase))
            {
                target = pc;
                break;
            }
        }

        if (target == null)
        {
            PlayerControl? partialMatch = null;
            var matchCount = 0;

            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc == null || pc.Data == null || pc.Data.IsDead || pc.Data.Disconnected) continue;
                if (pc.PlayerId == Player.PlayerId) continue;

                if (pc.Data.PlayerName.Trim().StartsWith(trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    partialMatch = pc;
                    matchCount++;
                }
            }

            if (matchCount == 1)
                target = partialMatch;
        }

        if (target == null)
            return DeathNoteSubmitResult.NotFound;

        _cursedTarget = target;
        _killTimer = (int)OptionGroupSingleton<DeathNoteModifierOptions>.Instance.DeathNoteTimer.Value;
        _timerActive = true;
        _soundPlayed = false;

        _used = true;

        if (_notePickup != null)
        {
            UnityEngine.Object.Destroy(_notePickup);
            _notePickup = null;
        }

        var successNotif = Helpers.CreateAndShowNotification(
            $"<b><color=#2A0A2A>{target.Data.PlayerName} has been cursed. Death awaits...</color></b>",
            Color.white,
            new Vector3(0f, 1f, -20f),
            spr: TouExtensionModifierIcons.DeathNoteModifierIcon.LoadAsset());
        successNotif.AdjustNotification();

        return DeathNoteSubmitResult.Success;
    }

    [HideFromIl2Cpp]
    private void ExecuteCursedTarget()
    {
        _timerActive = false;

        if (_cursedTarget == null || _cursedTarget.Data == null || _cursedTarget.Data.IsDead)
        {
            _cursedTarget = null;
            return;
        }

        if (Player == null)
        {
            _cursedTarget = null;
            return;
        }

        Player.RpcSpecialMurder(
            _cursedTarget,
            isIndirect: true,
            ignoreShield: true,
            didSucceed: true,
            resetKillTimer: false,
            createDeadBody: true,
            teleportMurderer: false,
            showKillAnim: false,
            playKillSound: false,
            causeOfDeath: "HeartAttack");

        RpcPlayDeathNoteLaugh(Player);

        _cursedTarget = null;
    }

    [MethodRpc((uint)Networking.ExtensionRpc.DeathNoteLaugh)]
    public static void RpcPlayDeathNoteLaugh(PlayerControl player)
    {
        var clip = TouExtensionAudio.DeathNoteLaughSound.LoadAsset();
        if (clip != null)
        {
            var source = SoundManager.Instance.PlaySound(clip, false, 1f);
            if (source != null)
            {
                Coroutines.Start(CoFadeOut(source, clip.length));
            }
        }
    }

    private static IEnumerator CoFadeOut(AudioSource source, float clipLength)
    {
        float fadeTime = 2.0f; 
        float waitTime = Mathf.Max(0, clipLength - fadeTime - 0.1f);

        yield return new WaitForSeconds(waitTime);

        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < fadeTime && source != null)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeTime);
            yield return null;
        }

        if (source != null)
        {
            source.Stop();
        }
    }

    public override void OnDeactivate()
    {
        if (_notePickup != null)
        {
            UnityEngine.Object.Destroy(_notePickup);
            _notePickup = null;
        }

        _timerActive = false;
        _cursedTarget = null;
    }
}

public enum DeathNoteSubmitResult
{
    Success,
    NotFound,
    SelfTarget
}



