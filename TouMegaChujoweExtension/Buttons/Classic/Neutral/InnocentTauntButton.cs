using System.Collections;
using System.Globalization;
using System.Linq;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Events;
using TownOfUs.Extensions;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Networking;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class InnocentTauntButton : TownOfUsRoleButton<InnocentRole>
{
    public override string Name => TouLocale.Get("ExtensionRoleInnocentTaunt", "Taunt");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override float Cooldown => OptionGroupSingleton<InnocentOptions>.Instance.TauntCooldown;
    public override LoadableAsset<Sprite> Sprite => TouNeutAssets.JesterHauntSprite;
    public override Color TextOutlineColor => TouExtensionColors.Innocent;
    public override int MaxUses => (int)OptionGroupSingleton<InnocentOptions>.Instance.MaxTaunts;
    public override bool ZeroIsInfinite { get; set; } = true;
    public override float EffectDuration => OptionGroupSingleton<InnocentOptions>.Instance.TauntDuration;

    private bool _isProcessingClick;
    private bool _isMenuOpen;
    private byte? _tauntedPlayerId;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);

        if (Button?.usesRemainingSprite != null)
        {
            Button.usesRemainingSprite.sprite = TouAssets.AbilityCounterBasicSprite.LoadAsset();
            Button.usesRemainingSprite.color = TextOutlineColor;
            Button.usesRemainingSprite.gameObject.SetActive(MaxUses > 0);
        }

        if (Button?.usesRemainingText != null)
        {
            Button.usesRemainingText.gameObject.SetActive(MaxUses > 0);
        }
    }

    public override bool Enabled(RoleBehaviour? role)
    {
        return base.Enabled(role) && PlayerControl.LocalPlayer != null && !PlayerControl.LocalPlayer.Data.IsDead;
    }

    public override bool CanUse()
    {
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data.IsDead)
        {
            return false;
        }

        if (Role != null && Role.HasTauntedThisRound)
        {
            return false;
        }

        if (_isMenuOpen || EffectActive || Minigame.Instance || MeetingHud.Instance || HudManager.Instance.Chat.IsOpenOrOpening)
        {
            return false;
        }

        if (!OptionGroupSingleton<InnocentOptions>.Instance.CanTauntFirstRound && DeathEventHandlers.CurrentRound <= 1)
        {
            return false;
        }

        return Timer <= 0f && (!LimitedUses || UsesLeft > 0);
    }

    public override void ClickHandler()
    {
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
        var innocent = PlayerControl.LocalPlayer;
        if (innocent == null) return;

        _isMenuOpen = true;
        var playerMenu = CustomPlayerMenu.Create();
        playerMenu.Begin(
            player => player != null && !player.HasDied() && player.PlayerId != innocent.PlayerId,
            player =>
            {
                _isMenuOpen = false;
                playerMenu.ForceClose();

                if (player == null || Role == null)
                {
                    Timer = 0f;
                    return;
                }

                ClearExistingMarkerForInnocent(innocent.PlayerId);
                player.RpcAddModifier<InnocentTargetModifier>(innocent.PlayerId);
                Coroutines.Start(CoForceMarkedKill(innocent.PlayerId, player.PlayerId));
                ShowTauntedNotification(player);

                _tauntedPlayerId = player.PlayerId;

                Role.HasTauntedThisRound = true;
                SpendTauntUse();
                
                if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == innocent.PlayerId)
                {
                    TownOfUs.Assets.TouAudio.PlaySound(TownOfUs.Assets.TouAudio.NoisemakerIntroSound);
                }

                EffectActive = true;
                Timer = EffectDuration;
            });
    }

    private void SpendTauntUse()
    {
        if (!LimitedUses || ZeroIsInfinite && MaxUses == 0)
        {
            return;
        }

        UsesLeft = Mathf.Max(UsesLeft - 1, 0);
        SetUses(UsesLeft);

        if (UsesLeft <= 0 && Role != null)
        {
            Role.TransformWhenTauntResolved = true;
        }
    }

    private static void ShowTauntedNotification(PlayerControl player)
    {
        var playerName = player.Data?.PlayerName ?? player.name;
        var targetName = $"{TouExtensionColors.Innocent.ToTextColor()}{playerName}</color>";
        var message = TouLocale.Get("ExtensionRoleInnocentTauntedNotif", "{0} has been taunted!")
            .Replace("{0}", targetName);

        var notif = MiraAPI.Utilities.Helpers.CreateAndShowNotification(
            $"<b>{message}</b>",
            Color.white,
            new Vector3(0f, 1f, -20f),
            spr: TouExtensionIcons.InnocentRoleIcon.LoadAsset());

        notif?.AdjustNotification();
    }

    public static void ClearExistingMarkerForInnocent(byte innocentPlayerId)
    {
        foreach (var player in PlayerControl.AllPlayerControls.ToArray())
        {
            if (player == null) continue;

            foreach (var marker in player.GetModifiers<InnocentTargetModifier>().ToArray())
            {
                if (marker.InnocentPlayerId == innocentPlayerId)
                {
                    player.RpcRemoveModifier(marker.UniqueId);
                }
            }
        }
    }

    [HideFromIl2Cpp]
    private static IEnumerator CoForceMarkedKill(byte innocentPlayerId, byte markedPlayerId)
    {
        var delay = OptionGroupSingleton<InnocentOptions>.Instance.TauntDuration;
        var endTime = Time.time + delay;

        yield return new WaitForSeconds(0.1f);

        while (Time.time < endTime)
        {
            if (MeetingHud.Instance || ExileController.Instance) yield break;

            var marked = GameData.Instance?.GetPlayerById(markedPlayerId)?.Object;
            if (marked == null || marked.HasDied() || !HasTauntMarkerForInnocent(marked, innocentPlayerId))
            {
                ResolveNoKill(innocentPlayerId);
                yield break;
            }

            var victim = FindForcedVictim(marked, innocentPlayerId);
            if (victim != null)
            {
                var killDistances = GameOptionsManager.Instance.currentNormalGameOptions.GetFloatArray(FloatArrayOptionNames.KillDistances);
                var killDist = killDistances[GameOptionsManager.Instance.currentNormalGameOptions.KillDistance];
                var dist = Vector2.Distance(marked.GetTruePosition(), victim.GetTruePosition());
                if (dist <= killDist)
                {
                    if (InnocentRole.ActiveInnocents.TryGetValue(innocentPlayerId, out var role))
                    {
                        role.BeginTauntWinWindow(marked.PlayerId);
                    }

                    if (TouMegaChujoweExtension.Modules.PoisonSystem.CheckAndTriggerShields(marked, victim))
                    {
                        Color flashColor = Color.white;
                        if (victim.TryGetModifier<TownOfUs.Modifiers.Crewmate.MedicShieldModifier>(out _))
                        {
                            flashColor = new Color(0f, 0.4f, 0f);
                        }
                        else if (victim.TryGetModifier<BodyguardShieldModifier>(out _))
                        {
                            flashColor = new Color(0f, 0.2f, 0.5f);
                        }
                        else if (victim.TryGetModifier<DoctorShieldModifier>(out _))
                        {
                            flashColor = new Color(0.46f, 0.72f, 0.38f);
                        }
                        else if (victim.HasModifier<TownOfUs.Modifiers.Crewmate.WardenFortifiedModifier>())
                        {
                            flashColor = new Color(0.6f, 0f, 1f);
                        }
                        else if (victim.HasModifier<TownOfUs.Modifiers.Crewmate.ClericBarrierModifier>())
                        {
                            flashColor = new Color(0f, 1f, 0.7f);
                        }
                        else if (victim.HasModifier<TownOfUs.Modifiers.Crewmate.MagicMirrorModifier>())
                        {
                            flashColor = new Color(0.56f, 0.63f, 0.76f);
                        }

                        if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == innocentPlayerId)
                        {
                            Coroutines.Start(MiscUtils.CoFlash(flashColor));
                        }

                        ResolveNoKill(innocentPlayerId);
                        yield break;
                    }

                    marked.RpcSpecialMurder(
                        victim,
                        isIndirect: false,
                        ignoreShield: false,
                        didSucceed: true,
                        resetKillTimer: true,
                        createDeadBody: true,
                        teleportMurderer: false,
                        showKillAnim: true,
                        playKillSound: true,
                        causeOfDeath: "InnocentTaunt");

                    ResetLocalButtonCooldown(innocentPlayerId);
                    QueueMeetingAlert(innocentPlayerId, "ExtensionRoleInnocentForcedKillNotif", "Your taunted player killed a Crewmate. Get them exiled at the next meeting!");

                    yield break;
                }
            }

            yield return new WaitForSeconds(0.1f);
        }

        ResolveNoKill(innocentPlayerId);
    }

    private static void ResolveNoKill(byte innocentPlayerId)
    {
        ClearExistingMarkerForInnocent(innocentPlayerId);
        ResetLocalButtonCooldown(innocentPlayerId);
        ShowInnocentAlert(innocentPlayerId, "ExtensionRoleInnocentNoKillNotif", "Your taunted player did not kill a Crewmate in time.");

        if (InnocentRole.ActiveInnocents.TryGetValue(innocentPlayerId, out var innocent))
        {
            innocent.AwaitingNextMeetingExile = false;
            innocent.TauntedKillerId = null;

            if (innocent.TransformWhenTauntResolved)
            {
                innocent.WinWindowExpired = true;
            }
        }
    }

    private static void QueueMeetingAlert(byte innocentPlayerId, string key, string fallback)
    {
        if (!InnocentRole.ActiveInnocents.TryGetValue(innocentPlayerId, out var innocent))
        {
            return;
        }

        innocent.PendingMeetingAlertKey = key;
        innocent.PendingMeetingAlertFallback = fallback;
    }

    private static PlayerControl? FindForcedVictim(PlayerControl marked, byte innocentPlayerId)
    {
        return PlayerControl.AllPlayerControls.ToArray()
            .Where(player => IsClosestPlayerCandidate(player, marked.PlayerId, innocentPlayerId))
            .Where(player => player!.IsCrewmate())
            .OrderBy(player => Vector2.Distance(marked.GetTruePosition(), player.GetTruePosition()))
            .FirstOrDefault();
    }

    private static bool IsClosestPlayerCandidate(PlayerControl? player, byte markedPlayerId, byte innocentPlayerId)
    {
        return player != null &&
               player.PlayerId != markedPlayerId &&
               player.PlayerId != innocentPlayerId &&
               !player.HasDied() &&
               player.Data != null &&
               !player.Data.Disconnected;
    }

    private static bool HasTauntMarkerForInnocent(PlayerControl player, byte innocentPlayerId)
    {
        return player.GetModifiers<InnocentTargetModifier>()
            .Any(marker => marker.InnocentPlayerId == innocentPlayerId);
    }

    private static void ResetLocalButtonCooldown(byte innocentPlayerId)
    {
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.PlayerId != innocentPlayerId)
        {
            return;
        }

        var button = CustomButtonSingleton<InnocentTauntButton>.Instance;
        if (button != null)
        {
            button.EffectActive = false;
            button.Timer = button.Cooldown;
        }
    }

    private static void ShowInnocentAlert(byte innocentPlayerId, string key, string fallback)
    {
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.PlayerId != innocentPlayerId)
        {
            return;
        }

        var notif = MiraAPI.Utilities.Helpers.CreateAndShowNotification(
            $"<b>{TouExtensionColors.Innocent.ToTextColor()}{TouLocale.Get(key, fallback)}</color></b>",
            Color.white,
            new Vector3(0f, 1f, -20f),
            spr: TouExtensionIcons.InnocentRoleIcon.LoadAsset());

        notif?.AdjustNotification();
    }

    [HideFromIl2Cpp]
    private static IEnumerator CoReportBaitImmediately(byte killerId, byte victimId)
    {
        yield return new WaitForSeconds(0.1f);

        if (MeetingHud.Instance || ExileController.Instance)
        {
            yield break;
        }

        var killer = GameData.Instance?.GetPlayerById(killerId)?.Object;
        var victim = GameData.Instance?.GetPlayerById(victimId);
        if (killer == null || victim == null || victim.Object == null)
        {
            yield break;
        }

        killer.RpcStartMeeting(victim);
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        if (_isMenuOpen && !Minigame.Instance)
        {
            _isMenuOpen = false;
        }

        var shouldShow = Role != null && !playerControl.HasDied() && !MeetingHud.Instance;

        if (Button != null && Button.gameObject.activeSelf != shouldShow)
        {
            Button.gameObject.SetActive(shouldShow);
        }

        if (shouldShow)
        {
            base.FixedUpdate(playerControl);

            if (EffectActive)
            {
                Button?.OverrideText(TouLocale.Get("ExtensionRoleInnocentTauntedButton", "Taunted"));
            }
            else
            {
                if (_tauntedPlayerId.HasValue)
                {
                    _tauntedPlayerId = null;
                }
                Button?.OverrideText(Name);
            }
        }

        if (Button?.usesRemainingSprite != null)
        {
            Button.usesRemainingSprite.gameObject.SetActive(MaxUses > 0);
        }

        if (Button?.usesRemainingText != null)
        {
            Button.usesRemainingText.gameObject.SetActive(MaxUses > 0);
        }
    }
}
