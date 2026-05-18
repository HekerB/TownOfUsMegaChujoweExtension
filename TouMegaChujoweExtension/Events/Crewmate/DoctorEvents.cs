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
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;
using TouMegaChujoweExtension.Modifiers.Crewmate;
using TownOfUs;
using TownOfUs.Events;
using TownOfUs.Modifiers;

namespace TouMegaChujoweExtension.Events.Crewmate;

public static class DoctorEvents
{
    private static readonly Dictionary<byte, List<PendingInject>> PendingInjects = [];

    public static void ScheduleInject(PlayerControl doctor, PlayerControl target, int seed)
    {
        if (target == null || target.HasDied() || doctor == null)
        {
            return;
        }

        var options = OptionGroupSingleton<DoctorOptions>.Instance;
        var delay = options.EffectDelay;

        var pending = new PendingInject
        {
            Doctor = doctor,
            Target = target,
            Delay = delay,
            ScheduledTime = Time.time,
            InjectId = Guid.NewGuid(),
            Seed = seed
        };

        if (!PendingInjects.TryGetValue(target.PlayerId, out var list))
        {
            list = [];
            PendingInjects[target.PlayerId] = list;
        }
        list.Add(pending);
        Coroutines.Start(CoApplyInject(pending));
    }

    private static IEnumerator CoApplyInject(PendingInject pending)
    {
        yield return new WaitForSeconds(pending.Delay);

        if (pending.Target == null || pending.Target.HasDied() || pending.Doctor == null || pending.Doctor.HasDied())
        {
            if (pending.Target != null && PendingInjects.TryGetValue(pending.Target.PlayerId, out var list))
            {
                list.RemoveAll(p => p.InjectId == pending.InjectId);
                if (list.Count == 0)
                {
                    PendingInjects.Remove(pending.Target.PlayerId);
                }
            }
            yield break;
        }

        ApplyInjectEffect(pending.Doctor, pending.Target, pending.Seed);

        if (PendingInjects.TryGetValue(pending.Target.PlayerId, out var list2))
        {
            list2.RemoveAll(p => p.InjectId == pending.InjectId);
            if (list2.Count == 0)
            {
                PendingInjects.Remove(pending.Target.PlayerId);
            }
        }
    }

