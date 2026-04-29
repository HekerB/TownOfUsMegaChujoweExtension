using System;
using System.Collections;
using System.Linq;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using Reactor.Utilities;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Buttons.Crewmate;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;
using TouMegaChujoweExtension.Modifiers.Crewmate;

namespace TouMegaChujoweExtension.Events.Crewmate;

public static class DoctorEvents
{
    private static readonly Dictionary<byte, List<PendingHeal>> PendingHeals = new();

    public static void ScheduleHeal(PlayerControl doctor, PlayerControl target, int seed)
    {
        if (target == null || target.HasDied() || doctor == null)
        {
            return;
        }

        var options = OptionGroupSingleton<DoctorOptions>.Instance;
        var delay = options.EffectDelay;

        var pending = new PendingHeal
        {
            Doctor = doctor,
            Target = target,
            Delay = delay,
            ScheduledTime = Time.time,
            HealId = Guid.NewGuid(),
            Seed = seed
        };

        if (!PendingHeals.ContainsKey(target.PlayerId))
        {
            PendingHeals[target.PlayerId] = new List<PendingHeal>();
        }
        PendingHeals[target.PlayerId].Add(pending);
        Coroutines.Start(CoApplyHeal(pending));
    }

    private static IEnumerator CoApplyHeal(PendingHeal pending)
    {
        yield return new WaitForSeconds(pending.Delay);

        if (pending.Target == null || pending.Target.HasDied() || pending.Doctor == null || pending.Doctor.HasDied())
        {
            if (PendingHeals.ContainsKey(pending.Target.PlayerId))
            {
                PendingHeals[pending.Target.PlayerId].RemoveAll(p => p.HealId == pending.HealId);
                if (PendingHeals[pending.Target.PlayerId].Count == 0)
                {
                    PendingHeals.Remove(pending.Target.PlayerId);
                }
            }
            yield break;
        }

        ApplyHealEffect(pending.Doctor, pending.Target, pending.Seed);
        
        if (PendingHeals.ContainsKey(pending.Target.PlayerId))
        {
            PendingHeals[pending.Target.PlayerId].RemoveAll(p => p.HealId == pending.HealId);
            if (PendingHeals[pending.Target.PlayerId].Count == 0)
            {
                PendingHeals.Remove(pending.Target.PlayerId);
            }
        }
    }

