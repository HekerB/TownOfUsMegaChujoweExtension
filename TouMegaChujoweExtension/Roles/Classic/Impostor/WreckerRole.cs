using Il2CppInterop.Runtime.Attributes;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using TownOfUs.Interfaces;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Impostor;

/// <summary>
/// Test role. An Impostor Power that smashes a hole through the wall it stands at, as long as that wall
/// leads somewhere players can walk (never the hull with space or sky behind it). Placeholder icons are
/// TOU-Mira's Miner ones.
/// </summary>
public sealed class WreckerRole(IntPtr cppPtr) : ImpostorRole(cppPtr), IExtensionRole, IWikiDiscoverable, IDoomable, ICrewVariant
{
    private const float HearingRange = 8f;

    public DoomableType DoomHintType => DoomableType.Relentless;
    public string LocaleKey => "Wrecker";
    public RoleBehaviour CrewVariant => CrewVariants.Of<TownOfUs.Roles.Crewmate.EngineerTouRole>();
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Wrecker");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");
    public Color RoleColor => TouExtensionColors.Wrecker;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorPower;

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = true,
        Icon = TouRoleIcons.Miner,
        IntroSound = TouAudio.ExplodeIntro,
        CanUseVent = true,
        OptionsScreenshot = TouBanners.ImpostorRoleBanner,
    };

    [HideFromIl2Cpp]
    public List<Type> RoleButtons => [typeof(WreckerSmashButton)];

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}Smash", "Smash"),
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}SmashWikiDescription"),
            TouImpAssets.MineSprite)
    ];

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription")
            + MiscUtils.AppendOptionsText(GetType());
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        if (player.AmOwner)
        {
            // Mapping the floor takes a few frames; start it now so the button works from the first round.
            WallBreakSystem.Prepare();
        }
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    [MethodRpc((uint)ExtensionRpc.WreckerSmash)]
    public static void RpcSmash(PlayerControl wrecker, Vector2 origin, Vector2 direction, float depth)
    {
        if (wrecker == null || wrecker.Data?.Role is not WreckerRole || wrecker.HasDied())
        {
            return;
        }

        WallBreakSystem.ApplyBreach(origin, direction, depth);

        var local = PlayerControl.LocalPlayer;
        var loud = OptionGroupSingleton<WreckerOptions>.Instance.SmashIsLoud;
        if (local != null && (wrecker.AmOwner || (loud && Vector2.Distance(local.GetTruePosition(), origin) <= HearingRange)))
        {
            TouAudio.PlaySound(TouAudio.BombExplode, 0.6f);
        }
    }
}
