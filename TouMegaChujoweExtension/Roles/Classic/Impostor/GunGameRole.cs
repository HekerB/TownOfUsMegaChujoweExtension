using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Modules;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Impostor;

public sealed class GunGameRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public string LocaleKey => "GunGame";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Gun Game");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");
    public Color RoleColor => Palette.ImpostorRed;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorPower;
    public DoomableType DoomHintType => DoomableType.Relentless;


    public static Dictionary<byte, int> CurrentChainIndex { get; } = [];
    public static Dictionary<byte, int> CurrentKillCount { get; } = [];
    public static Dictionary<byte, List<ushort>> RememberedRoleIds { get; } = [];
    public static HashSet<ushort> UsedImpostorRoleIds { get; } = [];

    public static readonly List<Type> Chain =
    [
        typeof(SniperRole),
        typeof(RcXdRole),
        typeof(DetonatorRole),
        typeof(TownOfUs.Roles.Impostor.BomberRole)
    ];

    private static readonly List<Type> RandomImpostorRoles =
    [
        typeof(AstralRole),
        typeof(CharlatanRole),
        typeof(DetonatorRole),
        typeof(HackerRole),
        typeof(InjectorRole),
        typeof(KamikazeRole),
        typeof(OutlawRole),
        typeof(PoisonerRole),
        typeof(RcXdRole),
        typeof(SniperRole),
        typeof(SpeedyRole),
        typeof(WitchRole),
        typeof(WraithRole),
        typeof(TownOfUs.Roles.Impostor.AmbassadorRole),
        typeof(TownOfUs.Roles.Impostor.AmbusherRole),
        typeof(TownOfUs.Roles.Impostor.BlackmailerRole),
        typeof(TownOfUs.Roles.Impostor.BomberRole),
        typeof(TownOfUs.Roles.Impostor.EclipsalRole),
        typeof(TownOfUs.Roles.Impostor.EscapistRole),
        typeof(TownOfUs.Roles.Impostor.GrenadierRole),
        typeof(TownOfUs.Roles.Impostor.HypnotistRole),
        typeof(TownOfUs.Roles.Impostor.JanitorRole),
        typeof(TownOfUs.Roles.Impostor.MinerRole),
        typeof(TownOfUs.Roles.Impostor.MorphlingRole),
        typeof(TownOfUs.Roles.Impostor.ParasiteRole),
        typeof(TownOfUs.Roles.Impostor.PuppeteerRole),
        typeof(TownOfUs.Roles.Impostor.ScavengerRole),
        typeof(TownOfUs.Roles.Impostor.SpellslingerRole),
        typeof(TownOfUs.Roles.Impostor.SwooperRole),
        typeof(TownOfUs.Roles.Impostor.TraitorRole),
        typeof(TownOfUs.Roles.Impostor.UndertakerRole),
        typeof(TownOfUs.Roles.Impostor.VenererRole),
        typeof(TownOfUs.Roles.Impostor.WarlockRole)
    ];

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = true,
        Icon = TouExtensionIcons.GunGameRoleIcon,
        IntroSound = TouAudio.WarlockIntroSound,
        CanUseVent = true,
        OptionsScreenshot = TouBanners.ImpostorRoleBanner
    };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities => [];

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var stringB = ITownOfUsRole.SetNewTabText(this);
        stringB.AppendLine(GetProgressText());
        return stringB;
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);

        if (!CurrentChainIndex.ContainsKey(player.PlayerId))
        {
            CurrentChainIndex[player.PlayerId] = 0;
        }

        if (!CurrentKillCount.ContainsKey(player.PlayerId))
        {
            CurrentKillCount[player.PlayerId] = 0;
        }

        if (player.AmOwner && !player.HasModifier<GunGameModifier>())
        {
            player.RpcAddModifier<GunGameModifier>();
        }
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    [HideFromIl2Cpp]
    public static void ResetState()
    {
        CurrentChainIndex.Clear();
        CurrentKillCount.Clear();
        RememberedRoleIds.Clear();
        UsedImpostorRoleIds.Clear();
    }

    [HideFromIl2Cpp]
    public static void TriggerMutationLocal()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data.IsDead)
        {
            return;
        }

        var options = OptionGroupSingleton<GunGameOptions>.Instance;
        var nextRoleId = GetNextRoleId(
            local.PlayerId,
            options.RemoveExistingImpostorRoles,
            Mathf.Max(0, (int)options.MaxRememberedRoles),
            out var nextChainIndex);
        RpcMutate(local, nextRoleId, nextChainIndex);
    }

    private static ushort GetNextRoleId(byte playerId, bool removeExistingImpostorRoles, int maxRememberedRoles, out int nextChainIndex)
    {
        var rememberedRoles = GetRememberedRoles(playerId);

        if (maxRememberedRoles > 0 && rememberedRoles.Count >= maxRememberedRoles)
        {
            var randomIndex = UnityEngine.Random.Range(0, rememberedRoles.Count);
            nextChainIndex = randomIndex;
            return rememberedRoles[randomIndex];
        }

        if (removeExistingImpostorRoles)
        {
            RegisterExistingImpostorRoles();
        }

        // Lethal chain option removed; always use random role selection.
        // The original chain logic is omitted.

        nextChainIndex = 0;
        var rolePool = removeExistingImpostorRoles
            ? RandomImpostorRoles.Where(role => !UsedImpostorRoleIds.Contains(RoleId.Get(role)) && !rememberedRoles.Contains(RoleId.Get(role)) && !IsPowerRole(role)).ToList()
            : RandomImpostorRoles.Where(role => !rememberedRoles.Contains(RoleId.Get(role)) && !IsPowerRole(role)).ToList();

        if (rolePool.Count == 0)
        {
            rolePool = RandomImpostorRoles.Where(role => !IsPowerRole(role)).ToList();
        }

        var randomRoleType = rolePool[UnityEngine.Random.Range(0, rolePool.Count)];
        var nextRoleId = RoleId.Get(randomRoleType);

        if (maxRememberedRoles > 0)
        {
            rememberedRoles.Add(nextRoleId);
            nextChainIndex = rememberedRoles.Count >= maxRememberedRoles ? 0 : rememberedRoles.Count;
        }

        return nextRoleId;
    }

    [HideFromIl2Cpp]
    private static List<ushort> GetRememberedRoles(byte playerId)
    {
        if (!RememberedRoleIds.TryGetValue(playerId, out var roles))
        {
            roles = [];
            RememberedRoleIds[playerId] = roles;
        }

        return roles;
    }

    [HideFromIl2Cpp]
    public static void RegisterExistingImpostorRoles()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            var role = player?.GetRoleWhenAlive();
            if (IsTrackedImpostorRole(role))
            {
                UsedImpostorRoleIds.Add((ushort)role.Role);
            }
        }

    }

    private static bool IsTrackedImpostorRole(RoleBehaviour? role)
    {
        return role != null && role.IsImpostor() && role.Role is not RoleTypes.Impostor && role is not GunGameRole;
    }

    private static bool IsPowerRole(Type roleType)
    {
        var roleBehaviour = RoleManager.Instance.GetRole((RoleTypes)RoleId.Get(roleType));
        return roleBehaviour is ITownOfUsRole touRole && touRole.RoleAlignment == RoleAlignment.ImpostorPower;
    }

    private static string GetProgressText()
    {
        return TouLocale.GetParsed("ExtensionRoleGunGameProgressRandom", "Mode: Random role after each kill");
    }

    [MethodRpc((uint)ExtensionRpc.GunGameMutate, LocalHandling = RpcLocalHandling.Before)]
    public static void RpcMutate(PlayerControl player, ushort nextRoleId, int nextChainIndex)
    {
        if (player == null)
        {
            return;
        }

        CurrentChainIndex[player.PlayerId] = nextChainIndex;
        CurrentKillCount[player.PlayerId] = 0;
        UsedImpostorRoleIds.Add(nextRoleId);

        if (!player.HasModifier<GunGameModifier>())
        {
            player.AddModifier<GunGameModifier>();
        }

        if (player.AmOwner)
        {
            player.RpcChangeRole(nextRoleId, false);
            ApplyMutationCooldowns(player);
            ShowMutationNotification(nextRoleId);
        }
    }

    [HideFromIl2Cpp]
    private static void ApplyMutationCooldowns(PlayerControl player)
    {
        if (player == null)
        {
            return;
        }

        var role = player.Data?.Role;
        if (role == null)
        {
            return;
        }

        if (role.CanUseKillButton)
        {
            player.SetKillTimer(player.GetKillCooldown());
        }

        if (!HudManager.InstanceExists)
        {
            return;
        }

        foreach (var button in CustomButtonManager.Buttons)
        {
            if (button == null || !button.Enabled(role))
            {
                continue;
            }

            if (button.Button == null)
            {
                button.CreateButton(HudManager.Instance.transform);
            }

            button.SetActive(true, role);
            button.SetTimer(button.Cooldown);
        }
    }

    private static void ShowMutationNotification(ushort nextRoleId)
    {
        var roleName = TouLocale.Get("RoleImpostor", "Impostor");
        var role = RoleManager.Instance.GetRole((RoleTypes)nextRoleId);
        if (role is ITownOfUsRole touRole)
        {
            roleName = touRole.RoleName;
        }
        else if (role != null)
        {
            roleName = role.GetRoleName();
        }

        var roleIcon = role != null
            ? role.GetRoleIcon()
            : TouExtensionIcons.GunGameRoleIcon.LoadAsset();
        var impostorRoleName = $"{Palette.ImpostorRed.ToTextColor()}{roleName}</color>";

        var text = TouLocale.GetParsed("ExtensionRoleGunGameMutationNotification", "ROLE CHANGED! You are now <role>!")
            .Replace("<role>", impostorRoleName);

        try
        {
            MiraAPI.Utilities.Helpers.CreateAndShowNotification(
                text,
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: roleIcon)?.AdjustNotification();
        }
        catch
        {
            HudManager.Instance?.Notifier.AddDisconnectMessage(text);
        }
    }
}
