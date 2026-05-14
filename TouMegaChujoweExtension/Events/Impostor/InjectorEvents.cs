using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using Random = UnityEngine.Random;
using Reactor.Utilities;
using System.Collections;
using System.Linq;
using System;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Events.Impostor;

public static class InjectorEvents
{
    private static readonly Dictionary<byte, List<PendingInjection>> PendingInjections = new();
    private static readonly Dictionary<byte, int> AppliedInjectionCounts = new();

    public static void ScheduleInjection(PlayerControl injector, PlayerControl target, int seed)
    {
        if (target == null || target.HasDied() || injector == null)
        {
            return;
        }

        var options = OptionGroupSingleton<InjectorOptions>.Instance;
        var delay = options.EffectDelay;

        var pending = new PendingInjection
        {
            Injector = injector,
            Target = target,
            Delay = delay,
            ScheduledTime = Time.time,
            InjectionId = Guid.NewGuid(),
            Seed = seed
        };

        if (!PendingInjections.ContainsKey(target.PlayerId))
        {
            PendingInjections[target.PlayerId] = new List<PendingInjection>();
        }
        PendingInjections[target.PlayerId].Add(pending);
        Coroutines.Start(CoApplyInjection(pending));
    }

    private static IEnumerator CoApplyInjection(PendingInjection pending)
    {
        yield return new WaitForSeconds(pending.Delay);

        if (pending.Target == null || pending.Target.HasDied() || pending.Injector == null || pending.Injector.HasDied())
        {
            if (PendingInjections.ContainsKey(pending.Target.PlayerId))
            {
                PendingInjections[pending.Target.PlayerId].RemoveAll(p => p.InjectionId == pending.InjectionId);
                if (PendingInjections[pending.Target.PlayerId].Count == 0)
                {
                    PendingInjections.Remove(pending.Target.PlayerId);
                }
            }
            yield break;
        }

        ApplyInjectionEffect(pending.Injector, pending.Target, pending.InjectionId, pending.Seed);
        
        if (PendingInjections.ContainsKey(pending.Target.PlayerId))
        {
            PendingInjections[pending.Target.PlayerId].RemoveAll(p => p.InjectionId == pending.InjectionId);
            if (PendingInjections[pending.Target.PlayerId].Count == 0)
            {
                PendingInjections.Remove(pending.Target.PlayerId);
            }
        }
    }

    private static void ApplyInjectionEffect(PlayerControl injector, PlayerControl target, Guid injectionId, int seed)
    {
        if (target == null || target.HasDied())
        {
            return;
        }

        var options = OptionGroupSingleton<InjectorOptions>.Instance;
        var duration = options.EffectDuration;

        if (!AppliedInjectionCounts.ContainsKey(target.PlayerId))
        {
            AppliedInjectionCounts[target.PlayerId] = 0;
        }
        AppliedInjectionCounts[target.PlayerId]++;

        if (AppliedInjectionCounts[target.PlayerId] > 1)
        {
            duration *= options.DoubleInjectionMultiplier;
        }

        var durationType = options.EffectDurationType.Value;

        var effects = new List<(float Weight, Func<BaseModifier> CreateModifier, string NotificationKey)>();

        // Negative effects
        effects.Add((options.ChanceInvertedControls, () => new InjectedInvertedControlsModifier(duration, durationType), "ExtensionInjectorNotificationInvertedControls"));
        effects.Add((options.ChanceLowVision, () => new InjectedLowVisionModifier(duration, durationType), "ExtensionInjectorNotificationLowVision"));
        effects.Add((options.ChanceSlowness, () => new InjectedSlownessModifier(duration, durationType), "ExtensionInjectorNotificationSlowness"));
        effects.Add((options.ChanceVeryLowVision, () => new InjectedVeryLowVisionModifier(duration, durationType), "ExtensionInjectorNotificationVeryLowVision"));
        effects.Add((options.ChanceConfused, () => new InjectedConfusedModifier(duration, durationType), "ExtensionInjectorNotificationConfused"));
        
        // Only add NoVent if the player can actually vent
        var canVent = target.Data?.Role != null && (
            target.IsImpostor() || 
            target.Data.Role.CanVent || 
            (target.Data.Role is MiraAPI.Roles.ICustomRole customRole && customRole.Configuration.CanUseVent)
        );
        if (canVent)
        {
            effects.Add((options.ChanceNoVent, () => new InjectedNoVentModifier(duration, durationType), "ExtensionInjectorNotificationNoVent"));
        }
        
        effects.Add((options.ChanceNoUse, () => new InjectedNoUseModifier(duration, durationType), "ExtensionInjectorNotificationNoUse"));
        effects.Add((options.ChanceNoReport, () => new InjectedNoReportModifier(duration, durationType), "ExtensionInjectorNotificationNoReport"));
        effects.Add((options.ChanceNausea, () => new InjectedNauseaModifier(duration, durationType), "ExtensionInjectorNotificationNausea"));
        effects.Add((options.ChanceWeakness, () => new InjectedWeaknessModifier(duration, durationType), "ExtensionInjectorNotificationWeakness"));

        // Positive effects (only if enabled)
        if (options.PositiveEffectsEnabled)
        {
            effects.Add((options.ChanceSpeedBoost, () => new InjectedSpeedBoostModifier(duration, durationType), "ExtensionInjectorNotificationSpeedBoost"));
            effects.Add((options.ChanceVisionBoost, () => new InjectedVisionBoostModifier(duration, durationType), "ExtensionInjectorNotificationVisionBoost"));
            effects.Add((options.ChanceRegeneration, () => new InjectedRegenerationModifier(duration, durationType), "ExtensionInjectorNotificationRegeneration"));
        }

        // Calculate total weight
        var totalWeight = effects.Sum(e => e.Weight);

        // If total weight is 0, default to InvertedControls to ensure an effect is always applied
        if (totalWeight <= 0f)
        {
            var defaultModifier = new InjectedInvertedControlsModifier(duration, durationType);
            if (defaultModifier is IInjectedModifier defaultInjectedMod)
            {
                defaultInjectedMod.InjectionId = injectionId;
            }
            target.AddModifier(defaultModifier);
            ShowNotification(injector, target, "ExtensionInjectorNotificationInvertedControls", 
                defaultModifier is IInjectedModifier defaultInjected ? defaultInjected.GetEffectDescription() : string.Empty);
            return;
        }

        var rng = new System.Random(seed);
        var randomValue = (float)(rng.NextDouble() * totalWeight);
        var cumulativeWeight = 0f;
        BaseModifier? selectedModifier = null;
        string selectedNotificationKey = string.Empty;

        foreach (var (weight, createModifier, notificationKey) in effects)
        {
            cumulativeWeight += weight;
            if (randomValue <= cumulativeWeight)
            {
                selectedModifier = createModifier();
                selectedNotificationKey = notificationKey;
                break;
            }
        }

        if (selectedModifier == null)
        {
            selectedModifier = new InjectedInvertedControlsModifier(duration, durationType);
            selectedNotificationKey = "ExtensionInjectorNotificationInvertedControls";
        }

        if (selectedModifier is IInjectedModifier injectedMod)
        {
            injectedMod.InjectionId = injectionId;
        }
        target.AddModifier(selectedModifier);
        var effectDesc = selectedModifier is IInjectedModifier injected ? injected.GetEffectDescription() : string.Empty;
        ShowNotification(injector, target, selectedNotificationKey, effectDesc);
    }

