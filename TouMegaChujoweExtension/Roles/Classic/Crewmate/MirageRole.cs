using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using Reactor.Utilities;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Crewmate;

public sealed class MirageRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable, IUnguessable
{
    public DoomableType DoomHintType => DoomableType.Insight;
    internal static readonly Dictionary<byte, List<string>> TriggeredRoles = [];

    public string LocaleKey => "Mirage";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public override bool IsAffectedByComms => false;

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities
    {
        get
        {
            return
            [
                new(
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}Decoy", "Decoy"),
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}DecoyWikiDescription"),
                    TouExtensionCrewAssets.DecoyButtonSprite)
            ];
        }
    }

    public Color RoleColor => TouExtensionColors.Mirage;
    public ModdedRoleTeams Team => ModdedRoleTeams.Crewmate;
    public RoleAlignment RoleAlignment => RoleAlignment.CrewmateSupport;

    public CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TouExtensionIcons.MirageRoleIcon,
        IntroSound = TouAudio.NoisemakerIntroSound,
        OptionsScreenshot = TouExtensionBanners.MirageBanner,
    };
    public bool IsGuessable => true;
    public RoleBehaviour AppearAs => this;

    public void LobbyStart()
    {
        MirageDecoySystem.ClearAll();
        TriggeredRoles.Clear();
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        MirageDecoySystem.ClearForPlayer(targetPlayer.PlayerId);
    }

    [MethodRpc((uint)ExtensionRpc.MiragePlaceDecoy)]
    public static void RpcMiragePlaceDecoy(
        PlayerControl mirage,
        PlayerControl appearanceSource,
        Vector2 pos,
        float z,
        float durationSeconds)
    {
        if (mirage?.Data?.Role is not MirageRole)
        {
            return;
        }

        if (mirage.AmOwner)
        {
            TouAudio.PlaySound(TouExtensionAudio.DecoyPlaceSound);
        }

        var worldPos = new Vector3(pos.x, pos.y, z);
        MirageDecoySystem.RevealOrSpawnDecoy(mirage.PlayerId, appearanceSource, worldPos, durationSeconds);
    }

    [MethodRpc((uint)ExtensionRpc.MiragePrimeDecoy)]
    public static void RpcMiragePrimeDecoy(
        PlayerControl mirage,
        PlayerControl appearanceSource,
        Vector2 pos,
        float z)
    {
        if (mirage?.Data?.Role is not MirageRole)
        {
            return;
        }

        var worldPos = new Vector3(pos.x, pos.y, z);
        MirageDecoySystem.PrimeDecoy(mirage.PlayerId, appearanceSource, worldPos);
    }

    [MethodRpc((uint)ExtensionRpc.MirageDestroyDecoy)]
    public static void RpcMirageDestroyDecoy(PlayerControl mirage)
    {
        if (mirage?.Data?.Role is not MirageRole)
        {
            return;
        }

        if (mirage.AmOwner)
        {
            TouAudio.PlaySound(TouExtensionAudio.DecoyDestroySound);
        }

        if (MirageDecoySystem.TryRemoveDecoy(mirage.PlayerId, out _) && mirage.AmOwner)
        {
            MirageDecoyButton.LocalInstance?.StartCooldownAndReset();
        }
    }

    [MethodRpc((uint)ExtensionRpc.MirageTriggerDecoy)]
    public static void RpcMirageTriggerDecoy(PlayerControl mirage, PlayerControl interactor, Vector2 pos)
    {
        if (mirage?.Data?.Role is not MirageRole)
        {
            return;
        }

        if (OptionGroupSingleton<MirageOptions>.Instance.RevealInteractorRole && interactor != null)
        {
            if (!TriggeredRoles.TryGetValue(mirage.PlayerId, out var roles))
            {
                roles = [];
                TriggeredRoles[mirage.PlayerId] = roles;
            }

            var role = interactor.Data?.Role;
            string roleName;
            Color roleColor = Color.white;

            if (role != null)
            {
                roleName = role.GetRoleName();
                if (string.IsNullOrWhiteSpace(roleName) || roleName == "Unknown")
                {
                    // Fallback to class name if localization or standard retrieval fails
                    roleName = role.GetType().Name.Replace("Role", "").Replace("RoleBehaviour", "");
                    if (string.IsNullOrWhiteSpace(roleName)) roleName = "Player";
                }
                roleColor = role is ICustomRole customRole ? customRole.RoleColor : role.TeamColor;
            }
            else
            {
                roleName = "Unknown Player";
            }

            var coloredRoleName = $"<color=#{ColorUtility.ToHtmlStringRGBA(roleColor)}>{roleName}</color>";

            if (!roles.Contains(coloredRoleName))
            {
                roles.Add(coloredRoleName);
            }
        }

        if (mirage.AmOwner)
        {
            TouAudio.PlaySound(TouExtensionAudio.DecoyDestroySound);
        }

        MirageDecoySystem.TryRemoveDecoy(mirage.PlayerId, out _);

        if (interactor != null && interactor.AmOwner)
        {
            Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Mirage));
            TouAudio.PlaySound(TouAudio.DiscoveredSound);
            var msg = TouLocale.GetParsed("ExtensionRoleMirageInteractorTriggered", "You interacted with a decoy!");
            var notif = Helpers.CreateAndShowNotification(
                msg,
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TouExtensionIcons.MirageRoleIcon.LoadAsset());
            notif.AdjustNotification();
        }

        if (mirage.AmOwner)
        {
            MirageDecoyButton.LocalInstance?.StartCooldownAndReset();

            Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Mirage));
            TouAudio.PlaySound(TouAudio.DiscoveredSound);

            var msg = TouLocale.Get("ExtensionRoleMirageOwnerTriggered", "Your decoy was triggered!");

            var notif = Helpers.CreateAndShowNotification(
                msg,
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TouExtensionIcons.MirageRoleIcon.LoadAsset());
            notif.AdjustNotification();

            var arrowDur = OptionGroupSingleton<MirageOptions>.Instance.ArrowTime;
            var arrowTarget = OptionGroupSingleton<MirageOptions>.Instance.ArrowTarget;
            if (arrowDur > 0f && mirage.TryGetComponent<ModifierComponent>(out var modComp))
            {
                if (arrowTarget == MirageArrowTarget.Interactor && interactor != null)
                {
                    modComp.AddModifier(new PlayerArrowModifier(interactor, TouExtensionColors.Mirage, arrowDur));
                }
                else
                {
                    modComp.AddModifier(new VentArrowModifier(pos, TouExtensionColors.Mirage, arrowDur));
                }
            }
        }
    }
}