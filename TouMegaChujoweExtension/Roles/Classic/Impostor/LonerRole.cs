using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using System.Text;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Hud;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Rpc;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Modifiers.Game.Impostor;
using TownOfUs.Modules;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Roles.Impostor;
using TownOfUs.Utilities;
using UnityEngine;


namespace TouMegaChujoweExtension.Roles.Classic.Impostor;

public sealed class LonerRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable, ITraitorIgnore
{
    private static readonly HashSet<byte> RecruitedLoners = [];
    private static readonly HashSet<byte> MutatedLoners = [];
    private static readonly HashSet<byte> PendingMutations = [];

    public static Dictionary<byte, int> CurrentKillCount { get; } = [];
    public static HashSet<ushort> UsedImpostorRoleIds { get; } = [];

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

    public DoomableType DoomHintType => DoomableType.Trickster;
    public bool IsIgnored => true;
    public string LocaleKey => "Loner";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Loner");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public Color RoleColor => Palette.ImpostorRed;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorPower;

    public CustomRoleConfiguration Configuration => new(this)
    {
        MaxRoleCount = 1,
        UseVanillaKillButton = true,
        CanUseVent = true,
        Icon = TouExtensionIcons.LonerRoleIcon,
        IntroSound = TouAudio.GlitchSound,
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
        var options = OptionGroupSingleton<LonerOptions>.Instance;
        if (!options.ChangeRoleAfterKills || HasMutated(Player))
        {
            return stringB;
        }

        var needed = Mathf.Clamp((int)options.KillsNeededToChangeRole.Value, 1, 5);
        var done = HasRecruited(Player)
            ? Mathf.Clamp(CurrentKillCount.GetValueOrDefault(Player.PlayerId), 0, needed)
            : 0;

        stringB.AppendLine("<b>" + TouLocale.GetParsed("ExtensionRoleLonerTabKillsAfterRecruit", "Kills After Recruit: {0} / {1}")
            .Replace("{0}", done.ToString())
            .Replace("{1}", needed.ToString()) + "</b>");
        return stringB;
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        if (player.AmOwner)
        {
            if (!player.HasModifier<LonerModifier>())
            {
                player.RpcAddModifier<LonerModifier>();
            }

            ButtonResetPatches.ResetCooldowns();
            player.SetKillTimer(player.GetKillCooldown());
        }
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    [HideFromIl2Cpp]
    public static bool HasRecruited(PlayerControl player)
    {
        return player != null && RecruitedLoners.Contains(player.PlayerId);
    }

    [HideFromIl2Cpp]
    public static void ResetState()
    {
        RecruitedLoners.Clear();
        MutatedLoners.Clear();
        PendingMutations.Clear();
        CurrentKillCount.Clear();
        UsedImpostorRoleIds.Clear();
    }

    [HideFromIl2Cpp]
    public static bool HasMutated(PlayerControl player)
    {
        return player != null && MutatedLoners.Contains(player.PlayerId);
    }

    [HideFromIl2Cpp]
    public static void MarkMutationPending(PlayerControl player)
    {
        if (player != null && !HasMutated(player))
        {
            PendingMutations.Add(player.PlayerId);
        }
    }

    [HideFromIl2Cpp]
    public static bool HasPendingMutation(PlayerControl player)
    {
        return player != null && PendingMutations.Contains(player.PlayerId);
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

    [HideFromIl2Cpp]
    public static void TriggerMutationLocal()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data.IsDead || HasMutated(local))
        {
            return;
        }

        var options = OptionGroupSingleton<LonerOptions>.Instance;
        var nextRoleId = GetNextRoleId(options.RemoveExistingImpostorRoles);
        RpcMutate(local, nextRoleId);
    }

