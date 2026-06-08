using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities.Assets;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using Reactor.Utilities;
using System.Collections;
using System.Linq;
using System.Text;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Crewmate;

/// <summary>
/// Trapper role: Places traps on vents that immobilize players who use them.
/// </summary>
public sealed class TrapperRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public override bool IsAffectedByComms => false;

    public DoomableType DoomHintType => DoomableType.Trickster;
    public string LocaleKey => "Trapper";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

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
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}Trap", "Trap"),
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}TrapWikiDescription"),
                    TouCrewAssets.TrapSprite)
            ];
        }
    }

    public Color RoleColor => TouExtensionColors.Trapper;
    public ModdedRoleTeams Team => ModdedRoleTeams.Crewmate;
    public RoleAlignment RoleAlignment => RoleAlignment.CrewmateSupport;

    public CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TouExtensionIcons.TrapperRoleIcon,
        IntroSound = TouAudio.EngineerIntroSound,
        OptionsScreenshot = TouBanners.CrewmateRoleBanner,
    };

    public void LobbyStart()
    {
        VentTrapSystem.ClearAll();
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);

        VentTrapSystem.ClearOwnedBy(targetPlayer.PlayerId);
    }

    [MethodRpc((uint)ExtensionRpc.TrapperPlaceTrap)]
    public static void RpcTrapperPlaceTrap(PlayerControl trapper, int ventId)
    {
        if (trapper == null || trapper.Data?.Role is not TrapperRole)
        {
            return;
        }

        VentTrapSystem.Place(ventId, trapper.PlayerId);

        if (trapper.AmOwner)
        {
            var vent = Helpers.GetVentById(ventId);
            var room = vent != null ? MiscUtils.GetRoomName(vent.transform.position) : TouLocale.Get("Unknown", "Unknown");
            var msg = TouLocale.GetParsed("ExtensionRoleTrapperPlaced", "Trapped a vent in <room>!", new()
            {
                ["<room>"] = room
            });

            var notif = Helpers.CreateAndShowNotification(
                msg,
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TouRoleIcons.Trapper.LoadAsset());
            notif.AdjustNotification();
        }
    }

    [HideFromIl2Cpp]
    [MethodRpc((uint)ExtensionRpc.TrapperTriggerTrap)]
    public static void RpcTrapperTriggerTrap(PlayerControl trapper, int ventId, byte victimId)
    {
        if (trapper == null)
        {
            return;
        }

        VentTrapSystem.Remove(ventId);

        var victim = MiscUtils.PlayerById(victimId);
        if (victim == null)
        {
            return;
        }

        if (!VentTrapSystem.IsEligibleToBeTrapped(victim))
        {
            return;
        }

        var vent = Helpers.GetVentById(ventId);
        var ventTopPos = vent != null ? VentTrapSystem.GetVentTopPosition(vent) : (Vector2)victim.transform.position;

        Coroutines.Start(CoTrapperTriggerTrap(trapper, victim, ventId, ventTopPos, vent));
    }

    private static IEnumerator CoTrapperTriggerTrap(PlayerControl trapper, PlayerControl victim, int ventId, Vector2 ventTopPos, Vent? vent)
    {
        yield return new WaitForSeconds(0.3f);

        if (victim.AmOwner)
        {
            CoApplyTrapToVictimAfterVentAnim(victim, ventId, ventTopPos, vent);
        }
        else if (trapper.AmOwner)
        {
            Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Trapper));

            var arrowDur = OptionGroupSingleton<TrapperOptions>.Instance.ArrowDuration;
            var arrowTarget = OptionGroupSingleton<TrapperOptions>.Instance.ArrowTarget;
            if (trapper.TryGetComponent<ModifierComponent>(out var modifierComp) && arrowDur > 0f)
            {
                if (arrowTarget == TrapperArrowTarget.Person && victim != null)
                {
                    modifierComp.AddModifier(new PlayerArrowModifier(victim, TouExtensionColors.Trapper, arrowDur));
                }
                else
                {
                    modifierComp.AddModifier(new VentArrowModifier(ventTopPos, TouExtensionColors.Trapper, arrowDur));
                }
            }

            var room = vent != null ? MiscUtils.GetRoomName(vent.transform.position) : TouLocale.Get("Unknown", "Unknown");
            var msg = TouLocale.GetParsed("ExtensionRoleTrapperTriggered", "Your trap was triggered in <room>!", new()
            {
                ["<room>"] = room
            });

            var notif = Helpers.CreateAndShowNotification(
                msg,
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TouRoleIcons.Trapper.LoadAsset());
            notif.AdjustNotification();
        }
    }

    private static void CoApplyTrapToVictimAfterVentAnim(PlayerControl victim, int ventId, Vector2 ventTopPos, Vent? vent)
    {
        if (victim == null || victim.HasDied() || !victim.AmOwner)
        {
            return;
        }

        var dur = OptionGroupSingleton<TrapperOptions>.Instance.Trappeduration;
        if (victim.TryGetComponent<ModifierComponent>(out var modifierComp))
        {
            modifierComp.AddModifier(new TrappedOnVentModifier(ventTopPos, dur, ventId));
        }

        Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Trapper));
        TouAudio.PlaySound(TouAudio.DiscoveredSound);

        var room = vent != null ? MiscUtils.GetRoomName(vent.transform.position) : TouLocale.Get("Unknown", "Unknown");
        var msg = TouLocale.GetParsed("ExtensionRoleTrapperCaught", "You were caught in a trap in <room>!", new()
        {
            ["<room>"] = room
        });

        var notif = Helpers.CreateAndShowNotification(
            msg,
            Color.white,
            new Vector3(0f, 1f, -20f),
            spr: TouRoleIcons.Trapper.LoadAsset());
        notif.AdjustNotification();
    }

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var stringB = ITownOfUsRole.SetNewTabText(this);
        var myTraps = VentTrapSystem.GetTrapEntriesOwnedBy(Player.PlayerId).ToList();

        if (myTraps.Count != 0)
        {
            stringB.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"\n<b>{TouLocale.GetParsed("ExtensionRoleTrapperTabHeader", "Active Traps:")}</b>");
            var roundsLast = (int)OptionGroupSingleton<TrapperOptions>.Instance.TrapRoundsLast;
            foreach (var trap in myTraps)
            {
                var vent = Helpers.GetVentById(trap.Key);
                var room = vent != null ? MiscUtils.GetRoomName(vent.transform.position) : TouLocale.Get("Unknown", "Unknown");
                var ventLabel = TouLocale.GetParsed("ExtensionRoleTrapperVentLabelTabText", "<room> Vent")
                    .Replace("<room>", room);
                var roundsText = roundsLast <= 0
                    ? string.Empty
                    : $": {TouLocale.GetParsed("ExtensionRoleTrapperVentRoundsTabText", "<rounds> Round(s) Remaining").Replace("<rounds>", trap.Value.ToString(System.Globalization.CultureInfo.InvariantCulture))}";
                stringB.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"<b><size=70%>{ventLabel}{roundsText}</size></b>");
            }
        }

        return stringB;
    }
}




















