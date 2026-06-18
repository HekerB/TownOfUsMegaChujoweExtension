using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using BepInEx.Unity.IL2CPP;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Crewmate;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TouMegaChujoweExtension.Utilities;
using TownOfUs;
using TownOfUs.Modules.Localization;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using TownOfUs.Utilities.Appearances;
using UnityEngine;

namespace TouMegaChujoweExtension.Modules;

public static class PerfectCommsIntegration
{
    public const string PerfectCommsPluginId = "com.edgetel.perfectcomms";
    private const string ModId = TouMegaChujoweExtensionPlugin.Id;

    private const string PelicanBellyVoice = "PelicanBellyVoice";
    private const string RecruitVoice = "RecruitVoice";
    private const string LawyerClientVoice = "LawyerClientVoice";
    private const string ApocalypseVoice = "ApocalypseVoice";
    private const string SpiritMasterGhostVoice = "SpiritMasterGhostVoice";
    private const string HackerJamMutesVoice = "HackerJamMutesVoice";
    private const string VoodooMutesVoice = "VoodooMutesVoice";
    private const string VoodooMuteNextRound = "VoodooMuteNextRound";
    private const string MuteHiddenPlayers = "MuteHiddenPlayers";
    private const string EvokerMufflesHearing = "EvokerMufflesHearing";
    private const string DoctorInjectorMufflesHearing = "DoctorInjectorMufflesHearing";
    private const string MuteMorphedPlayers = "MuteMorphedPlayers";
    private static bool registered;
    private static bool evokerMufflesHearing = true;
    private static bool doctorInjectorMufflesHearing = true;
    private static bool hackerJamMutesVoice = true;
    private static VoicePhase lastPhase = VoicePhase.Lobby;
    private static readonly HashSet<byte> meetingVoodooMutedPlayers = [];
    private static readonly HashSet<byte> nextRoundVoodooMutedPlayers = [];
    private static PerfectCommsBridge? bridge;

    public static void Register()
    {
        if (registered)
        {
            Info("[PerfectCommsIntegration] Already registered.");
            return;
        }

        bool loaded = IL2CPPChainloader.Instance.Plugins.ContainsKey(PerfectCommsPluginId);
        Info($"[PerfectCommsIntegration] Register requested. Perfect Comms loaded: {loaded}");
        if (!loaded)
        {
            return;
        }

        try
        {
            bridge = PerfectCommsBridge.Create();
            bridge.Unregister(ModId);
            bridge.RegisterModTab(ModId, TouMegaChujoweExtensionPlugin.CensorVisibleText("ToU: Chujowe"));
            RegisterOptions();
            bridge.RegisterVoiceRule(ModId, ResolveRuleObject);
            bridge.RegisterGlobalGate(ModId, VoicePhase.Tasks, IsHackerJamVoiceGateActive, "Hacker Jam");
            bridge.RegisterGlobalGate(ModId, VoicePhase.Meeting, IsHackerJamVoiceGateActive, "Hacker Jam");
            bridge.RegisterListenerFilter(ModId, ShouldMuffleLocalListener);
            bridge.RegisterVoiceChannel(ModId, ResolvePelicanChannelObject);
            bridge.RegisterVoiceChannel(ModId, ResolveSpiritMasterChannelObject);
            bridge.RegisterVoiceChannel(ModId, ResolveLawyerChannelObject);
            bridge.RegisterVoiceChannel(ModId, ResolveRecruitChannelObject);
            bridge.RegisterVoiceChannel(ModId, ResolveApocalypseChannelObject);

            registered = true;
            Info("[PerfectCommsIntegration] Registered TouMCE voice options and rules.");
        }
        catch (Exception ex)
        {
            try
            {
                bridge?.Unregister(ModId);
            }
            catch
            {
                // Registration already failed; cleanup is best-effort.
            }

            bridge = null;
            Error($"[PerfectCommsIntegration] Registration failed: {ex}");
        }
    }

