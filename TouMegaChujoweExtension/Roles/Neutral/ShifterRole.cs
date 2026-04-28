using System.Text;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Networking;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Utilities;
using TownOfUs;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Game;
using TownOfUs.Modifiers.Game.Alliance;
using TownOfUs.Modifiers.Game.Neutral;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Neutral;

    public sealed class ShifterRole(IntPtr cppPtr) : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable {
    public DoomableType DoomHintType => DoomableType.Trickster;
    public string LocaleKey => "Shifter";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");
    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") +
               MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(TouLocale.GetParsed("ExtensionRoleShifterShift", "Shift"),
            TouLocale.GetParsed("ExtensionRoleShifterShiftWikiDescription",
                "Select a player to steal their role."),
            TouNeutAssets.ShiftSprite)
    ];

    public Color RoleColor => TouExtensionColors.Shifter;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralBenign;

    public byte PendingTargetId { get; set; } = byte.MaxValue;
    public ushort PendingStolenRoleId { get; set; } = ushort.MaxValue;
    public bool ShiftUsed { get; set; }

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = false,
        IntroSound = TouAudio.DiscoveredSound,
        Icon = TouRoleIcons.Shifter,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>()
    };

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var stringB = ITownOfUsRole.SetNewTabText(this);
        if (PendingTargetId != byte.MaxValue)
        {
            var target = MiscUtils.PlayerById(PendingTargetId);
            if (target != null)
            {
                stringB.AppendLine($"Pending Target: {target.Data.PlayerName}");
            }
        }
        else
        {
            stringB.AppendLine("No target selected.");
        }

        return stringB;
    }

    public bool WinConditionMet()
    {
        return false;
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        PendingTargetId = byte.MaxValue;
        PendingStolenRoleId = ushort.MaxValue;
        ShiftUsed = false;
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    public override bool CanUse(IUsable usable)
    {
        return GameManager.Instance.LogicUsables.CanUse(usable, Player);
    }

    public override bool DidWin(GameOverReason gameOverReason)
    {
        if (OptionGroupSingleton<ShifterOptions>.Instance.WinsWithCrew)
        {
            return gameOverReason is GameOverReason.CrewmatesByVote or GameOverReason.CrewmatesByTask;
        }
        return false;
    }

    public override void OnMeetingStart()
    {
        RoleBehaviourStubs.OnMeetingStart(this);

        if (!AmongUsClient.Instance.AmHost)
            return;

        if (ShiftUsed || Player.HasDied() || PendingTargetId == byte.MaxValue)
            return;

        var target = MiscUtils.PlayerById(PendingTargetId);

        if (target == null || target.HasDied())
        {
            RpcCancelShift(Player);
            return;
        }

        var shieldType = target.GetShieldType();
        if (shieldType == ShieldType.Warden || shieldType == ShieldType.FirstDead || shieldType == ShieldType.Cleric)
        {
            RpcCancelShift(Player);
            return;
        }

        ushort stolenRoleId = PendingStolenRoleId;
        if (stolenRoleId == ushort.MaxValue)
        {
            RpcCancelShift(Player);
            return;
        }

        var options = OptionGroupSingleton<ShifterOptions>.Instance;
        ushort becomeRoleId = options.ShiftedBecomes switch
        {
            ShiftedBecomesOption.Amnesiac => RoleId.Get<AmnesiacRole>(),
            ShiftedBecomesOption.Jester => RoleId.Get<JesterRole>(),
            ShiftedBecomesOption.Survivor => RoleId.Get<SurvivorRole>(),
            ShiftedBecomesOption.Mercenary => RoleId.Get<MercenaryRole>(),
            ShiftedBecomesOption.Shifter => RoleId.Get<ShifterRole>(),
            ShiftedBecomesOption.Crewmate => (ushort)RoleTypes.Crewmate,
            _ => (ushort)RoleTypes.Crewmate
        };
        RpcExecuteShift(Player, target, becomeRoleId, stolenRoleId);
    }

    [HideFromIl2Cpp]
    public static bool IsValidShiftTarget(PlayerControl target)
    {
        if (target == null)
            return false;

        if (target.IsImpostorAligned())
            return false;

        if (target.Is(RoleAlignment.NeutralKilling) || target.Is(RoleAlignment.NeutralEvil))
            return false;

        if (target.HasModifier<EgotistModifier>() || target.HasModifier<CrewpostorModifier>())
            return false;

        if (target.Is(RoleAlignment.NeutralBenign))
            return OptionGroupSingleton<ShifterOptions>.Instance.CanShiftNeutralBenign;

        return true;
    }

    [MethodRpc((uint)Networking.ExtensionRpc.ShifterSetTarget)]
    public static void RpcSetShiftTarget(PlayerControl shifter, byte targetId)
    {
        if (shifter.Data?.Role is not ShifterRole shifterRole)
            return;

        shifterRole.PendingTargetId = targetId;

        if (AmongUsClient.Instance.AmHost)
        {
            var target = MiscUtils.PlayerById(targetId);
            if (target?.Data?.Role != null)
            {
                if (target.Data.Role is ICustomRole customRole)
                    shifterRole.PendingStolenRoleId = (ushort)RoleId.Get(customRole.GetType());
                else
                    shifterRole.PendingStolenRoleId = (ushort)target.Data.Role.Role;
            }
        }
    }

    [MethodRpc((uint)Networking.ExtensionRpc.ShifterExecuteShift)]
    public static void RpcExecuteShift(PlayerControl shifter, PlayerControl target, ushort becomeRoleId, ushort stolenRoleId)
    {
        var wasShifterRole = shifter.Data?.Role as ShifterRole;
        if (wasShifterRole == null)
        {
            return;
        }
        var options = OptionGroupSingleton<ShifterOptions>.Instance;

        // Get all modifiers for target
        var targetModifiers = target.GetModifiers<BaseModifier>();

        if (options.StealModifiers)
        {
            foreach (var modifier in targetModifiers)
            {
                if (modifier is not DeathHandlerModifier && modifier is not LoverModifier)
                {
                    shifter.AddModifier(modifier.GetType());
                    target.RemoveModifier(modifier.GetType());
                }
            }
        }

        target.ChangeRole(becomeRoleId);
        
        // BUG FIX: Remove ALL Assassin-related modifiers from the target
        // We iterate and check for the base type or name to be safe
        foreach (var modifier in targetModifiers)
        {
            if (modifier is AssassinModifier || modifier.GetType().Name.Contains("Assassin"))
            {
                target.RemoveModifier(modifier.GetType());
            }
        }

        if (target.Data.Role is not ICustomRole && !target.HasModifier<DeathHandlerModifier>())
            target.AddModifier<DeathHandlerModifier>();
            
        shifter.ChangeRole(stolenRoleId);
        
        if (shifter.Data.Role is not ICustomRole && !shifter.HasModifier<DeathHandlerModifier>())
            shifter.AddModifier<DeathHandlerModifier>();
            
        wasShifterRole.ShiftUsed = true;
        wasShifterRole.PendingTargetId = byte.MaxValue;
        wasShifterRole.PendingStolenRoleId = ushort.MaxValue;

        // Notifications
        if (target.AmOwner)
        {
            PirateDuelSystem.FlashScreen(TouExtensionColors.Shifter, 0.5f, 0.3f);
            ShowShifterNotification("Your role has been stolen!");
        }

        if (shifter.AmOwner)
        {
            PirateDuelSystem.FlashScreen(TouExtensionColors.Shifter, 0.5f, 0.3f);
            ShowShifterNotification($"You stole {target.Data.PlayerName}'s role!");
        }
    }

    [MethodRpc((uint)Networking.ExtensionRpc.ShifterCancelShift)]
    public static void RpcCancelShift(PlayerControl shifter)
    {
        if (shifter.Data?.Role is not ShifterRole shifterRole)
            return;
        shifterRole.PendingTargetId = byte.MaxValue;
        shifterRole.PendingStolenRoleId = ushort.MaxValue;
        if (shifter.AmOwner)
            ShowShifterNotification("Your target died. Shift cancelled.");
    }

    [MethodRpc((uint)Networking.ExtensionRpc.ShifterDie)]
    public static void RpcShifterDie(PlayerControl shifter)
    {
        if (shifter.Data?.Role is not ShifterRole)
            return;
        if (AmongUsClient.Instance.AmHost)
            shifter.RpcCustomMurder(shifter);
        if (shifter.AmOwner)
            ShowShifterNotification("You tried to shift an invalid target and died!");
    }
    private static void ShowShifterNotification(string text)
    {
        try
        {
            var notif = Helpers.CreateAndShowNotification(
                text,
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TouRoleIcons.Shifter.LoadAsset());
            notif?.AdjustNotification();
        }
        catch
        {
            if (HudManager.Instance != null)
                HudManager.Instance.Notifier.AddDisconnectMessage(text);
        }
    }
}