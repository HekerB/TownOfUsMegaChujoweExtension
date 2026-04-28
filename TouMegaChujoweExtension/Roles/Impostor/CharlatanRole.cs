using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Roles.Impostor;
using TownOfUs.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMegaChujoweExtension.Roles.Impostor;

public sealed class CharlatanRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Trickster;
    public string LocaleKey => "Charlatan";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Charlatan;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorSupport;

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = true,
        Icon = TouExtensionIcons.CharlatanRole
    };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities
    {
        get
        {
            var abilities = new List<CustomButtonWikiDescription>();
            var options = OptionGroupSingleton<CharlatanOptions>.Instance;

            if (options.DeceiveEnabled)
            {
                abilities.Add(new(
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}Deceive", "Deceive"),
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}DeceiveWikiDescription"),
                    TouExtensionImpAssets.DeceiveButtonSprite));
            }

            abilities.Add(new(
                TouLocale.GetParsed($"ExtensionRole{LocaleKey}Conceal", "Conceal"),
                TouLocale.GetParsed($"ExtensionRole{LocaleKey}ConcealWikiDescription"),
                TouExtensionImpAssets.ConcealButtonSprite));

            return abilities;
        }
    }

    public void LobbyStart()
    {
        CharlatanConcealSystem.ClearAll();
        CharlatanDeceiveSystem.ClearAll();
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        CharlatanConcealSystem.ClearForPlayer(targetPlayer.PlayerId);
        CharlatanDeceiveSystem.ClearForPlayer(targetPlayer.PlayerId);
    }

    [MethodRpc((uint)ExtensionRpc.CharlatanConceal)]
    public static void RpcCharlatanConceal(PlayerControl charlatan, byte bodyId)
    {
        if (charlatan?.Data?.Role is not CharlatanRole)
        {
            return;
        }

        var body = Object.FindObjectsOfType<DeadBody>().FirstOrDefault(x => x.ParentId == bodyId);
        if (body == null)
        {
            return;
        }

        var bodyPlayer = MiscUtils.PlayerById(bodyId);
        if (bodyPlayer != null)
        {
            MiscUtils.RemovePet(bodyPlayer);
        }

        var options = OptionGroupSingleton<CharlatanOptions>.Instance;
        CharlatanConcealSystem.ConcealBody(charlatan.PlayerId, bodyId, options.ConcealDelay);
    }
}
