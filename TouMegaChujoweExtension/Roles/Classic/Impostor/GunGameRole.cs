using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Roles;
using MiraAPI.Patches.Stubs;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using AmongUs.GameOptions;
using UnityEngine;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Roles.Impostor;

namespace TouMegaChujoweExtension.Roles.Classic.Impostor;

public sealed class GunGameRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable
{
    public string LocaleKey => "GunGame";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Gun Game");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => Palette.ImpostorRed;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorPower;

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = true,
        Icon = TouExtensionIcons.GunGameRoleIcon,
        IntroSound = TouAudio.ViperIntroSound,
        CanUseVent = OptionGroupSingleton<GunGameOptions>.Instance.CanVent,
        OptionsScreenshot = TouBanners.ImpostorRoleBanner
    };

    [HideFromIl2Cpp]
    public List<Type> RoleButtons => new List<Type>(); // Uses mutated role's buttons

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities => new List<CustomButtonWikiDescription>();

    /// <summary>
    /// Tracks chain index per player.
    /// </summary>
    public static Dictionary<byte, int> CurrentChainIndex { get; } = new();

    /// <summary>
    /// The fixed chain order:
    /// Sniper -> RC-XD -> Detonator -> Bomber(C4) -> vanilla Impostor (final)
    /// </summary>
    public static readonly List<Type> Chain = new()
    {
        typeof(SniperRole),
        typeof(RcXdRole),
        typeof(DetonatorRole),
        typeof(TownOfUs.Roles.Impostor.BomberRole),
    };

    /// <summary>
    /// All impostor roles for random mode (when UseLethalChain is false).
    /// </summary>
    private static readonly List<Type> AllImpostorRoles = new()
    {
        typeof(AstralRole),
        typeof(CharlatanRole),
        typeof(DetonatorRole),
        typeof(DumperRole),
        typeof(HackerRole),
        typeof(InjectorRole),
        typeof(KamikazeRole),
        typeof(OutlawRole),
        typeof(PoisonerRole),
        typeof(RcXdRole),
        typeof(SandwormRole),
        typeof(SniperRole),
        typeof(SpeedyRole),
        typeof(TomahawkRole),
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
    };

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        if (player.AmOwner)
        {
            CurrentChainIndex[player.PlayerId] = 0;
            if (!player.HasModifier<TouMegaChujoweExtension.Modifiers.Impostor.GunGameModifier>())
            {
                player.RpcAddModifier<TouMegaChujoweExtension.Modifiers.Impostor.GunGameModifier>();
            }
        }
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    [HideFromIl2Cpp]
    public static void TriggerMutationLocal()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data.IsDead) return;

        var options = OptionGroupSingleton<GunGameOptions>.Instance;
        ushort nextRoleId;
        int nextChainIndex = 0;

        if (options.UseLethalChain)
        {
            if (!CurrentChainIndex.TryGetValue(local.PlayerId, out int idx)) idx = 0;

            if (idx < Chain.Count)
            {
                var nextRoleType = Chain[idx];
                nextRoleId = RoleId.Get(nextRoleType);
                nextChainIndex = idx + 1;
            }
            else
            {
                // Final stage: become vanilla Impostor
                nextRoleId = (ushort)RoleTypes.Impostor;
                nextChainIndex = idx + 1; // Don't increment further
            }
        }
        else
        {
            // Pick a random enabled Impostor role
            var rnd = new System.Random();
            var randomRoleType = AllImpostorRoles[rnd.Next(AllImpostorRoles.Count)];
            nextRoleId = RoleId.Get(randomRoleType);
            nextChainIndex = 0;
        }

        RpcMutate(local, nextRoleId, nextChainIndex);
    }

    [Reactor.Networking.Attributes.MethodRpc((uint)ExtensionRpc.GunGameMutate, LocalHandling = Reactor.Networking.Rpc.RpcLocalHandling.Before)]
    public static void RpcMutate(PlayerControl player, ushort nextRoleId, int nextChainIndex)
    {
        if (player == null) return;

        CurrentChainIndex[player.PlayerId] = nextChainIndex;
        if (!player.HasModifier<TouMegaChujoweExtension.Modifiers.Impostor.GunGameModifier>())
        {
            player.AddModifier<TouMegaChujoweExtension.Modifiers.Impostor.GunGameModifier>();
        }

        if (player.AmOwner)
        {
            player.RpcChangeRole(nextRoleId, false);
        }

        if (player.AmOwner)
        {
            var roleName = "a new role";
            if (player.Data?.Role != null)
            {
                if (player.Data.Role is ITownOfUsRole touRole)
                    roleName = touRole.RoleName;
                else if (player.Data.Role.Role == RoleTypes.Impostor)
                    roleName = "Impostor";
            }
            try
            {
                MiraAPI.Utilities.Helpers.CreateAndShowNotification(
                    $"ROLE CHANGED! You are now {roleName}!", 
                    Color.white, 
                    new Vector3(0f, 1f, -20f), 
                    spr: TouExtensionIcons.GunGameRoleIcon.LoadAsset())?.AdjustNotification();
            }
            catch
            {
                if (HudManager.Instance != null)
                    HudManager.Instance.Notifier.AddDisconnectMessage($"ROLE CHANGED! You are now {roleName}!");
            }
        }
    }
}
