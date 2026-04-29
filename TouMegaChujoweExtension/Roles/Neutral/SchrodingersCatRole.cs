using System.Text;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Networking;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using TownOfUs.Assets;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using UnityEngine;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs.Modifiers;
using MiraAPI.Modifiers;
using Reactor.Utilities;

namespace TouMegaChujoweExtension.Roles.Neutral;

public sealed class SchrodingersCatRole(IntPtr cppPtr) : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    private static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("SchrodingersCatRole");

    public DoomableType DoomHintType => DoomableType.Trickster;
    public string LocaleKey => "SchrodingersCat";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") +
               MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities => [];

    public Color RoleColor => TouExtensionColors.SchrodingersCat;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralBenign;

    /// <summary>
    /// PlayerId of the killer who adopted this cat. byte.MaxValue = no partner yet.
    /// </summary>
    public byte TeammateId { get; set; } = byte.MaxValue;

    /// <summary>
    /// Whether the cat has already been adopted (first kill absorbed).
    /// </summary>
    [HideFromIl2Cpp]
    public bool IsAdopted => TeammateId != byte.MaxValue;

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = false,
        IntroSound = TouAudio.DiscoveredSound,
        Icon = TouExtensionNeuAssets.SchrodingersCatRoleIcon,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>()
    };

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var stringB = ITownOfUsRole.SetNewTabText(this);

        if (IsAdopted)
        {
            var teammate = MiscUtils.PlayerById(TeammateId);
            if (teammate != null)
            {
                var teammateName = teammate.Data.PlayerName;
                var teammateColor = GetTeammateRoleColor();
                stringB.AppendLine($"Partner: <color=#{ColorUtility.ToHtmlStringRGBA(teammateColor)}>{teammateName}</color>");
            }
            else
            {
                stringB.AppendLine("Partner: Unknown");
            }
        }
        else
        {
            stringB.AppendLine("No partner yet. Survive a kill attempt to be adopted!");
        }

        return stringB;
    }

    public bool WinConditionMet()
    {
        return false; // Cat never triggers game over on its own
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        TeammateId = byte.MaxValue;
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
        if (!IsAdopted)
            return false;

        if (Player.HasDied())
            return false;

        var teammate = MiscUtils.PlayerById(TeammateId);
        if (teammate == null || teammate.Data == null)
            return false;

        // Check if teammate's role won
        var teammateRole = teammate.Data.Role;
        if (teammateRole == null)
            return false;

        // If teammate is alive and their DidWin returns true, cat wins too
        if (teammateRole.DidWin(gameOverReason))
            return true;

        // Also check by game over reason alignment
        // Impostor teammate + impostor win
        if (teammateRole.IsImpostor && IsImpostorWin(gameOverReason))
            return true;

        // Crewmate teammate + crew win
        if (!teammateRole.IsImpostor && teammateRole is not ICustomRole && IsCrewmateWin(gameOverReason))
            return true;

        return false;
    }

    private static bool IsImpostorWin(GameOverReason reason)
    {
        return reason is GameOverReason.ImpostorsByKill or GameOverReason.ImpostorsByVote or GameOverReason.ImpostorsBySabotage or GameOverReason.ImpostorDisconnect;
    }

    private static bool IsCrewmateWin(GameOverReason reason)
    {
        return reason is GameOverReason.CrewmatesByVote or GameOverReason.CrewmatesByTask;
    }

    [HideFromIl2Cpp]
    private Color GetTeammateRoleColor()
    {
        var teammate = MiscUtils.PlayerById(TeammateId);
        if (teammate?.Data?.Role == null)
            return Color.white;

        if (teammate.Data.Role is ITownOfUsRole touRole)
            return touRole.RoleColor;

        if (teammate.Data.Role.IsImpostor)
            return Palette.ImpostorRed;

        return Palette.CrewmateBlue;
    }

    /// <summary>
    /// Called via RPC when a killer tries to kill this cat for the first time.
    /// </summary>
    [MethodRpc((uint)Networking.ExtensionRpc.CatSetTeammate)]
    public static void RpcSetTeammate(PlayerControl cat, byte killerId)
    {
        if (cat.Data?.Role is not SchrodingersCatRole catRole)
            return;

        if (catRole.IsAdopted) return;

        catRole.TeammateId = killerId;
        Log.LogInfo($"SchrodingersCat {cat.Data.PlayerName} adopted by killer {killerId}");

        var killer = MiscUtils.PlayerById(killerId);

        // Role reveal logic
        if (killer != null && OptionGroupSingleton<SchrodingersCatOptions>.Instance.RevealRolesToEachOther)
        {
            if (!cat.HasModifier<CatRevealModifier>())
                cat.AddModifier<CatRevealModifier>(cat.Data.Role);

            if (!killer.HasModifier<PartnerRevealModifier>())
                killer.AddModifier<PartnerRevealModifier>(killer.Data.Role);
        }

        // Flash for cat
        if (cat.AmOwner)
        {
            Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.SchrodingersCat));
            ShowCatNotification(killer != null
                ? $"You were adopted by {killer.Data.PlayerName}!"
                : "You were adopted!");
        }

        // Flash for killer
        if (killer != null && killer.AmOwner)
        {
            Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.SchrodingersCat));
            ShowCatNotification($"You adopted {cat.Data.PlayerName}!");
        }
    }

    [MethodRpc((uint)Networking.ExtensionRpc.CatChangeRoleOnOwnerDeath)]
    public static void RpcChangeRoleOnOwnerDeath(PlayerControl cat, ushort newRoleId)
    {
        if (cat == null || cat.HasDied()) return;
        cat.ChangeRole(newRoleId);
    }

    [MethodRpc((uint)Networking.ExtensionRpc.SendCatChat)]
    public static void RpcSendCatChat(PlayerControl sender, string text)
    {
        if (sender == null) return;
        if (!MiraAPI.GameOptions.OptionGroupSingleton<TouMegaChujoweExtension.Options.GeneralOptions>.Instance.CatChat) return;

        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null) return;

        // Find the relationship
        PlayerControl? involvedCat = null;
        PlayerControl? involvedPartner = null;

        if (sender.IsRole<SchrodingersCatRole>())
        {
            involvedCat = sender;
            involvedPartner = MiscUtils.PlayerById(involvedCat.GetRole<SchrodingersCatRole>().TeammateId);
        }
        else
        {
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p != null && p.IsRole<SchrodingersCatRole>() && p.GetRole<SchrodingersCatRole>().TeammateId == sender.PlayerId)
                {
                    involvedCat = p;
                    involvedPartner = sender;
                    break;
                }
            }
        }

        if (involvedCat == null || involvedPartner == null) return;

        // Determine recipients
        var recipients = new List<byte> { involvedCat.PlayerId, involvedPartner.PlayerId };

        if (involvedPartner.Data.Role.IsImpostor)
        {
            foreach (var p in PlayerControl.AllPlayerControls)
                if (p != null && p.Data != null && p.Data.Role.IsImpostor)
                    if (!recipients.Contains(p.PlayerId)) recipients.Add(p.PlayerId);
        }
        else if (involvedPartner.IsRole<VampireRole>())
        {
            foreach (var p in PlayerControl.AllPlayerControls)
                if (p != null && p.IsRole<VampireRole>())
                    if (!recipients.Contains(p.PlayerId)) recipients.Add(p.PlayerId);
        }
        else if (involvedPartner.HasModifier<TownOfUs.Modifiers.Game.Alliance.LoverModifier>())
        {
            foreach (var p in PlayerControl.AllPlayerControls)
                if (p != null && p.HasModifier<TownOfUs.Modifiers.Game.Alliance.LoverModifier>())
                    if (!recipients.Contains(p.PlayerId)) recipients.Add(p.PlayerId);
        }

        bool canSee = recipients.Contains(localPlayer.PlayerId);
        if (!canSee && TownOfUs.Modifiers.DeathHandlerModifier.IsFullyDead(localPlayer) && OptionGroupSingleton<TownOfUs.Options.GeneralOptions>.Instance.TheDeadKnow)
        {
            canSee = true;
        }

        if (canSee)
        {
            string titlePrefix = "(Cat Chat)";
            if (involvedPartner.Data.Role.IsImpostor) titlePrefix = "(Impostor Chat)";
            else if (involvedPartner.IsRole<VampireRole>()) titlePrefix = "(Vampire Chat)";
            else if (involvedPartner.HasModifier<TownOfUs.Modifiers.Game.Alliance.LoverModifier>()) titlePrefix = "(Lover Chat)";
            
            string title = $"{titlePrefix} {sender.Data.PlayerName}";
            MiscUtils.AddTeamChat(sender.Data, title, text, bubbleType: BubbleType.Other, onLeft: !sender.AmOwner);
            
            if (MeetingHud.Instance != null && !sender.AmOwner)
            {
                TownOfUs.Patches.Options.TeamChatPatches.TeamChatManager.MarkChatAsUnread(65);
            }
        }
    }

    private static void ShowCatNotification(string text)
    {
        try
        {
            var notif = Helpers.CreateAndShowNotification(
                text,
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TouExtensionNeuAssets.SchrodingersCatRoleIcon.LoadAsset());
            notif?.AdjustNotification();
        }
        catch
        {
            if (HudManager.Instance != null)
                HudManager.Instance.Notifier.AddDisconnectMessage(text);
        }
    }
}
