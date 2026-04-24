using MiraAPI.GameOptions;
using MiraAPI.Networking;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Options;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Impostor;

public sealed class RcXdRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    [HideFromIl2Cpp] public RcXdCar? ActiveCar { get; set; }

    public DoomableType DoomHintType => DoomableType.Relentless;
    public string LocaleKey => "RcXd";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "RC-XD");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb", "Remote controlled explosive");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription", "Deploy an RC car and drive it into enemies");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription",
            "Deploy a remote-controlled car bomb. Use arrow keys to steer it and detonate near enemies.") +
               MiscUtils.AppendOptionsText(GetType());
    }
	
	[HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.GetParsed("ExtensionRoleRcXdDeploy", "Deploy"),
            TouLocale.GetParsed("ExtensionRoleRcXdDeployWikiDescription"),
            TouExtensionImpAssets.RcXdDeployButton),
        new(
            TouLocale.GetParsed("ExtensionRoleRcXdDetonate", "Detonate"),
            TouLocale.GetParsed("ExtensionRoleRcXdDetonateWikiDescription"),
            TouExtensionImpAssets.RcXdDetonateButton)
    ];

    public Color RoleColor => TouExtensionColors.RcXd;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorPower;

    public CustomRoleConfiguration Configuration => new(this)
	{
		UseVanillaKillButton = true,  // â† ZMIANA: zwykĹ‚y kill button obok ability
		Icon = TouExtensionIcons.RcXdRoleIcon,
		IntroSound = TouExtensionAudio.RCXDIntro,
		CanUseVent = OptionGroupSingleton<RcXdOptions>.Instance.CanVent,
	};

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        if (ActiveCar != null)
        {
            ActiveCar.DoDestroy();
            ActiveCar = null;
        }
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    [MethodRpc((uint)ExtensionRpc.RcXdDeploy)]
    public static void RpcDeployCar(PlayerControl player, Vector2 position)
    {
        if (LobbyBehaviour.Instance) return;
        if (player.Data?.Role is not RcXdRole role)
        {
            Error("RpcDeployCar - Not an RC-XD role");
            return;
        }

        var car = RcXdCar.Create(player, position);
        if (car != null)
        {
            role.ActiveCar = car;
        }
    }

    [MethodRpc((uint)ExtensionRpc.RcXdDetonate)]
    public static void RpcDetonateCar(PlayerControl player)
    {
        if (LobbyBehaviour.Instance) return;
        if (player.Data?.Role is not RcXdRole role) return;

        role.ActiveCar?.Detonate();
    }

    [MethodRpc((uint)ExtensionRpc.RcXdUpdatePosition)]
    public static void RpcUpdateCarPosition(PlayerControl player, float x, float y, float flip)
    {
        if (LobbyBehaviour.Instance) return;
        if (player.Data?.Role is not RcXdRole role) return;
        if (player.AmOwner) return;

        role.ActiveCar?.UpdatePosition(new Vector2(x, y), flip > 0.5f);
    }
}
