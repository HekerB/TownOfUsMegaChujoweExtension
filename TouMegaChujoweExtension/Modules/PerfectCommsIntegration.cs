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
using TownOfUs.Extensions;
using TownOfUs.Utilities;
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

    private static bool registered;
    private static bool evokerMufflesHearing = true;
    private static bool doctorInjectorMufflesHearing = true;
    private static string lastPhase = "Lobby";
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
            bridge.RegisterModTab(ModId, "ToU:Ch**owe");
            RegisterOptions();
            bridge.RegisterVoiceRule(ModId, ResolveRuleObject);
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
        bridge.RegisterHostEnumOption(
            ModId,
            SpiritMasterGhostVoice,
            Label(TouExtensionColors.SpiritMaster, "Spirit Master", "Ghost Link"),
            (int)GhostVoiceMode.Both,
            ["Off", Label(TouExtensionColors.SpiritMaster, "Spirit", "Talks to Ghost"), Label(Palette.White, "Ghost", "Talks to Spirit"), "Both Ways"]);
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

        if (ShouldMuteLivingPlayerForLocalGhost(ctx))
        {
            return bridge!.Mute("Living Players Muted While Dead");
        }

        if (ctx.Player == null || IsDead(ctx.Player))
        {
            return bridge!.PassResult;
        }

        bool voicePhase = ctx.Phase is "Tasks" or "Meeting";
        if (!voicePhase)
        {
            return bridge!.PassResult;
        }

        if (ctx.GetOption(HackerJamMutesVoice) && HackerSystem.IsJammed)
        {
            return bridge!.Mute("Hacker Jam");
        }

        if (ctx.GetOption(VoodooMutesVoice) && IsVoodooMutedForVoice(ctx))
        {
            return bridge!.Mute("Voodoo Muted");
        }

        if (!ctx.GetOption(PelicanBellyVoice) && PelicanSystem.IsSwallowed(ctx.Player.PlayerId))
        {
            return bridge!.Mute("Swallowed");
        }

        if (ctx.GetOption(MuteHiddenPlayers) && IsHiddenForVoice(ctx.Player))
        {
            return bridge!.Mute("Hidden");
        }

        return bridge!.PassResult;
    }

    private static bool ShouldMuteLivingPlayerForLocalGhost(VoiceContext ctx)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (ctx.Phase is not ("Tasks" or "Meeting") ||
            localPlayer == null ||
            ctx.Player == null ||
            IsDead(ctx.Player) ||
            !IsDead(localPlayer))
        {
            return false;
        }

        try
        {
            return LocalSettingsTabSingleton<TouExtensionLocalSettings>.Instance
                .MuteLivingPlayersWhileDead.Value;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsVoodooMutedForVoice(VoiceContext ctx)
    {
        bool hasActiveCurse = ctx.Player!.HasModifier<VoodooMutedModifier>();
        if (ctx.Phase == "Meeting")
        {
            if (hasActiveCurse)
            {
                meetingVoodooMutedPlayers.Add(ctx.Player.PlayerId);
            }

            return hasActiveCurse;
        }

        if (ctx.Phase != "Tasks")
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

    public static void ClearVoodooMute(byte playerId)
    {
        meetingVoodooMutedPlayers.Remove(playerId);
        nextRoundVoodooMutedPlayers.Remove(playerId);
    }

    private static void TrackPhaseTransition(string phase)
    {
        if (phase == lastPhase)
        {
            return;
        }

        if (lastPhase == "Meeting" && phase == "Tasks")
        {
            nextRoundVoodooMutedPlayers.Clear();
            foreach (byte playerId in meetingVoodooMutedPlayers)
            {
                nextRoundVoodooMutedPlayers.Add(playerId);
            }

            meetingVoodooMutedPlayers.Clear();
        }
        else if (phase is "Lobby" or "Exile")
        {
            meetingVoodooMutedPlayers.Clear();
            nextRoundVoodooMutedPlayers.Clear();
        }

        lastPhase = phase;
    }

    private static object? ResolvePelicanChannelObject(object context)
    {
        var ctx = new VoiceContext(context);
        if (!ctx.GetOption(PelicanBellyVoice) || !IsLiveVoicePhase(ctx) || IsDead(ctx.Player))
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

        if (!IsDead(ctx.Player) && ctx.Player!.GetRole<SpiritMasterRole>() is { MediatedPlayers.Count: > 0 })
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
        if (!ctx.GetOption(LawyerClientVoice) || !IsLiveVoicePhase(ctx) || IsDead(ctx.Player))
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
        if (!ctx.GetOption(RecruitVoice) || !IsLiveVoicePhase(ctx) || IsDead(ctx.Player))
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
        if (!ctx.GetOption(ApocalypseVoice) || !IsLiveVoicePhase(ctx) || IsDead(ctx.Player))
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
        evokerMufflesHearing = ctx.GetOption(EvokerMufflesHearing);
        doctorInjectorMufflesHearing = ctx.GetOption(DoctorInjectorMufflesHearing);
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

    private static object Radio(string key)
    {
        return bridge!.CreateChannelResult(key);
    }

    private static bool IsLiveVoicePhase(VoiceContext ctx)
    {
        return ctx.Phase is "Tasks" or "Meeting";
    }

    private static bool IsDead(PlayerControl? player)
    {
        return player?.Data == null || player.HasDied();
    }

    private enum GhostVoiceMode
    {
        None,
        SpiritMasterToGhost,
        GhostToSpiritMaster,
        Both,
    }

    private sealed class VoiceContext
    {
        private readonly object context;
        private readonly Type contextType;

        public VoiceContext(object context)
        {
            this.context = context;
            contextType = context.GetType();
            Player = (PlayerControl?)contextType.GetProperty("Player")?.GetValue(context);
            Phase = contextType.GetProperty("Phase")?.GetValue(context)?.ToString() ?? "Lobby";
        }

        public PlayerControl? Player { get; }

        public string Phase { get; }

        public bool GetOption(string key)
        {
            var getter = contextType.GetProperty("GetOption")?.GetValue(context) as Delegate;
            return getter != null && (bool)getter.DynamicInvoke(key)!;
        }

        public int GetEnumOption(string key)
        {
            var getter = contextType.GetProperty("GetEnumOption")?.GetValue(context) as Delegate;
            return getter != null ? (int)getter.DynamicInvoke(key)! : 0;
        }
    }

    private sealed class PerfectCommsBridge
    {
        private readonly Type apiType;
        private readonly Type ruleContextType;
        private readonly Type ruleResultType;
        private readonly Type channelResultType;
        private readonly Type audioShapeType;
        private readonly Type hostOptionType;
        private readonly Type hostEnumOptionType;

        private PerfectCommsBridge(Assembly assembly)
        {
            apiType = GetRequiredType(assembly, "PerfectComms.Api.PerfectCommsApi");
            ruleContextType = GetRequiredType(assembly, "PerfectComms.Api.VoiceRuleContext");
            ruleResultType = GetRequiredType(assembly, "PerfectComms.Api.VoiceRuleResult");
            channelResultType = GetRequiredType(assembly, "PerfectComms.Api.VoiceChannelResult");
            audioShapeType = GetRequiredType(assembly, "PerfectComms.Api.VoiceAudioShape");
            hostOptionType = GetRequiredType(assembly, "PerfectComms.Api.VoiceHostOption");
            hostEnumOptionType = GetRequiredType(assembly, "PerfectComms.Api.VoiceHostEnumOption");
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

        private static Type GetRequiredType(Assembly assembly, string typeName)
        {
            return assembly.GetType(typeName)
                   ?? throw new TypeLoadException($"Perfect Comms API type not found: {typeName}");
        }
    }
}