    private static void RegisterOptions()
    {
        if (bridge == null)
        {
            return;
        }

        bridge.RegisterHostOption(ModId, HackerJamMutesVoice, Label(TouExtensionColors.Hacker, "Hacker", "Jam Silences Voice"), true);
        bridge.RegisterHostOption(ModId, VoodooMutesVoice, Label(Palette.ImpostorRed, "Voodoo", "Curse Mutes Voice"), true);
        bridge.RegisterHostOption(ModId, VoodooMuteNextRound, Label(Palette.ImpostorRed, "Voodoo", "Curse Carries Next Round"), false);
        bridge.RegisterHostOption(ModId, MuteHiddenPlayers, Label(TouExtensionColors.Vanisher, "Hidden Players", "Stay Muted"), true);
        bridge.RegisterHostOption(
            ModId,
            DoctorInjectorMufflesHearing,
            $"{RoleName(TouExtensionColors.Injector, "Injector")}{RoleName(Palette.White, "/")}{RoleName(TouExtensionColors.Doctor, "Doctor")}: Negative Effects Muffle Hearing",
            true);

        bridge.RegisterHostOption(ModId, PelicanBellyVoice, Label(TouExtensionColors.Pelican, "Pelican", "Can Chat With Lunch"), true);
        bridge.RegisterHostOption(ModId, RecruitVoice, Label(TouExtensionColors.Jackal, "Recruit", "Team Radio"), true);
        bridge.RegisterHostOption(ModId, LawyerClientVoice, Label(TownOfUsColors.Lawyer, "Lawyer Duo", "Private Radio"), true);
        bridge.RegisterHostOption(ModId, ApocalypseVoice, Label(TouExtensionColors.Death, "Apocalypse", "Horsemen Radio"), true);

        bridge.RegisterHostOption(ModId, EvokerMufflesHearing, Label(TouExtensionColors.Evoker, "Evoker", "Blindness Muffles Hearing"), true);
        bridge.RegisterHostOption(ModId, MuteMorphedPlayers, Label(Palette.ImpostorRed, TouLocale.Get("ExtensionOptionPCMorphedCamouflagedGroup", "Morphed"), TouLocale.Get("ExtensionOptionPCMorphedCamouflagedMute", "Stay Muted")), true);
        bridge.RegisterHostEnumOption(
            ModId,
            SpiritMasterGhostVoice,
            Label(TouExtensionColors.SpiritMaster, "Spirit Master", "Ghost Link"),
            (int)GhostVoiceMode.Both,
            ["Off", "Both Ways"]);
    }

    private static string Label(Color color, string roleName, string suffix)
    {
        return $"{RoleName(color, roleName)}: {suffix}";
    }