    private static void ShowNotification(PlayerControl injector, PlayerControl target, string notificationKey, string effectDescription = "")
    {
        if (injector == null || !injector.AmOwner)
        {
            return;
        }

        var baseMessage = TouLocale.GetParsed(notificationKey, notificationKey);
        var message = string.IsNullOrEmpty(effectDescription) ? baseMessage : $"{baseMessage} ({effectDescription})";
        var injectorColor = ColorUtility.ToHtmlStringRGBA(TouExtensionColors.Injector);
        
        var localizedPrefix = TouLocale.GetParsed("ExtensionInjectorNotificationPrefix", "Injected {0}:");
        var finalMessage = string.Format(localizedPrefix, target.Data.PlayerName) + " " + message;

        var notif = Helpers.CreateAndShowNotification(
            $"<b><color=#{injectorColor}>{finalMessage}</color></b>",
            Color.white,
            new Vector3(0f, 1f, -20f),
            spr: TouExtensionIcons.InjectorRole.LoadAsset());

        notif.AdjustNotification();
    }

    public static void ShowEffectWoreOffNotification(PlayerControl target, string notificationKey)
    {
        if (target == null || !target.AmOwner)
        {
            return;
        }

        var message = TouLocale.GetParsed(notificationKey, notificationKey);
        var injectorColor = ColorUtility.ToHtmlStringRGBA(TouExtensionColors.Injector);
        var notif = Helpers.CreateAndShowNotification(
            $"<b><color=#{injectorColor}>{message}</color></b>",
            Color.white,
            new Vector3(0f, 1f, -20f),
            spr: TouExtensionIcons.InjectorRole.LoadAsset());

        notif.AdjustNotification();
    }

    [RegisterEvent]
    public static void EjectionEventHandler(EjectionEvent @event)
    {
        var exiled = @event.ExileController?.initData?.networkedPlayer?.Object;
        if (exiled == null)
        {
            return;
        }

        var keysToCheck = PendingInjections.Keys.ToList();
        foreach (var key in keysToCheck)
        {
            if (PendingInjections[key].Any(p => p.Injector?.PlayerId == exiled.PlayerId))
            {
                PendingInjections[key].RemoveAll(p => p.Injector?.PlayerId == exiled.PlayerId);
                if (PendingInjections[key].Count == 0)
                {
                    PendingInjections.Remove(key);
                }
            }
        }

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied())
            {
                continue;
            }

            if (player.HasModifier<InjectedInvertedControlsModifier>() ||
                player.HasModifier<InjectedLowVisionModifier>() ||
                player.HasModifier<InjectedVeryLowVisionModifier>() ||
                player.HasModifier<InjectedSlownessModifier>() ||
                player.HasModifier<InjectedConfusedModifier>())
            {
            }
        }
    }

    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (@event.TriggeredByIntro)
        {
            AppliedInjectionCounts.Clear();
            PendingInjections.Clear();
        }

        if (!@event.TriggeredByIntro)
        {
            return;
        }

        if (PlayerControl.LocalPlayer?.Data?.Role is not InjectorRole)
        {
            return;
        }

        var btn = CustomButtonSingleton<InjectorInjectButton>.Instance;
        var options = OptionGroupSingleton<InjectorOptions>.Instance;
        btn.SetUses((int)options.InitialUses);
    }

    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        var source = @event.Source;
        if (source == null || source.Data.Role is not InjectorRole)
        {
            return;
        }

        var options = OptionGroupSingleton<InjectorOptions>.Instance;
        
        var injectButton = CustomButtonSingleton<InjectorInjectButton>.Instance;
        if (injectButton != null && options.SharedCooldown)
        {
            injectButton.Timer = injectButton.Cooldown;
        }
    }

    private sealed class PendingInjection
    {
        public PlayerControl? Injector { get; set; }
        public PlayerControl? Target { get; set; }
        public float Delay { get; set; }
        public float ScheduledTime { get; set; }
        public Guid InjectionId { get; set; }
        public int Seed { get; set; }
    }
}


















