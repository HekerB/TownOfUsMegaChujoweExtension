using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting.Voting;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using Reactor.Utilities.Extensions;
using Reactor.Utilities;
using System.Collections;
using System.Linq;
using System;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities.Appearances;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Events;

public static class SpitefulEvents
{
    private static readonly Dictionary<byte, HashSet<byte>> SpitefulVoters = [];

    [RegisterEvent]
    public static void GameEndEventHandler(GameEndEvent @event)
    {
        SpitefulVoters.Clear();

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null)
            {
                continue;
            }

            if (player.TryGetModifier<SpitefulEffectModifier>(out var mod))
            {
                player.RemoveModifier(mod);
            }
        }
    }

    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        SpitefulVoters.Clear();

        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || localPlayer.HasDied() || !localPlayer.AmOwner)
        {
            return;
        }

        var mod = localPlayer.GetModifier<SpitefulEffectModifier>();
        if (mod == null || mod.EffectType != SpitefulEffectType.IncreasedCooldowns)
        {
            return;
        }

        Coroutines.Start(CoFixSpitefulCooldowns(localPlayer));
    }

    private static IEnumerator CoFixSpitefulCooldowns(PlayerControl player)
    {
        while (MeetingHud.Instance != null || ExileController.Instance != null)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        if (player == null || player.HasDied() || !player.AmOwner)
        {
            yield break;
        }

        var currentMod = player.GetModifier<SpitefulEffectModifier>();
        if (currentMod == null || currentMod.EffectType != SpitefulEffectType.IncreasedCooldowns)
        {
            yield break;
        }

        var role = player.Data?.Role;
        if (role == null)
        {
            yield break;
        }

        foreach (var button in CustomButtonManager.Buttons)
        {
            if (button == null)
            {
                continue;
            }

            if (!button.Enabled(role))
            {
                continue;
            }

            button.EffectActive = false;
            
            float baseCooldown = button.Cooldown;
            float multiplier = currentMod.CooldownMultiplier;
            float multipliedCooldown = baseCooldown * multiplier;
            button.Timer = multipliedCooldown;
        }
    }

    [RegisterEvent]
    public static void StartMeetingEventHandler(StartMeetingEvent @event)
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied() || !player.HasModifier<SpitefulEffectModifier>())
            {
                continue;
            }

            var mod = player.GetModifier<SpitefulEffectModifier>();
            if (mod != null && mod.DurationType == SpitefulDurationType.NextRounds)
            {
                mod.DecrementRounds();
                if (mod.RoundsRemaining <= 0)
                {
                    player.RemoveModifier(mod);
                }
            }
        }
    }

    [RegisterEvent]
    public static void HandleVoteEventHandler(HandleVoteEvent @event)
    {
        var votingPlayer = @event.Player;
        var suspectPlayer = @event.TargetPlayerInfo;

        if (suspectPlayer?.Object == null || !suspectPlayer.Object.HasModifier<SpitefulModifier>())
        {
            return;
        }

        if (!SpitefulVoters.TryGetValue(suspectPlayer.Object.PlayerId, out var voters))
        {
            voters = [];
            SpitefulVoters[suspectPlayer.Object.PlayerId] = voters;
        }

        voters.Add(votingPlayer.PlayerId);
    }

    [RegisterEvent]
    public static void EjectionEventHandler(EjectionEvent @event)
    {
        var exiled = @event.ExileController?.initData?.networkedPlayer?.Object;
        if (exiled == null || !exiled.HasModifier<SpitefulModifier>())
        {
            return;
        }

        if (!SpitefulVoters.TryGetValue(exiled.PlayerId, out var voters) || voters.Count == 0)
        {
            return;
        }

        var options = OptionGroupSingleton<SpitefulModifierOptions>.Instance;
        var effectType = options.SpitefulEffectType.Value;
        var durationType = options.SpitefulDurationType.Value;
        var rounds = (int)options.SpitefulRoundCount.Value;
        var impact = options.SpitefulImpact.Value;

        var effectDescription = GetEffectDescription(effectType, impact);
        var spitefulName = TouLocale.Get("ExtensionModifierSpiteful");
        var spitefulColor = (Color)new Color32(255, 100, 0, 255);

        var exiledName = exiled.Data.PlayerName;
        foreach (var voterId in voters.ToList())
        {
            if (voterId == exiled.PlayerId)
            {
                continue;
            }

            Coroutines.Start(CoAddSpitefulModifier(voterId, effectType, durationType, rounds, impact, effectDescription, spitefulName, spitefulColor, exiledName));
        }

        SpitefulVoters.Remove(exiled.PlayerId);
    }

    private static IEnumerator CoAddSpitefulModifier(byte voterId, SpitefulEffectType effectType, SpitefulDurationType durationType, int rounds, float impact, string effectDescription, string spitefulName, Color spitefulColor, string exiledPlayerName)
    {
        var voter = MiscUtils.PlayerById(voterId);
        if (voter == null || voter.HasDied() || voter.Data == null)
        {
            yield break;
        }

        if (voter.HasModifier<SpitefulEffectModifier>())
        {
            yield break;
        }

        var modifier = new SpitefulEffectModifier(
            effectType,
            durationType,
            rounds,
            impact
        );

        voter.AddModifier(modifier);

        while (MeetingHud.Instance != null || ExileController.Instance != null)
        {
            yield return null;
        }

        if (effectType == SpitefulEffectType.Slowness)
        {
            voter.RawSetAppearance(modifier);
        }


        if (voter.AmOwner)
        {
            var notificationText = TouLocale.GetParsed(
                "ExtensionModifierSpitefulVoterNotification",
                $"<b>The player you voted for was {spitefulColor.ToTextColor()}{spitefulName}</color>!</b>{Environment.NewLine}<color=#{spitefulColor.ToHtmlStringRGBA()}>{effectDescription}</color>",
                new Dictionary<string, string>
                {
                    { "<spiteful>", $"{spitefulColor.ToTextColor()}{spitefulName}</color>" },
                    { "<player>", exiledPlayerName },
                    { "<effect>", effectDescription }
                });

            var notif = Helpers.CreateAndShowNotification(
                notificationText,
                Color.white,
                new Vector3(0f, 1f, -20f));

            notif.AdjustNotification();
        }
    }

    private static string GetEffectDescription(SpitefulEffectType effectType, float impact)
    {
        var impactPercent = (int)impact;
        return effectType switch
        {
            SpitefulEffectType.LowerVision => TouLocale.GetParsed(
                "ExtensionModifierSpitefulEffectLowerVisionDescription",
                $"Your vision is reduced by {impactPercent}%",
                new Dictionary<string, string> { { "<impact>", impactPercent.ToString(System.Globalization.CultureInfo.InvariantCulture) } }),
            SpitefulEffectType.Slowness => TouLocale.GetParsed(
                "ExtensionModifierSpitefulEffectSlownessDescription",
                $"Your speed is reduced by {impactPercent}%",
                new Dictionary<string, string> { { "<impact>", impactPercent.ToString(System.Globalization.CultureInfo.InvariantCulture) } }),
            SpitefulEffectType.IncreasedCooldowns => TouLocale.GetParsed(
                "ExtensionModifierSpitefulEffectIncreasedCooldownsDescription",
                $"Your ability cooldowns are increased by {impactPercent}%",
                new Dictionary<string, string> { { "<impact>", impactPercent.ToString(System.Globalization.CultureInfo.InvariantCulture) } }),
            _ => string.Empty
        };
    }
}