    [HideFromIl2Cpp]
    public static void TriggerPendingMutationLocal()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !HasPendingMutation(local))
        {
            return;
        }

        PendingMutations.Remove(local.PlayerId);
        TriggerMutationLocal();
    }

    [MethodRpc((uint)ExtensionRpc.LonerRecruit)]
    public static void RpcRecruit(PlayerControl loner, PlayerControl target)
    {
        if (loner == null || target == null || loner.Data?.Role is not LonerRole || HasRecruited(loner))
        {
            return;
        }

        if (loner.HasDied() || target.HasDied() || target.IsImpostor() || target.Data?.Disconnected == true)
        {
            return;
        }

        RecruitedLoners.Add(loner.PlayerId);
        PendingMutations.Remove(loner.PlayerId);
        CurrentKillCount[loner.PlayerId] = 0;
        var options = OptionGroupSingleton<LonerOptions>.Instance;
        var roleId = options.RecruitBecomesTraitor
            ? RoleId.Get<TraitorRole>()
            : (ushort)RoleTypes.Impostor;

        target.ChangeRole(roleId, recordRole: false);

        if (options.RecruitedImpostorBecomesAssassin && !target.HasModifier<ImpostorAssassinModifier>())
        {
            target.AddModifier<ImpostorAssassinModifier>();
        }

        if (target.AmOwner)
        {
            ButtonResetPatches.ResetCooldowns();
            target.SetKillTimer(target.GetKillCooldown());
        }

        ShowRecruitNotification(loner, target);
    }

    private static ushort GetNextRoleId(bool removeExistingImpostorRoles)
    {
        if (removeExistingImpostorRoles)
        {
            RegisterExistingImpostorRoles();
        }

        var rolePool = removeExistingImpostorRoles
            ? RandomImpostorRoles.Where(role => !UsedImpostorRoleIds.Contains(RoleId.Get(role)) && !IsDisallowedMutationRole(role)).ToList()
            : RandomImpostorRoles.Where(role => !IsDisallowedMutationRole(role)).ToList();

        if (rolePool.Count == 0)
        {
            rolePool = RandomImpostorRoles.Where(role => !IsDisallowedMutationRole(role)).ToList();
        }

        return RoleId.Get(rolePool[UnityEngine.Random.Range(0, rolePool.Count)]);
    }

    private static bool IsTrackedImpostorRole(RoleBehaviour? role)
    {
        return role != null &&
               role.IsImpostor() &&
               role.Role is not RoleTypes.Impostor &&
               role is not LonerRole &&
               role is not GunGameRole;
    }

    private static bool IsDisallowedMutationRole(Type roleType)
    {
        var roleBehaviour = RoleManager.Instance.GetRole((RoleTypes)RoleId.Get(roleType));
        return roleBehaviour is ITownOfUsRole touRole &&
               touRole.RoleAlignment is RoleAlignment.ImpostorPower or RoleAlignment.ImpostorKilling;
    }

    [MethodRpc((uint)ExtensionRpc.LonerMutate, LocalHandling = RpcLocalHandling.Before)]
    public static void RpcMutate(PlayerControl player, ushort nextRoleId)
    {
        if (player == null || HasMutated(player))
        {
            return;
        }

        MutatedLoners.Add(player.PlayerId);
        PendingMutations.Remove(player.PlayerId);
        CurrentKillCount[player.PlayerId] = 0;
        UsedImpostorRoleIds.Add(nextRoleId);

        if (!player.HasModifier<LonerModifier>())
        {
            player.AddModifier<LonerModifier>();
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
        var role = player?.Data?.Role;
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
            : TouExtensionIcons.LonerRoleIcon.LoadAsset();
        var impostorRoleName = $"{Palette.ImpostorRed.ToTextColor()}{roleName}</color>";

        var text = TouLocale.GetParsed("ExtensionRoleLonerMutationNotification", "Your Loner cover broke. You are now <role>!")
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

    private static void ShowRecruitNotification(PlayerControl loner, PlayerControl target)
    {
        if (!loner.AmOwner && !target.AmOwner)
        {
            return;
        }

        var text = loner.AmOwner
            ? TouLocale.GetParsed("ExtensionRoleLonerRecruitSuccess", "You recruited <player> as an Impostor!")
                .Replace("<player>", target.Data.PlayerName)
            : TouLocale.GetParsed("ExtensionRoleLonerRecruited", "You were recruited by the Loner!");

        try
        {
            MiraAPI.Utilities.Helpers.CreateAndShowNotification(
                text,
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TouExtensionIcons.LonerRoleIcon.LoadAsset())?.AdjustNotification();
        }
        catch
        {
            HudManager.Instance?.Notifier.AddDisconnectMessage(text);
        }
    }
}
