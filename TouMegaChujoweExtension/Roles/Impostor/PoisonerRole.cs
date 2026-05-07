using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TownOfUs.Assets;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Impostor;

public sealed class PoisonerRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Insight;
    public string LocaleKey => "Poisoner";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
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
        Icon = TouRoleIcons.Poisoner,
        IntroSound = TouAudio.ViperIntroSound,
        CanUseVent = OptionGroupSingleton<PoisonerOptions>.Instance.CanVent,
        OptionsScreenshot = TouBanners.ImpostorRoleBanner,
    };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}Poison", "Poison"),
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}PoisonWikiDescription"),
            TouExtensionImpAssets.PoisonButtonSprite),
        new(
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}Vine", "Vine"),
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}VineWikiDescription"),
            TouExtensionImpAssets.VineButtonSprite)
    ];

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    [MethodRpc((uint)ExtensionRpc.PoisonerPoisonTarget)]
    public static void RpcPoisonTarget(PlayerControl poisoner, byte targetId)
    {
        var target = MiscUtils.PlayerById(targetId);
        if (target == null || target.Data.IsDead) return;
        PoisonSystem.StartPoison(poisoner.PlayerId, targetId);
    }
	
    [MethodRpc((uint)ExtensionRpc.PoisonerPlayDeathAnim)]
    public static void RpcPlayDeathAnim(PlayerControl sender, byte targetId)
    {
        PoisonDeathAnimSystem.TriggerDeathAnimation(targetId);
    }

    [MethodRpc((uint)ExtensionRpc.PoisonerVineTarget)]
    public static void RpcVineTarget(PlayerControl poisoner, byte targetId)
    {
        var target = MiscUtils.PlayerById(targetId);
        if (target == null || target.Data.IsDead) return;
        PoisonSystem.StartVine(poisoner.PlayerId, targetId);
    }

    // Kept for ExtensionRpc enum compatibility — no longer used for killing
    [MethodRpc((uint)ExtensionRpc.PoisonerKillTarget)]
    public static void RpcPoisonKill(PlayerControl poisoner, byte targetId)
    {
        // Kill is now handled by RpcSpecialMurder in PoisonSystem.ExecuteKill
    }
}