    private static void ApplyHealEffect(PlayerControl doctor, PlayerControl target, int seed)
    {
        if (target == null || target.HasDied())
        {
            return;
        }

        var options = OptionGroupSingleton<DoctorOptions>.Instance;
        var duration = options.EffectDuration;
        var durationType = options.EffectDurationType.Value;

        var effects = new List<(float Weight, Func<BaseModifier> CreateModifier, string NotificationKey, string Desc)>();

        effects.Add((options.ChanceSpeedBoost, new Func<BaseModifier>(() => new DoctorSpeedBoostModifier(duration, durationType)), "ExtensionDoctorNotificationSpeedBoost", "Increased movement speed"));
        effects.Add((options.ChanceVisionBoost, new Func<BaseModifier>(() => new DoctorVisionBoostModifier(duration, durationType)), "ExtensionDoctorNotificationVisionBoost", "Increased vision"));
        effects.Add((options.ChanceCleanse, new Func<BaseModifier>(() => new DoctorCleanseModifier()), "ExtensionDoctorNotificationCleanse", "Negative effects removed"));
        effects.Add((options.ChanceShield, new Func<BaseModifier>(() => new DoctorShieldModifier(doctor, duration, durationType)), "ExtensionDoctorNotificationShield", "Protected from one attack"));
        effects.Add((options.ChanceCanVent, new Func<BaseModifier>(() => new DoctorCanVentModifier(duration, durationType)), "ExtensionDoctorNotificationCanVent", "Can use vents"));
        effects.Add((options.ChanceRegeneration, new Func<BaseModifier>(() => new DoctorRegenerationModifier(duration, durationType)), "ExtensionDoctorNotificationRegeneration", "Cooldowns recover faster"));

        // Injector negative effects if enabled
        if (options.CanGiveNegativeEffects)
        {
            effects.Add((options.ChanceSpeedBoost, new Func<BaseModifier>(() => new TouMegaChujoweExtension.Modifiers.InjectedSlownessModifier(duration, TouMegaChujoweExtension.Options.Roles.Impostor.InjectorEffectDurationType.SetTime)), "ExtensionRoleInjectorEffectTypeEnumSlowness", "Slowness"));
            effects.Add((options.ChanceVisionBoost, new Func<BaseModifier>(() => new TouMegaChujoweExtension.Modifiers.InjectedLowVisionModifier(duration, TouMegaChujoweExtension.Options.Roles.Impostor.InjectorEffectDurationType.SetTime)), "ExtensionRoleInjectorEffectTypeEnumLowVision", "Low Vision"));
            effects.Add((options.ChanceCleanse, new Func<BaseModifier>(() => new TouMegaChujoweExtension.Modifiers.InjectedConfusedModifier(duration, TouMegaChujoweExtension.Options.Roles.Impostor.InjectorEffectDurationType.SetTime)), "ExtensionRoleInjectorEffectTypeEnumConfused", "Confused"));
        }

        var totalWeight = effects.Sum(e => e.Weight);

        if (totalWeight <= 0f)
        {
            // Default to Speed Boost if nothing is configured
            var defaultMod = new DoctorSpeedBoostModifier(duration, durationType);
            target.AddModifier(defaultMod);
            ShowNotification(target, "ExtensionDoctorNotificationSpeedBoost", "Increased movement speed");
            return;
        }

        // Use System.Random for deterministic generation across clients using the same seed
        var rng = new System.Random(seed);
        var randomValue = (float)(rng.NextDouble() * totalWeight);
        
        var cumulativeWeight = 0f;
        BaseModifier? selectedModifier = null;
        string selectedNotificationKey = string.Empty;
        string selectedDesc = string.Empty;

        foreach (var (weight, createModifier, notificationKey, desc) in effects)
        {
            cumulativeWeight += weight;
            if (randomValue <= cumulativeWeight)
            {
                selectedModifier = createModifier();
                selectedNotificationKey = notificationKey;
                selectedDesc = desc;
                break;
            }
        }

        if (selectedModifier != null)
        {
            target.AddModifier(selectedModifier);
            ShowNotification(target, selectedNotificationKey, selectedDesc);
            
            // Start coroutine to remove ClericBarrierModifier if it was selected and durationType is SetTime
            // TimedModifiers handle their own duration removal
        }
    }


    public static void ShieldAttacked(PlayerControl doctor, PlayerControl attacker, PlayerControl target)
    {
        var options = OptionGroupSingleton<DoctorOptions>.Instance;
        
        if (options.DoctorSeesShieldFlash)
        {
            ShowNotification(doctor, "ExtensionDoctorNotificationShieldAttacked", $"Your shield on {target.Data.PlayerName} protected them from {attacker.Data.PlayerName}!");
        }

        if (options.TargetSeesShield)
        {
            ShowNotification(target, "ExtensionDoctorNotificationShieldSaved", $"You were protected by the Doctor's shield from {attacker.Data.PlayerName}!");
        }
    }

    public static void ShowNotification(PlayerControl target, string notificationKey, string effectDescription = "")
    {
        if (target == null || !target.AmOwner)
        {
            return;
        }

        var baseMessage = TouLocale.GetParsed(notificationKey, notificationKey);
        var message = string.IsNullOrEmpty(effectDescription) ? baseMessage : $"{baseMessage} ({effectDescription})";
        var doctorColor = ColorUtility.ToHtmlStringRGBA(TouExtensionColors.Doctor);
        var notif = Helpers.CreateAndShowNotification(
            $"<b><color=#{doctorColor}>{message}</color></b>",
            Color.white,
            new Vector3(0f, 1f, -20f),
            spr: TouExtensionIcons.DoctorRoleIcon.LoadAsset());

        notif.AdjustNotification();
    }

    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (!@event.TriggeredByIntro)
        {
            return;
        }

        if (PlayerControl.LocalPlayer?.Data?.Role is not DoctorRole)
        {
            return;
        }

        var btn = CustomButtonSingleton<DoctorHealButton>.Instance;
        var options = OptionGroupSingleton<DoctorOptions>.Instance;
        btn.SetUses((int)options.InitialUses);
    }

    private class PendingHeal
    {
        public PlayerControl? Doctor { get; set; }
        public PlayerControl? Target { get; set; }
        public float Delay { get; set; }
        public float ScheduledTime { get; set; }
        public Guid HealId { get; set; }
        public int Seed { get; set; }
    }
}
