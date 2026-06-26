using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using TouMiraRolesExtension.Assets;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Networking;
using TownOfUs.Assets;
using TouMiraRolesExtension.Options.Roles.Neutral;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Options.Roles.Neutral;
using Reactor.Utilities;
using TownOfUs.Modules;
using TownOfUs.Modules.Components;
using TownOfUs.Modules.TimeLord;
using TownOfUs.Options;
using TownOfUs.Options.Roles.Crewmate;
using TownOfUs.Roles;
using TownOfUs.Roles.Impostor;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMiraRolesExtension.Roles.Neutral;

public sealed class ScavengerRole(IntPtr cppPtr) : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable, ISpawnChange
{
    public DoomableType DoomHintType => DoomableType.Fearmonger;
    public string LocaleKey => "Scavenger";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Scavenger;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralEvil;

    public bool NoSpawn => MiscUtils.SpawnableRoles.Any(x => x is JanitorRole);

    [HideFromIl2Cpp]
    public int BodiesEaten { get; set; }

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = OptionGroupSingleton<ScavengerOptions>.Instance.CanVent,
        Icon = TouExtensionAssets.ScavengerRoleIcon,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>()
    };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities
    {
        get
        {
            var abilities = new List<CustomButtonWikiDescription>
            {
                new(
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}Eat", "Eat"),
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}EatWikiDescription"),
                    TouExtensionAssets.ScavengerEatButtonSprite)
            };

            var options = OptionGroupSingleton<ScavengerOptions>.Instance;
            if (options.ScavengeEnabled && options.ScavengeDuration.Value > 0f)
            {
                abilities.Add(new(
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}Scavenge", "Scavenge"),
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}ScavengeWikiDescription"),
                    TouExtensionAssets.ScavengerScavengeButtonSprite));
            }

            return abilities;
        }
    }

    public bool WinConditionMet()
    {
        if (Player.HasDied())
        {
            return false;
        }

        var options = OptionGroupSingleton<ScavengerOptions>.Instance;
        return BodiesEaten >= (int)options.BodiesToWin;
    }

    public bool IsWinConditionImpossible()
    {
        if (Player.HasDied())
        {
            return true;
        }

        var options = OptionGroupSingleton<ScavengerOptions>.Instance;
        var bodiesNeeded = (int)options.BodiesToWin;
        var bodiesEaten = BodiesEaten;
        var bodiesRemaining = bodiesNeeded - bodiesEaten;

        var allBodies = Object.FindObjectsOfType<DeadBody>();
        var availableBodies = allBodies.Count(b => !ScavengerSystem.IsBodyEaten(b.ParentId));

        var alivePlayers = Helpers.GetAlivePlayers();
        var potentialBodies = alivePlayers.Count - 1;

        return availableBodies + potentialBodies < bodiesRemaining;
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        BodiesEaten = 0;
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        ScavengerSystem.ClearForPlayer(targetPlayer.PlayerId);
    }

    public override bool CanUse(IUsable usable)
    {
        return GameManager.Instance.LogicUsables.CanUse(usable, Player);
    }

    public override bool DidWin(GameOverReason gameOverReason)
    {
        return WinConditionMet();
    }

    [MethodRpc((uint)ExtensionRpc.ScavengerEat)]
    public static void RpcScavengerEat(PlayerControl scavenger, byte bodyId)
    {
        if (scavenger?.Data?.Role is not ScavengerRole role)
        {
            return;
        }

        var body = TimeLordBodyManager.FindDeadBodyIncludingInactive(bodyId);
        if (body == null)
        {
            body = Object.FindObjectsOfType<DeadBody>().FirstOrDefault(x => x.ParentId == bodyId);
        }

        if (body == null)
        {
            return;
        }

        role.BodiesEaten++;
        ScavengerSystem.MarkBodyEaten(bodyId);

        if (scavenger.AmOwner)
        {
            TouAudio.PlaySound(TouExtensionAudio.ScavengerEatSound);
        }

        var isHost = AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;
        var optionEnabled = OptionGroupSingleton<TimeLordOptions>.Instance.UncleanBodiesOnRewind;
        var shouldRecord = isHost ? optionEnabled : (optionEnabled || TimeLordRewindSystem.MatchHasTimeLord());

        var destroyBody = (BodyVitalsMode)OptionGroupSingleton<GameMechanicOptions>.Instance.CleanedBodiesAppearance.Value;

        if (shouldRecord)
        {
            var bodyPlayer = MiscUtils.PlayerById(bodyId);
            if (bodyPlayer != null)
            {
                TownOfUs.Events.Crewmate.TimeLordEventHandlers.RecordBodyCleaned(scavenger, body, body.transform.position, 
                    TimeLordBodyManager.CleanedBodySource.Janitor);
            }
            Coroutines.Start(TimeLordBodyManager.CoHideBodyForTimeLord(body, destroyBody));
        }
        else if (isHost)
        {
            Coroutines.Start(body.CoCleanCustom(destroyBody));
        }
        Coroutines.Start(CrimeSceneComponent.CoClean(body));

        if (role.IsWinConditionImpossible() && !scavenger.HasDied())
        {
            var options = OptionGroupSingleton<ScavengerOptions>.Instance;
            var roleType = ((BecomeOptions)options.OnLoseBecomes.Value) switch
            {
                BecomeOptions.Crew => (ushort)RoleTypes.Crewmate,
                BecomeOptions.Jester => RoleId.Get<JesterRole>(),
                BecomeOptions.Survivor => RoleId.Get<SurvivorRole>(),
                BecomeOptions.Amnesiac => RoleId.Get<AmnesiacRole>(),
                BecomeOptions.Mercenary => RoleId.Get<MercenaryRole>(),
                _ => (ushort)RoleTypes.Crewmate
            };

            scavenger.ChangeRole(roleType);
        }
    }

    [MethodRpc((uint)ExtensionRpc.ScavengerScavenge)]
    public static void RpcScavengerScavenge(PlayerControl scavenger)
    {
        if (scavenger?.Data?.Role is not ScavengerRole)
        {
            return;
        }

        var options = OptionGroupSingleton<ScavengerOptions>.Instance;
        ScavengerSystem.StartScavenge(scavenger.PlayerId, options.ScavengeDuration.Value);
    }
}