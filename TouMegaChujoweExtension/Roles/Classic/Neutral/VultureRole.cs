using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Object = UnityEngine.Object;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using Reactor.Utilities;
using System.Collections;
using TownOfUs.Assets;
using TownOfUs.Events.Crewmate;
using TownOfUs.Extensions;
using TownOfUs.Modules.Components;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.TimeLord;
using TownOfUs.Modules.Wiki;
using TownOfUs.Modules;
using TownOfUs.Options.Roles.Crewmate;
using TownOfUs.Options.Roles.Neutral;
using TownOfUs.Options;
using TownOfUs.Patches.Options;
using TownOfUs.Roles.Impostor;
using TownOfUs.Roles.Neutral;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TownOfUs.Buttons;
using TownOfUs;
using TouMegaChujoweExtension.Buttons.Classic.Neutral;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Neutral;

public sealed class VultureRole(IntPtr cppPtr) : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable, ISpawnChange
{
    public DoomableType DoomHintType => DoomableType.Fearmonger;
    public string LocaleKey => "Vulture";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Vulture;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralEvil;

    public bool NoSpawn => MiscUtils.SpawnableRoles.Any(x => x is JanitorRole);

    [HideFromIl2Cpp]
    public int BodiesEaten { get; set; }

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = OptionGroupSingleton<VultureOptions>.Instance.CanVent,
        Icon = TouExtensionIcons.VultureRoleIcon,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>(),
        IntroSound = TouAudio.ChefSound,
        OptionsScreenshot = TouExtensionBanners.VultureBanner,
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
                    TouExtensionNeuAssets.VultureEatButtonSprite)
            };

            var options = OptionGroupSingleton<VultureOptions>.Instance;
            if (options.ScavengeEnabled && options.ScavengeDuration.Value > 0f)
            {
                abilities.Add(new(
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}Scavenge", "Scavenge"),
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}ScavengeWikiDescription"),
                    TouExtensionNeuAssets.VultureScavengeButtonSprite));
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

        var options = OptionGroupSingleton<VultureOptions>.Instance;
        return BodiesEaten >= (int)options.BodiesToWin;
    }

    public bool IsWinConditionImpossible()
    {
        if (Player.HasDied())
        {
            return true;
        }

        var options = OptionGroupSingleton<VultureOptions>.Instance;
        var bodiesNeeded = (int)options.BodiesToWin;
        var bodiesEaten = BodiesEaten;
        var bodiesRemaining = bodiesNeeded - bodiesEaten;

        var allBodies = Object.FindObjectsOfType<DeadBody>();
        var availableBodies = allBodies.Count(b => !VultureSystem.IsBodyEaten(b.ParentId));

        var alivePlayers = Helpers.GetAlivePlayers();
        var potentialBodies = alivePlayers.Count - 1;

        return availableBodies + potentialBodies < bodiesRemaining;
    }

    public void OffsetButtons()
    {
        var canVent = OptionGroupSingleton<VultureOptions>.Instance.CanVent || LocalSettingsTabSingleton<TownOfUsLocalSettings>.Instance.OffsetButtonsToggle.Value;
        var scavenge = MiraAPI.Hud.CustomButtonSingleton<VultureScavengeButton>.Instance;
        var eat = MiraAPI.Hud.CustomButtonSingleton<VultureEatButton>.Instance;
        
        Reactor.Utilities.Coroutines.Start(MiscUtils.CoMoveButtonIndex(scavenge, !canVent));
        Reactor.Utilities.Coroutines.Start(MiscUtils.CoMoveButtonIndex(eat, !canVent));
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        BodiesEaten = 0;
        if (player.AmOwner)
        {
            OffsetButtons();
            HudManager.Instance.ImpostorVentButton.graphic.sprite = TouAssets.VentSprite.LoadAsset();
            HudManager.Instance.ImpostorVentButton.buttonLabelText.SetOutlineColor(RoleColor);
        }
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        VultureSystem.ClearForPlayer(targetPlayer.PlayerId);

        if (targetPlayer.AmOwner)
        {
            HudManager.Instance.ImpostorVentButton.graphic.sprite = TouAssets.VentSprite.LoadAsset();
            HudManager.Instance.ImpostorVentButton.buttonLabelText.SetOutlineColor(TownOfUsColors.Impostor);
        }
    }

    public override bool CanUse(IUsable usable)
    {
        return GameManager.Instance.LogicUsables.CanUse(usable, Player);
    }

    public override bool DidWin(GameOverReason gameOverReason)
    {
        return WinConditionMet();
    }

    [MethodRpc((uint)ExtensionRpc.VultureEat, LocalHandling = RpcLocalHandling.Before)]
    public static void RpcVultureEat(PlayerControl sender, byte bodyId)
    {
        if (sender == null) return;

        // Find body
        var body = Object.FindObjectsOfType<DeadBody>().FirstOrDefault(x => x.ParentId == bodyId);
        if (body == null)
        {
            // Try fallback
            body = TimeLordBodyManager.FindDeadBodyIncludingInactive(bodyId);
        }

        if (body == null) return;

        // Update count for Vulture
        if (sender.Data?.Role is VultureRole role)
        {
            role.BodiesEaten++;
        }
        VultureSystem.MarkBodyEaten(bodyId);

        if (sender.AmOwner)
        {
            TouAudio.PlaySound(TouExtensionAudio.VultureEatSound);
        }

        var cleanDuration = 1.75f;

        Coroutines.Start(CoSafeClean(body, cleanDuration, sender));

        // Win condition check (only for host or owner)
        if (sender.AmOwner || (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost))
        {
            CheckWinConditionFor(sender);
        }
    }

    [HideFromIl2Cpp]
    private static IEnumerator CoSafeClean(DeadBody body, float duration, PlayerControl scavenger)
    {
        if (body == null || body.gameObject == null) yield break;

        var bodyId = body.ParentId;
        var bodyPlayer = MiscUtils.PlayerById(bodyId);
        if (bodyPlayer != null)
        {
            MiscUtils.RemovePet(bodyPlayer);
        }

        // Fade out
        if (duration > 0f && body.bodyRenderers != null && body.bodyRenderers.Length > 0)
        {
            var renderer = body.bodyRenderers[^1];
            if (renderer != null)
            {
                yield return MiscUtils.PerformTimedAction(duration, t => {
                    if (renderer != null) renderer.color = renderer.color.SetAlpha(1 - t);
                });
            }
        }

        // Definitive removal
        if (body == null || body.gameObject == null) yield break;
        body.gameObject.SetActive(false);

        var optionEnabled = OptionGroupSingleton<TimeLordOptions>.Instance.UncleanBodiesOnRewind;
        if (optionEnabled)
        {
            if (bodyPlayer != null)
            {
                TimeLordEventHandlers.RecordBodyCleaned(scavenger, body, body.transform.position, 
                    TimeLordBodyManager.CleanedBodySource.Janitor);
            }
            var destroyBody = (BodyVitalsMode)OptionGroupSingleton<GameMechanicOptions>.Instance.CleanedBodiesAppearance.Value;
            Coroutines.Start(TimeLordBodyManager.CoHideBodyForTimeLord(body, destroyBody)); 
        }
        else
        {
            Object.Destroy(body.gameObject);
        }
        Coroutines.Start(CrimeSceneComponent.CoClean(body));
    }

    private static void CheckWinConditionFor(PlayerControl Vulture)
    {
        if (Vulture.Data?.Role is not VultureRole role) return;

        if (role.IsWinConditionImpossible() && !Vulture.HasDied())
        {
            var options = OptionGroupSingleton<VultureOptions>.Instance;
            var roleType = ((BecomeOptions)options.OnLoseBecomes.Value) switch
            {
                BecomeOptions.Crew => (ushort)RoleTypes.Crewmate,
                BecomeOptions.Jester => RoleId.Get<JesterRole>(),
                BecomeOptions.Survivor => RoleId.Get<SurvivorRole>(),
                BecomeOptions.Amnesiac => RoleId.Get<AmnesiacRole>(),
                BecomeOptions.Mercenary => RoleId.Get<MercenaryRole>(),
                _ => (ushort)RoleTypes.Crewmate
            };

            Vulture.RpcChangeRole(roleType, true);
        }
    }

    [MethodRpc((uint)ExtensionRpc.VultureScavenge)]
    public static void RpcVultureScavenge(PlayerControl Vulture)
    {
        if (Vulture?.Data?.Role is not VultureRole)
        {
            return;
        }


        var options = OptionGroupSingleton<VultureOptions>.Instance;
        VultureSystem.StartScavenge(Vulture.PlayerId, options.ScavengeDuration.Value);
    }
}















