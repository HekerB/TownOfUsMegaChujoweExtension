using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using Reactor.Utilities;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Impostor;

public sealed class WraithRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Hunter;
    public string LocaleKey => "Wraith";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Wraith;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorPower;

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = true,
        Icon = TouRoleIcons.Wraith,
        IntroSound = TouAudio.PhantomIntroSound,
        CanUseVent = OptionGroupSingleton<WraithOptions>.Instance.CanVent
    };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities
    {
        get
        {
            return
            [
                new(
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}Dash", "Dash"),
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}DashWikiDescription"),
                    TouImpAssets.SprintSprite),
                new(
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}Lantern", "Lantern"),
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}LanternWikiDescription"),
                    TouExtensionImpAssets.LanternButtonSprite)
            ];
        }
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        WraithLanternSystem.ClearForPlayer(targetPlayer.PlayerId);
    }

    [MethodRpc((uint)ExtensionRpc.WraithPlaceLantern)]
    public static void RpcWraithPlaceLantern(PlayerControl wraith, Vector2 pos)
    {
        if (wraith?.Data?.Role is not WraithRole)
        {
            return;
        }

        var opts = OptionGroupSingleton<WraithOptions>.Instance;
        if (!opts.LanternEnabled)
        {
            return;
        }

        WraithLanternSystem.PlaceLantern(wraith.PlayerId, pos, opts.LanternDuration.Value);
    }

    [MethodRpc((uint)ExtensionRpc.WraithReturnLantern)]
    public static void RpcWraithReturnLantern(PlayerControl wraith, Vector2 pos)
    {
        if (wraith?.Data?.Role is not WraithRole)
        {
            return;
        }

        if (!WraithLanternSystem.TryReturnLantern(wraith.PlayerId, out var markedPos))
        {
            return;
        }

        if (Vector2.Distance(markedPos, pos) > 0.25f)
        {
            pos = markedPos;
        }

        var invisDuration = OptionGroupSingleton<WraithOptions>.Instance.InvisibleDuration.Value;
        if (invisDuration > 0f && !wraith.HasDied())
        {
            wraith.AddModifier<WraithLanternInvisibilityModifier>();
        }

        if (wraith.AmOwner)
        {
            TouAudio.PlaySound(TouAudio.SwooperActivateSound);
        }

        Coroutines.Start(CoDelayedTeleport(wraith, pos));
    }

    private static System.Collections.IEnumerator CoDelayedTeleport(PlayerControl wraith, Vector2 pos)
    {
        yield return new WaitForSeconds(0.08f);

        if (wraith == null || wraith.HasDied())
        {
            yield break;
        }

        wraith.transform.position = pos;
        wraith.MyPhysics.ResetMoveState();
        wraith.NetTransform.SnapTo(pos);
        if (wraith.AmOwner)
        {
            PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(pos);
        }
    }

    [MethodRpc((uint)ExtensionRpc.WraithBreakLantern)]
    public static void RpcWraithBreakLantern(PlayerControl wraith, Vector2 pos)
    {
        if (wraith == null)
        {
            return;
        }

        if (PlayerControl.LocalPlayer != null)
        {
            TouAudio.PlaySound(TouExtensionAudio.LanternBreakSound);
        }

        WraithLanternSystem.BreakLantern(wraith.PlayerId, pos);
    }
}
