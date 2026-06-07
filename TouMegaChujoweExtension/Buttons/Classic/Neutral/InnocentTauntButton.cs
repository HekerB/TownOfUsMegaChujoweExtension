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
using TownOfUs.Modules.Localization;
using TownOfUs.Modifiers.Game.Crewmate;
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

    private bool _isProcessingClick;
    private bool _isMenuOpen;
    private float _tauntCountdown;
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

        if (_isMenuOpen || _tauntCountdown > 0f || Minigame.Instance || MeetingHud.Instance || HudManager.Instance.Chat.IsOpenOrOpening)
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
                    Timer = 0.01f;
                    return;
                }

                ClearExistingMarkerForInnocent(innocent.PlayerId);
                player.RpcAddModifier<InnocentTargetModifier>(innocent.PlayerId);
                Coroutines.Start(CoForceMarkedKill(innocent.PlayerId, player.PlayerId));
                ShowTauntedNotification(player);

                _tauntedPlayerId = player.PlayerId;
                _tauntCountdown = OptionGroupSingleton<InnocentOptions>.Instance.ForcedKillDelay;

                SpendTauntUse();
                Timer = 0.01f;
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

    private static void ClearExistingMarkerForInnocent(byte innocentPlayerId)
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
        var delay = OptionGroupSingleton<InnocentOptions>.Instance.ForcedKillDelay;
        var endTime = Time.time + delay;

        while (Time.time < endTime)
        {
            if (MeetingHud.Instance || ExileController.Instance) yield break;

            var marked = GameData.Instance?.GetPlayerById(markedPlayerId)?.Object;
            if (marked == null || marked.HasDied() || !marked.HasModifier<InnocentTargetModifier>())
            {
                InnocentRole.TryTransformAfterSpentTaunts(innocentPlayerId);
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
                    if (InnocentRole.ActiveInnocents.TryGetValue(innocentPlayerId, out var innocent))
                    {
                        innocent.BeginTauntWinWindow(marked.PlayerId);
                    }

                    marked.RpcCustomMurder(victim);

                    if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == innocentPlayerId)
                    {
                        var button = PlayerControl.LocalPlayer.GetComponent<InnocentTauntButton>();
                        if (button != null)
                        {
                            button.Timer = button.Cooldown;
                        }
                    }

                    yield return CoReportBaitImmediately(marked.PlayerId, victim.PlayerId);
                    yield break;
                }
            }

            yield return new WaitForSeconds(0.1f);
        }

        ClearExistingMarkerForInnocent(innocentPlayerId);

        if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == innocentPlayerId)
        {
            var button = PlayerControl.LocalPlayer.GetComponent<InnocentTauntButton>();
            if (button != null)
            {
                button.Timer = button.Cooldown;
            }
        }

        InnocentRole.TryTransformAfterSpentTaunts(innocentPlayerId);
    }

    private static PlayerControl? FindForcedVictim(PlayerControl marked, byte innocentPlayerId)
    {
        return PlayerControl.AllPlayerControls.ToArray()
            .Where(player => IsForcedVictimCandidate(player, marked.PlayerId, innocentPlayerId))
            .OrderBy(player => Vector2.Distance(marked.GetTruePosition(), player.GetTruePosition()))
            .FirstOrDefault();
    }

    private static bool IsForcedVictimCandidate(PlayerControl? player, byte markedPlayerId, byte innocentPlayerId)
    {
        return player != null &&
               player.PlayerId != markedPlayerId &&
               player.PlayerId != innocentPlayerId &&
               !player.HasDied() &&
               player.Data != null &&
               !player.Data.Disconnected &&
               player.IsCrewmate();
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

        var shouldShow = Role != null && !playerControl.HasDied();

        if (Button != null && Button.gameObject.activeSelf != shouldShow)
        {
            Button.gameObject.SetActive(shouldShow);
        }

        if (shouldShow)
        {
            base.FixedUpdate(playerControl);
        }

        UpdateTauntedCountdown();

        if (Button?.usesRemainingSprite != null)
        {
            Button.usesRemainingSprite.gameObject.SetActive(MaxUses > 0);
        }

        if (Button?.usesRemainingText != null)
        {
            Button.usesRemainingText.gameObject.SetActive(MaxUses > 0);
        }
    }

    private void UpdateTauntedCountdown()
    {
        if (_tauntCountdown <= 0f)
        {
            if (_tauntedPlayerId.HasValue)
            {
                _tauntedPlayerId = null;
                Button?.OverrideText(Name);
            }

            return;
        }

        _tauntCountdown = Mathf.Max(0f, _tauntCountdown - Time.fixedDeltaTime);
        Button?.OverrideText(TouLocale.Get("ExtensionRoleInnocentTauntedButton", "Taunted"));

        if (Button?.cooldownTimerText == null) return;

        var format = _tauntCountdown <= 10f && MiraAPI.LocalSettings.LocalSettingsTabSingleton<TownOfUs.TownOfUsLocalSettings>.Instance.PreciseCooldownsToggle.Value
            ? "0.0"
            : "0";
        Button.cooldownTimerText.text = _tauntCountdown.ToString(format, System.Globalization.NumberFormatInfo.InvariantInfo);
        Button.cooldownTimerText.gameObject.SetActive(true);
        Button.cooldownTimerText.color = Color.white;
    }
}