    private static void ApplyInjectEffect(PlayerControl doctor, PlayerControl target, int seed)
    {
        if (target == null || target.HasDied())
        {
            return;
        }

        var options = OptionGroupSingleton<DoctorOptions>.Instance;
        var duration = options.EffectDuration;
        var durationType = options.EffectDurationType.Value;

        List<(float Weight, Func<BaseModifier> CreateModifier, string NotificationKey, string Desc)> effects = [];

        effects.Add((options.ChanceSpeedBoost, new Func<BaseModifier>(() => new DoctorSpeedBoostModifier(duration, durationType)), "ExtensionDoctorNotificationSpeedBoost", "Increased movement speed"));
        effects.Add((options.ChanceVisionBoost, new Func<BaseModifier>(() => new DoctorVisionBoostModifier(duration, durationType)), "ExtensionDoctorNotificationVisionBoost", "Increased vision"));
        effects.Add((options.ChanceCleanse, new Func<BaseModifier>(() => new DoctorCleanseModifier()), "ExtensionDoctorNotificationCleanse", "Removed negative effects"));
        effects.Add((options.ChanceShield, new Func<BaseModifier>(() => new DoctorShieldModifier(doctor, duration, durationType)), "ExtensionDoctorNotificationShield", "Protected from one attack"));
        effects.Add((options.ChanceCanVent, new Func<BaseModifier>(() => new DoctorCanVentModifier(duration, durationType)), "ExtensionDoctorNotificationCanVent", "Can use vents"));
        effects.Add((options.ChanceRegeneration, new Func<BaseModifier>(() => new DoctorRegenerationModifier(duration, durationType)), "ExtensionDoctorNotificationRegeneration", "Cooldowns recover faster"));

        // Injector negative effects if enabled
        if (options.CanGiveNegativeEffects)
        {
            effects.Add((options.ChanceNegativeSlowness, new Func<BaseModifier>(() => new TouMegaChujoweExtension.Modifiers.Impostor.InjectedSlownessModifier(duration, TouMegaChujoweExtension.Options.Roles.Impostor.InjectorEffectDurationType.SetTime)), "ExtensionInjectorNotificationSlowness", "Slowness"));
            effects.Add((options.ChanceNegativeLowVision, new Func<BaseModifier>(() => new TouMegaChujoweExtension.Modifiers.Impostor.InjectedLowVisionModifier(duration, TouMegaChujoweExtension.Options.Roles.Impostor.InjectorEffectDurationType.SetTime)), "ExtensionInjectorNotificationLowVision", "Low Vision"));
            effects.Add((options.ChanceNegativeConfused, new Func<BaseModifier>(() => new TouMegaChujoweExtension.Modifiers.Impostor.InjectedConfusedModifier(duration, TouMegaChujoweExtension.Options.Roles.Impostor.InjectorEffectDurationType.SetTime)), "ExtensionInjectorNotificationConfused", "Confused"));
        }

        var totalWeight = effects.Sum(e => e.Weight);

        if (totalWeight <= 0f)
        {
            // Default to Speed Boost if nothing is configured
            var defaultMod = new DoctorSpeedBoostModifier(duration, durationType);
            target.AddModifier(defaultMod);
            if (doctor != null && doctor.PlayerId != target.PlayerId)
            {
                ShowNotification(doctor, "ExtensionDoctorNotificationSpeedBoost", "Increased movement speed");
            }
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
            if (doctor != null && doctor.PlayerId != target.PlayerId)
            {
                ShowNotification(doctor, selectedNotificationKey, selectedDesc);
            }

            // Start coroutine to remove ClericBarrierModifier if it was selected and durationType is SetTime
            // TimedModifiers handle their own duration removal
        }
    }


    public static void ShieldAttacked(PlayerControl doctor, PlayerControl attacker, PlayerControl target)
    {
        var options = OptionGroupSingleton<DoctorOptions>.Instance;

        if (options.DoctorSeesShield)
        {
            ShowNotification(doctor, "ExtensionDoctorNotificationShieldAttacked", $"Your shield on {target.Data.PlayerName} protected them from {attacker.Data.PlayerName}!");
        }

        if (options.TargetSeesShield)
        {
            ShowNotification(target, "ExtensionDoctorNotificationShieldSaved", $"You were protected by the Doctor's shield from {attacker.Data.PlayerName}!");
        }
    }

    [RegisterEvent]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        var target = @event.Target;
        var source = @event.Source;

        if (target == null || source == null || MeetingHud.Instance || ExileController.Instance) return;

        if (target.TryGetModifier<DoctorShieldModifier>(out var shield))
        {
            if (target.PlayerId == source.PlayerId) return;

            if (source.HasModifier<IndirectAttackerModifier>()) return;

            MiscUtils.LogInfo(TownOfUsEventHandlers.LogLevel.Error, $"{target.Data.PlayerName} has a doctor shield, stopping an interaction from {source.Data.PlayerName}!");
            @event.Cancel();

            if (shield.Doctor != null && (TutorialManager.InstanceExists || source.AmOwner))
            {
                DoctorRole.RpcDoctorShieldAttacked(shield.Doctor, target, source);
            }
            target.RemoveModifier(shield);
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

        var btn = CustomButtonSingleton<DoctorInjectButton>.Instance;
        var options = OptionGroupSingleton<DoctorOptions>.Instance;
        btn.SetUses((int)options.InitialUses);
    }

    private sealed class PendingInject
    {
        public PlayerControl? Doctor { get; set; }
        public PlayerControl? Target { get; set; }
        public float Delay { get; set; }
        public float ScheduledTime { get; set; }
        public Guid InjectId { get; set; }
        public int Seed { get; set; }
    }
}
