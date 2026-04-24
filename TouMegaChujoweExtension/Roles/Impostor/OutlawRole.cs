using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Impostor;

public sealed class OutlawRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Death;
    public string LocaleKey => "Outlaw";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Outlaw");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb", "Double the trouble");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription", "Kill two players in quick succession");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription",
            "The Outlaw can perform a double kill. After killing a player, they have a short window to kill a second player without cooldown. If the window expires, the normal kill cooldown applies.") +
               MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Outlaw;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorKilling;

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = false,
        Icon = TouExtensionIcons.OutlawRole,
		IntroSound = TouAudio.WarlockIntroSound,
    };

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }
}
