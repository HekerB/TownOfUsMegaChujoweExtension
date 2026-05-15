using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Extensions;
using TownOfUs.Assets;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Crewmate;

public sealed class ConfuserRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITownOfUsRole, IWikiDiscoverable
{
    public string LocaleKey => "Confuser";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Confuser");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Confuser;
    public ModdedRoleTeams Team => ModdedRoleTeams.Crewmate;
    public RoleAlignment RoleAlignment => RoleAlignment.CrewmateSupport;

    public CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TownOfUs.Assets.TouRoleIcons.Herbalist,
    };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(TouLocale.GetParsed("ExtensionRoleConfuserConfuse", "Confuse"),
            TouLocale.GetParsed("ExtensionRoleConfuserConfuseWikiDescription"),
            TownOfUs.Assets.TouRoleIcons.Herbalist)
    ];

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    [MethodRpc((uint)ExtensionRpc.ConfuserConfuse)]
    public static void RpcConfuse(PlayerControl confuser, PlayerControl victim)
    {
        if (victim == null) return;
        var options = OptionGroupSingleton<ConfuserOptions>.Instance;
        victim.AddModifier<ConfusedModifier>(options.ConfuseDuration);
        
        if (victim.AmOwner)
        {
            PirateDuelSystem.FlashScreen(TouExtensionColors.Confuser, 0.5f, 0.3f);
        }
    }
}
