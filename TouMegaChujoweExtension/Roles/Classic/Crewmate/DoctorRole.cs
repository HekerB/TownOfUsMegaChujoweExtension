using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using Reactor.Utilities.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Events.Crewmate;
using TouMegaChujoweExtension.Networking;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Modifiers.Crewmate;
using MiraAPI.Modifiers;
using Reactor.Utilities;

namespace TouMegaChujoweExtension.Roles.Crewmate;

public sealed class DoctorRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Insight;
    public string LocaleKey => "Doctor";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Doctor");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Doctor;
    public ModdedRoleTeams Team => ModdedRoleTeams.Crewmate;
    public RoleAlignment RoleAlignment => RoleAlignment.CrewmateSupport;

    public CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TouExtensionIcons.DoctorRoleIcon,
        IntroSound = TouAudio.ScientistIntroSound,
    };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.GetParsed("ExtensionRoleDoctorInject", "Inject"),
            TouLocale.GetParsed("ExtensionRoleDoctorInjectWikiDescription"),
            TouMegaChujoweExtension.Assets.TouExtensionCrewAssets.DoctorInjectButtonSprite)
    ];

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    public override bool CanUse(IUsable usable)
    {
        return GameManager.Instance.LogicUsables.CanUse(usable, Player);
    }

    [MethodRpc((uint)ExtensionRpc.DoctorInject)]
    public static void RpcDoctorInject(PlayerControl doctor, PlayerControl target, int seed)
    {
        if (target == null) return;

        var random = new System.Random(seed);
        var options = OptionGroupSingleton<DoctorOptions>.Instance;

        // Pick effect based on chances
        var effects = new List<DoctorEffectType>();
        if (options.ChanceSpeedBoost > 0) effects.Add(DoctorEffectType.SpeedBoost);
        if (options.ChanceVisionBoost > 0) effects.Add(DoctorEffectType.VisionBoost);
        if (options.ChanceCleanse > 0) effects.Add(DoctorEffectType.Cleanse);
        if (options.ChanceShield > 0) effects.Add(DoctorEffectType.Shield);
        if (options.ChanceCanVent > 0) effects.Add(DoctorEffectType.CanVent);
        if (options.ChanceRegeneration > 0) effects.Add(DoctorEffectType.Regeneration);

        if (effects.Count == 0) return;

        // Weighted random
        float totalWeight = effects.Sum(e => options.GetEffectChance(e));
        float r = (float)random.NextDouble() * totalWeight;

        DoctorEffectType selected = effects[0];
        float currentWeight = 0;
        foreach (var e in effects)
        {
            currentWeight += options.GetEffectChance(e);
            if (r <= currentWeight)
            {
                selected = e;
                break;
            }
        }

        // Apply effect after delay
        Coroutines.Start(CoApplyEffect(doctor, target, selected, options.EffectDelay));
    }

    private static System.Collections.IEnumerator CoApplyEffect(PlayerControl doctor, PlayerControl target, DoctorEffectType effect, float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);
        if (target == null || target.Data.IsDead) yield break;

        var options = OptionGroupSingleton<DoctorOptions>.Instance;
        var duration = options.EffectDuration;
        var durationType = options.EffectDurationType.Value;

        switch (effect)
        {
            case DoctorEffectType.SpeedBoost:
                target.AddModifier<DoctorSpeedBoostModifier>(duration, durationType);
                break;
            case DoctorEffectType.VisionBoost:
                target.AddModifier<DoctorVisionBoostModifier>(duration, durationType);
                break;
            case DoctorEffectType.Cleanse:
                target.AddModifier<DoctorCleanseModifier>();
                break;
            case DoctorEffectType.Shield:
                target.AddModifier<DoctorShieldModifier>(doctor, duration, durationType);
                break;
            case DoctorEffectType.CanVent:
                target.AddModifier<DoctorCanVentModifier>(duration, durationType);
                break;
            case DoctorEffectType.Regeneration:
                target.AddModifier<DoctorRegenerationModifier>(duration, durationType);
                break;
        }
    }

    [MethodRpc((uint)ExtensionRpc.DoctorShieldAttacked)]
    public static void RpcDoctorShieldAttacked(PlayerControl doctor, PlayerControl target)
    {
        // Logika powiadomienia o ataku na tarczę
    }
}
