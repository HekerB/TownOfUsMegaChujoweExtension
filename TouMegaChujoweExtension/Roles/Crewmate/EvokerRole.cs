using TownOfUs.Assets;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Networking;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Crewmate;

public sealed class EvokerRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Hunter;
    public string LocaleKey => "Evoker";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Evoker");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

	public override bool IsAffectedByComms => false;

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") +
               MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Evoker;
    public ModdedRoleTeams Team => ModdedRoleTeams.Crewmate;
    public RoleAlignment RoleAlignment => RoleAlignment.CrewmateSupport;

    public int VerifiesUsed { get; set; }

    public CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TouExtensionIcons.EvokerRoleIcon,
        IntroSound = TownOfUs.Assets.TouAudio.PhantomIntroSound,
        OptionsScreenshot = TouBanners.CrewmateRoleBanner
    };

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        VerifiesUsed = 0;
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        VerifiesUsed = 0;
        EvokerSystem.EndBlind();
    }

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(TouLocale.GetParsed($"ExtensionRole{LocaleKey}Blind", "Blind"),
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}BlindWikiDescription"),
            TouExtensionCrewAssets.EvokerBlindButtonSprite),
        new(TouLocale.GetParsed($"ExtensionRole{LocaleKey}Verify", "Verify"),
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}VerifyWikiDescription"),
            TouExtensionCrewAssets.EvokerVerifyButtonSprite)
    ];

[MethodRpc((uint)ExtensionRpc.EvokerBlind)]
public static void RpcEvokerBlind(PlayerControl evoker, float duration)
{
    if (evoker.Data.Role is not EvokerRole)
    {
        return;
    }

    if (evoker.HasDied())
    {
        return;
    }

    EvokerSystem.StartBlind(evoker.PlayerId, duration);
}

    [MethodRpc((uint)ExtensionRpc.EvokerBlindEnd)]
    public static void RpcEvokerBlindEnd(PlayerControl evoker)
    {
        EvokerSystem.EndBlind();
    }

    [MethodRpc((uint)ExtensionRpc.EvokerVerify)]
    public static void RpcEvokerVerify(PlayerControl evoker, byte targetId)
    {
        if (evoker.Data.Role is EvokerRole role)
        {
            role.VerifiesUsed++;
        }
    }
}