    private static string RoleName(Color color, string roleName)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{roleName}</color>";
    }

    private static object ResolveRuleObject(object context)
    {
        var ctx = new VoiceContext(context);
        SyncListenerFilterOptions(ctx);
        TrackPhaseTransition(ctx.Phase);

        if (ctx.Player == null || ctx.IsDead)
        {
            return bridge!.PassResult;
        }

        bool voicePhase = ctx.Phase is VoicePhase.Tasks or VoicePhase.Meeting;
        if (!voicePhase)
        {
            return bridge!.PassResult;
        }

        if (MiraAPI.LocalSettings.LocalSettingsTabSingleton<TouExtensionLocalSettings>.Instance?.MuteAliveWhenGhost.Value ?? true)
        {
            var localPlayer = PlayerControl.LocalPlayer;
            if (localPlayer != null && IsDead(localPlayer))
            {
                return bridge!.Mute("Dead");
            }
        }

        var local = PlayerControl.LocalPlayer;
        if (local != null)
        {
            byte localId = local.PlayerId;
            byte speakerId = ctx.Player.PlayerId;

            bool localSwallowed = PelicanSystem.IsSwallowed(localId);
            bool speakerSwallowed = PelicanSystem.IsSwallowed(speakerId);

            if (localSwallowed || speakerSwallowed)
            {
                bool canHear = false;
                if (ctx.GetOption(PelicanBellyVoice))
                {
                    byte? localPelican = PelicanSystem.GetPelicanOf(localId);
                    byte? speakerPelican = PelicanSystem.GetPelicanOf(speakerId);

                    canHear = (localPelican.HasValue && localPelican.Value == speakerId) ||
                              (speakerPelican.HasValue && speakerPelican.Value == localId) ||
                              (localPelican.HasValue && speakerPelican.HasValue && localPelican.Value == speakerPelican.Value);
                }
                if (!canHear)
                {
                    return bridge!.Mute("Swallowed");
                }
            }
        }

        if (ctx.GetOption(VoodooMutesVoice) && IsVoodooMutedForVoice(ctx))
        {
            return bridge!.Mute("Voodoo Muted");
        }

        if (ctx.GetOption(MuteHiddenPlayers) && IsHiddenForVoice(ctx.Player))
        {
            return bridge!.Mute("Hidden");
        }

        if (ctx.GetOption(MuteMorphedPlayers) && IsDisguisedOrCamouflaged(ctx.Player))
        {
            return bridge!.Mute("Disguised");
        }

        return bridge!.PassResult;
    }

    private static bool IsVoodooMutedForVoice(VoiceContext ctx)
    {
        bool hasActiveCurse = ctx.Player!.HasModifier<VoodooMutedModifier>();
        if (ctx.Phase == VoicePhase.Meeting)
        {
            if (hasActiveCurse)
            {
                meetingVoodooMutedPlayers.Add(ctx.Player.PlayerId);
            }

            return hasActiveCurse;
        }

        if (ctx.Phase != VoicePhase.Tasks)
        {
            return false;
        }

        if (!ctx.GetOption(VoodooMuteNextRound))
        {
            nextRoundVoodooMutedPlayers.Clear();
            return false;
        }

        return hasActiveCurse || nextRoundVoodooMutedPlayers.Contains(ctx.Player.PlayerId);
    }

    private static void TrackPhaseTransition(VoicePhase phase)
    {
        if (phase == lastPhase)
        {
            return;
        }

        if (phase == VoicePhase.Tasks &&
            lastPhase is VoicePhase.Meeting or VoicePhase.Exile)
        {
            nextRoundVoodooMutedPlayers.Clear();
            foreach (byte playerId in meetingVoodooMutedPlayers)
            {
                nextRoundVoodooMutedPlayers.Add(playerId);
            }

            meetingVoodooMutedPlayers.Clear();
        }
        else if (phase == VoicePhase.Meeting)
        {
            nextRoundVoodooMutedPlayers.Clear();
        }
        else if (phase == VoicePhase.Lobby)
        {
            meetingVoodooMutedPlayers.Clear();
            nextRoundVoodooMutedPlayers.Clear();
        }

        lastPhase = phase;
    }

    private static object? ResolvePelicanChannelObject(object context)
    {
        var ctx = new VoiceContext(context);
        if (!ctx.GetOption(PelicanBellyVoice) || !IsLiveVoicePhase(ctx) || ctx.IsDead)
        {
            return null;
        }

        byte playerId = ctx.Player!.PlayerId;
        byte? pelicanId = PelicanSystem.GetPelicanOf(playerId);
        if (pelicanId.HasValue)
        {
            return Radio($"pelican:{pelicanId.Value}");
        }

        if (ctx.Player.IsRole<PelicanRole>() && PelicanSystem.GetSwallowedByPelican(playerId).Count > 0)
        {
            return Radio($"pelican:{playerId}");
        }

        return null;
    }

    private static object? ResolveSpiritMasterChannelObject(object context)
    {
        var ctx = new VoiceContext(context);
        if (!IsLiveVoicePhase(ctx))
        {
            return null;
        }

        var mode = (GhostVoiceMode)ctx.GetEnumOption(SpiritMasterGhostVoice);
        if (mode == GhostVoiceMode.None)
        {
            return null;
        }

        if (!ctx.IsDead && ctx.Player!.GetRole<SpiritMasterRole>() is { MediatedPlayers.Count: > 0 })
        {
            return Radio($"spirit-master:{ctx.Player.PlayerId}");
        }

        if (ctx.Player!.TryGetModifier<SpiritMasterMediatedModifier>(out var mediated))
        {
            return Radio($"spirit-master:{mediated.SpiritMasterId}");
        }

        return null;
    }

    private static object? ResolveLawyerChannelObject(object context)
    {
        var ctx = new VoiceContext(context);
        if (!ctx.GetOption(LawyerClientVoice) || !IsLiveVoicePhase(ctx) || ctx.IsDead)
        {
            return null;
        }

        if (ctx.Player!.IsRole<LawyerRole>() && LawyerUtils.GetClientForLawyer(ctx.Player) != null)
        {
            return Radio($"lawyer:{ctx.Player.PlayerId}");
        }

        var target = ctx.Player.GetModifiers<LawyerTargetModifier>().FirstOrDefault();
        return target != null ? Radio($"lawyer:{target.OwnerId}") : null;
    }

    private static object? ResolveRecruitChannelObject(object context)
    {
        var ctx = new VoiceContext(context);
        if (!ctx.GetOption(RecruitVoice) || !IsLiveVoicePhase(ctx) || ctx.IsDead)
        {
            return null;
        }

        if (ctx.Player!.IsRole<JackalRole>())
        {
            return Radio($"jackal:{ctx.Player.PlayerId}");
        }

        var sidekick = ctx.Player.GetModifier<SidekickModifier>();
        return sidekick != null && sidekick.JackalId != byte.MaxValue
            ? Radio($"jackal:{sidekick.JackalId}")
            : null;
    }

    private static object? ResolveApocalypseChannelObject(object context)
    {
        var ctx = new VoiceContext(context);
        if (!ctx.GetOption(ApocalypseVoice) || !IsLiveVoicePhase(ctx) || ctx.IsDead)
        {
            return null;
        }

        return ApocalypseUtils.IsApocalypsePlayer(ctx.Player!) ? Radio("apocalypse") : null;
    }

    private static bool ShouldMuffleLocalListener(PlayerControl local)
    {
        return local != null &&
               !IsDead(local) &&
               ((evokerMufflesHearing && local.HasModifier<EvokerBlindedModifier>()) ||
                (doctorInjectorMufflesHearing && HasDoctorInjectorNegativeEffect(local)));
    }

    private static void SyncListenerFilterOptions(VoiceContext ctx)
    {
        hackerJamMutesVoice = ctx.GetOption(HackerJamMutesVoice);
        evokerMufflesHearing = ctx.GetOption(EvokerMufflesHearing);
        doctorInjectorMufflesHearing = ctx.GetOption(DoctorInjectorMufflesHearing);
    }

    private static bool IsHackerJamVoiceGateActive()
    {
        return hackerJamMutesVoice && HackerSystem.IsJammed;
    }

    private static bool HasDoctorInjectorNegativeEffect(PlayerControl player)
    {
        return player.HasModifier<InjectedConfusedModifier>() ||
               player.HasModifier<InjectedInvertedControlsModifier>() ||
               player.HasModifier<InjectedLowVisionModifier>() ||
               player.HasModifier<InjectedNauseaModifier>() ||
               player.HasModifier<InjectedNoReportModifier>() ||
               player.HasModifier<InjectedNoUseModifier>() ||
               player.HasModifier<InjectedNoVentModifier>() ||
               player.HasModifier<InjectedSlownessModifier>() ||
               player.HasModifier<InjectedVeryLowVisionModifier>() ||
               player.HasModifier<InjectedWeaknessModifier>();
    }

    private static bool IsHiddenForVoice(PlayerControl player)
    {
        return player.HasModifier<AstralInvisibilityModifier>() ||
               player.HasModifier<AstralPhaseModifier>() ||
               player.HasModifier<BurrowerInvisibleModifier>() ||
               player.HasModifier<DeathInvisibleModifier>() ||
               player.HasModifier<SpeedyAccelerateModifier>() ||
               player.HasModifier<VanishModifier>() ||
               player.HasModifier<WraithLanternInvisibilityModifier>();
    }

    private static bool IsDisguisedOrCamouflaged(PlayerControl player)
    {
        return player.HasModifier<TownOfUs.Modifiers.Impostor.MorphlingMorphModifier>() ||
               player.HasModifier<TownOfUs.Modifiers.Neutral.GlitchMimicModifier>() ||
               player.HasModifier<DoppelgangerDisguiseModifier>();
    }

    private static object Radio(string key)
    {
        return bridge!.CreateChannelResult(key);
    }

    private static bool IsLiveVoicePhase(VoiceContext ctx)
    {
        return ctx.Phase is VoicePhase.Tasks or VoicePhase.Meeting;
    }

    private static bool IsDead(PlayerControl? player)
    {
        return player?.Data == null || player.HasDied();
    }

    private enum GhostVoiceMode
    {
        None,
        Both,
    }

    private enum VoicePhase
    {
        Lobby,
        Tasks,
        Meeting,
        Exile,
    }

    private readonly struct VoiceContext
    {
        private readonly Func<string, bool>? getOption;
        private readonly Func<string, int>? getEnumOption;

        public VoiceContext(object context)
        {
            var values = bridge!.ReadContext(context);
            Player = values.Player;
            Phase = values.Phase;
            IsDead = values.IsDead;
            getOption = values.GetOption;
            getEnumOption = values.GetEnumOption;
        }

        public PlayerControl? Player { get; }

        public VoicePhase Phase { get; }

        public bool IsDead { get; }

        public bool GetOption(string key)
        {
            return getOption?.Invoke(key) ?? false;
        }

        public int GetEnumOption(string key)
        {
            return getEnumOption?.Invoke(key) ?? 0;
        }
    }

    private sealed class PerfectCommsBridge
    {
        private readonly Type apiType;
        private readonly Type ruleContextType;
        private readonly Type ruleResultType;
        private readonly Type phaseType;
        private readonly Type channelResultType;
        private readonly Type audioShapeType;
        private readonly Type hostOptionType;
        private readonly Type hostEnumOptionType;
        private readonly Func<object, PlayerControl?> readPlayer;
        private readonly Func<object, int> readPhase;
        private readonly Func<object, bool> readIsDead;
        private readonly Func<object, Func<string, bool>?> readGetOption;
        private readonly Func<object, Func<string, int>?> readGetEnumOption;

        private PerfectCommsBridge(Assembly assembly)
        {
            apiType = GetRequiredType(assembly, "PerfectComms.Api.PerfectCommsApi");
            ruleContextType = GetRequiredType(assembly, "PerfectComms.Api.VoiceRuleContext");
            ruleResultType = GetRequiredType(assembly, "PerfectComms.Api.VoiceRuleResult");
            phaseType = GetRequiredType(assembly, "PerfectComms.Api.VoicePhaseKind");
            channelResultType = GetRequiredType(assembly, "PerfectComms.Api.VoiceChannelResult");
            audioShapeType = GetRequiredType(assembly, "PerfectComms.Api.VoiceAudioShape");
            hostOptionType = GetRequiredType(assembly, "PerfectComms.Api.VoiceHostOption");
            hostEnumOptionType = GetRequiredType(assembly, "PerfectComms.Api.VoiceHostEnumOption");
            readPlayer = BuildPropertyReader<PlayerControl?>(ruleContextType, "Player");
            readPhase = BuildEnumPropertyReader(ruleContextType, "Phase");
            readIsDead = BuildPropertyReader<bool>(ruleContextType, "IsDead");
            readGetOption = BuildPropertyReader<Func<string, bool>?>(ruleContextType, "GetOption");
            readGetEnumOption = BuildPropertyReader<Func<string, int>?>(ruleContextType, "GetEnumOption");
            PassResult = ruleResultType.GetField("Pass", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
                         ?? throw new MissingMemberException(ruleResultType.FullName, "Pass");
        }

        public object PassResult { get; }

        public static PerfectCommsBridge Create()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(candidate => candidate.GetName().Name == "PerfectComms")
                ?? Assembly.Load("PerfectComms");

            return new PerfectCommsBridge(assembly);
        }

        public void RegisterModTab(string modId, string tabLabel)
        {
            InvokeApi(nameof(RegisterModTab), modId, tabLabel);
        }

        public void RegisterHostOption(string modId, string key, string label, bool defaultValue)
        {
            var option = Activator.CreateInstance(hostOptionType, key, label, defaultValue);
            InvokeApi(nameof(RegisterHostOption), modId, option!);
        }

        public void RegisterHostEnumOption(string modId, string key, string label, int defaultValue, string[] choices)
        {
            var option = Activator.CreateInstance(hostEnumOptionType, key, label, defaultValue, choices);
            InvokeApi(nameof(RegisterHostEnumOption), modId, option!);
        }

        public void RegisterVoiceRule(string modId, Func<object, object> rule)
        {
            var delegateType = typeof(Func<,>).MakeGenericType(ruleContextType, ruleResultType);
            var callback = BuildObjectCallback(delegateType, ruleContextType, ruleResultType, rule);
            InvokeApi(nameof(RegisterVoiceRule), modId, callback);
        }

        public void RegisterGlobalGate(string modId, VoicePhase phase, Func<bool> isActive, string reason)
        {
            object apiPhase = Enum.ToObject(phaseType, (int)phase);
            InvokeApi(nameof(RegisterGlobalGate), modId, apiPhase, isActive, reason);
        }

        public void RegisterVoiceChannel(string modId, Func<object, object?> channel)
        {
            var delegateType = typeof(Func<,>).MakeGenericType(ruleContextType, channelResultType);
            var callback = BuildObjectCallback(delegateType, ruleContextType, channelResultType, channel);
            InvokeApi(nameof(RegisterVoiceChannel), modId, callback);
        }

        public void RegisterListenerFilter(string modId, Func<PlayerControl, bool> shouldMuffle)
        {
            var delegateType = typeof(Func<,>).MakeGenericType(typeof(PlayerControl), typeof(bool));
            var callback = Delegate.CreateDelegate(delegateType, shouldMuffle.Target, shouldMuffle.Method);
            InvokeApi(nameof(RegisterListenerFilter), modId, callback);
        }

        public object Mute(string reason)
        {
            return ruleResultType.GetMethod("Mute", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, [reason])
                   ?? PassResult;
        }

        public object CreateChannelResult(string key)
        {
            object radioShape = Enum.Parse(audioShapeType, "Radio");
            return Activator.CreateInstance(channelResultType, key, true, radioShape, 1f, null)
                   ?? throw new InvalidOperationException("Could not create Perfect Comms channel result.");
        }

        public void Unregister(string modId)
        {
            InvokeApi(nameof(Unregister), modId);
        }

        public ContextValues ReadContext(object context)
        {
            var phase = (VoicePhase)readPhase(context);
            return new ContextValues(
                readPlayer(context),
                phase,
                readIsDead(context),
                readGetOption(context),
                readGetEnumOption(context));
        }

        private void InvokeApi(string methodName, params object[] args)
        {
            var method = apiType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
                         ?? throw new MissingMethodException(apiType.FullName, methodName);
            method.Invoke(null, args);
        }

        private static Delegate BuildObjectCallback(
            Type delegateType,
            Type parameterType,
            Type returnType,
            Delegate callback)
        {
            var parameter = Expression.Parameter(parameterType, "context");
            var invoke = Expression.Invoke(Expression.Constant(callback), Expression.Convert(parameter, typeof(object)));
            var body = Expression.Convert(invoke, returnType);
            return Expression.Lambda(delegateType, body, parameter).Compile();
        }

        private static Func<object, T> BuildPropertyReader<T>(Type declaringType, string propertyName)
        {
            var property = declaringType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
                           ?? throw new MissingMemberException(declaringType.FullName, propertyName);
            var value = Expression.Parameter(typeof(object), "value");
            var typedValue = Expression.Convert(value, declaringType);
            var propertyValue = Expression.Property(typedValue, property);
            var converted = Expression.Convert(propertyValue, typeof(T));
            return Expression.Lambda<Func<object, T>>(converted, value).Compile();
        }

        private static Func<object, int> BuildEnumPropertyReader(Type declaringType, string propertyName)
        {
            var property = declaringType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
                           ?? throw new MissingMemberException(declaringType.FullName, propertyName);
            var value = Expression.Parameter(typeof(object), "value");
            var typedValue = Expression.Convert(value, declaringType);
            var propertyValue = Expression.Property(typedValue, property);
            var converted = Expression.Convert(propertyValue, typeof(int));
            return Expression.Lambda<Func<object, int>>(converted, value).Compile();
        }

        private static Type GetRequiredType(Assembly assembly, string typeName)
        {
            return assembly.GetType(typeName)
                   ?? throw new TypeLoadException($"Perfect Comms API type not found: {typeName}");
        }

        public readonly record struct ContextValues(
            PlayerControl? Player,
            VoicePhase Phase,
            bool IsDead,
            Func<string, bool>? GetOption,
            Func<string, int>? GetEnumOption);
    }
}
