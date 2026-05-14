using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using InnerNet;
using MiraAPI.GameEnd;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers.Types;
using MiraAPI.Modifiers;
using MiraAPI.Networking;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Random = System.Random;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using Reactor.Utilities.Extensions;
using Reactor.Utilities;
using System.Collections;
using System.Text.RegularExpressions;
using TownOfUs.Assets;
using TownOfUs.Events;
using TownOfUs.Extensions;
using TownOfUs.Modifiers.Game.Alliance;
using TownOfUs.Modifiers.Game;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Modules;
using TownOfUs.Options.Roles.Neutral;
using TownOfUs.Options;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles.Neutral;
using TownOfUs.Roles.Other;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TownOfUs;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Neutral;

public sealed class LawyerRole(IntPtr cppPtr) : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable,
    IAssignableTargets, ICrewVariant, IDisposable
{
    private const float NoObjectLastSeconds = 20f;
    private static readonly Regex SecondsRegex = new(@"(\d+)\s*s", RegexOptions.IgnoreCase);
    private static readonly Regex LastNumberRegex = new(@"(\d+)(?!.*\d)");

    [HideFromIl2Cpp]
    public PlayerControl? Client { get; set; }
    public bool ClientVoted { get; set; }
    public bool AboutToWin { get; set; }

    [HideFromIl2Cpp] 
    public List<byte> Voters { get; set; } = [];
    [HideFromIl2Cpp] 
    public int ObjectionsUsed { get; set; }
    [HideFromIl2Cpp] 
    public int ObjectionsUsedThisMeeting { get; set; }
    [HideFromIl2Cpp] 
    public bool HasObjected { get; set; }
    [HideFromIl2Cpp] 
    public List<byte> ObjectedVoters { get; set; } = [];
    [HideFromIl2Cpp] 
    public Dictionary<byte, byte> ObjectedVoterOriginalVotes { get; set; } = [];

    public MeetingMenu meetingMenu = null!;

    public int Priority { get; set; } = 2;

    public void HideObjectionButtons()
    {
        meetingMenu?.HideButtons();
    }

    public void AssignTargets()
    {
        if (!OptionGroupSingleton<RoleOptions>.Instance.IsClassicRoleAssignment)
        {
            return;
        }

        var lawyers = new List<PlayerControl>();
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc != null && pc.IsRole<LawyerRole>() && !pc.HasDied())
            {
                lawyers.Add(pc);
            }
        }

        var assignedClients = new HashSet<byte>();

        var lawyerOptions = OptionGroupSingleton<LawyerOptions>.Instance;
        var killerChance = (int)lawyerOptions.KillerClientChance;
        Random rnd = new();

        foreach (var lawyer in lawyers)
        {
            PlayerControl? target = null;
            var chance = rnd.Next(1, 101);

            if (chance <= killerChance && killerChance > 0)
            {
                var killers = new List<PlayerControl>();
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc.IsRole<LawyerRole>() || pc.HasDied() ||
                        (!pc.IsImpostorAligned() && !pc.Is(RoleAlignment.NeutralKilling)) ||
                        pc.HasModifier<ExecutionerTargetModifier>() ||
                        pc.HasModifier<GuardianAngelTargetModifier>() ||
                        pc.HasModifier<AllianceGameModifier>() ||
                        pc.HasModifier<LoverModifier>() ||
                        SpectatorRole.TrackedSpectators.Contains(pc.Data.PlayerName) ||
                        assignedClients.Contains(pc.PlayerId))
                    {
                        continue;
                    }

                    killers.Add(pc);
                }

                if (killers.Count > 0)
                {
                    target = killers[rnd.Next(killers.Count)];
                }
            }

            if (target == null)
            {
                var allPlayers = new List<PlayerControl>();
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc.IsRole<LawyerRole>() || pc.HasDied() ||
                        pc.HasModifier<ExecutionerTargetModifier>() ||
                        pc.HasModifier<GuardianAngelTargetModifier>() ||
                        pc.HasModifier<AllianceGameModifier>() ||
                        pc.HasModifier<LoverModifier>() ||
                        SpectatorRole.TrackedSpectators.Contains(pc.Data.PlayerName) ||
                        assignedClients.Contains(pc.PlayerId))
                    {
                        continue;
                    }

                    // If killerChance is 0, exclude killers from the fallback pool
                    if (killerChance == 0 && (pc.IsImpostorAligned() || pc.Is(RoleAlignment.NeutralKilling)))
                    {
                        continue;
                    }

                    allPlayers.Add(pc);
                }

                if (allPlayers.Count > 0)
                {
                    target = allPlayers[rnd.Next(allPlayers.Count)];
                }
            }

            if (target != null)
            {
                assignedClients.Add(target.PlayerId);
                RpcSetLawyerClient(lawyer, target);
            }
            else
            {
                lawyer.GetRole<LawyerRole>()!.CheckClientDeath(null);
            }
        }
    }

    public RoleBehaviour CrewVariant => RoleManager.Instance.GetRole((RoleTypes)RoleId.Get<SnitchRole>());
    public DoomableType DoomHintType => DoomableType.Trickster;
    public string LocaleKey => "Lawyer";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => ClientString(true);
    public string RoleLongDescription => ClientString();

    public string GetAdvancedDescription()
    {
        return
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") +
            MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities
    {
        get
        {
            var maxObjections = (int)OptionGroupSingleton<LawyerOptions>.Instance.MaxObjections;
            if (maxObjections <= 0)
            {
                return [];
            }

            return new List<CustomButtonWikiDescription>
            {
                new(TouLocale.Get("ExtensionRoleLawyerObject", "Object"),
                    TouLocale.Get("ExtensionRoleLawyerObjectWikiDescription"),
                    TouExtensionAssets.ObjectionButtonSprite)
            };
        }
    }

    private string ClientString(bool capitalize = false)
    {
        string desc;
        if (Client != null)
        {
            desc = capitalize
                ? TouLocale.GetParsed("ExtensionRoleLawyerIntroBlurb")
                : TouLocale.GetParsed("ExtensionRoleLawyerTabDescription");
            desc = desc.Replace("<client>", Client.Data.PlayerName);
        }
        else
        {
            desc = TouLocale.GetParsed("ExtensionRoleLawyerIntroBlurb");
        }

        return capitalize ? desc.ToTitleCase() : desc;
    }

    public Color RoleColor => TownOfUsColors.Lawyer;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralBenign;

    public bool SetupIntroTeam(IntroCutscene instance,
        ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
    {
        if (Player != PlayerControl.LocalPlayer)
        {
            return true;
        }

        var lawyerTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();

        lawyerTeam.Add(PlayerControl.LocalPlayer);
        if (Client != null)
        {
            lawyerTeam.Add(Client);
        }

        yourTeam = lawyerTeam;

        return true;
    }

    public CustomRoleConfiguration Configuration => new(this)
    {
        IntroSound = TouMegaChujoweExtension.Assets.TouExtensionAudio.ObjectionSound,
        Icon = TouRoleIcons.Lawyer,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>(),
        OptionsScreenshot = TouBanners.NeutralRoleBanner,
    };

    public bool MetWinCon => Client != null && !Client.HasDied();

    public bool WinConditionMet()
    {
        /* Win condition depends on Client's survival and AboutToWin status */
        if (Player.HasDied() || !AboutToWin)
        {
            return false;
        }

        return Client != null && !Client.HasDied();
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);

        if (Client == null)
        {
            Client = LawyerUtils.GetClientForLawyer(Player);
        }

        if (Client != null)
        {
            var lawyerRole = RoleManager.Instance.GetRole((RoleTypes)RoleId.Get<LawyerRole>());
            if (!Player.HasModifier<LawyerRevealModifier>())
            {
                Player.AddModifier<LawyerRevealModifier>(lawyerRole);
            }
            
            if (!Client.HasModifier<ClientRevealModifier>())
            {
                var clientRole = Client.Data?.Role;
                if (clientRole != null)
                {
                    Client.AddModifier<ClientRevealModifier>(clientRole);
                }
            }
        }

        if (Player.AmOwner)
        {
            var maxObjections = (int)OptionGroupSingleton<LawyerOptions>.Instance.MaxObjections;
            if (maxObjections > 0)
            {
                meetingMenu = new MeetingMenu(this, OnObjectClick, MeetingAbilityType.Click,
                    TouExtensionAssets.ObjectionButtonSprite, TouExtensionAssets.ObjectionButtonSprite, IsExemptForObjection)
                {


                    Position = new Vector3(-0.35f, 0f, -3f)
                };
            }
        }

        if (TutorialManager.InstanceExists && Client == null &&
            AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started && Player.AmOwner &&
            Player.IsHost())
        {
            Coroutines.Start(SetTutorialTargets(this));
        }
    }

    [HideFromIl2Cpp]
    private static IEnumerator SetTutorialTargets(LawyerRole lawyer)
    {
        yield return new WaitForSeconds(0.01f);
        lawyer.AssignTargets();
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        if (TutorialManager.InstanceExists && Player.AmOwner)
        {
            var client = LawyerUtils.GetClientForLawyer(Player);
            if (client != null)
            {
                client.RpcRemoveModifier<LawyerTargetModifier>();
            }
        }

        if (!Player.HasModifier<BasicGhostModifier>() && ClientVoted)
        {
            Player.AddModifier<BasicGhostModifier>();
        }

        if (Player.AmOwner)
        {
            Dispose();
        }
    }

    public void Dispose()
    {
        meetingMenu?.Dispose();
        meetingMenu = null!;
    }

    public override void OnMeetingStart()
    {
        RoleBehaviourStubs.OnMeetingStart(this);
        ObjectionsUsedThisMeeting = 0; HasObjected = false;
        ObjectedVoters.Clear();
        ObjectedVoterOriginalVotes.Clear();

        if (Player.AmOwner && meetingMenu != null && Client != null)
        {
            var maxObjectionsPerMeeting = (int)OptionGroupSingleton<LawyerOptions>.Instance.MaxObjectionsPerMeeting;
            var maxObjections = (int)OptionGroupSingleton<LawyerOptions>.Instance.MaxObjections;

            if (maxObjectionsPerMeeting > 0 || maxObjections > 0)
            {
                meetingMenu.GenButtons(MeetingHud.Instance,
                    Player.AmOwner && !Player.HasDied() && Client != null && !Client.HasDied());

                Coroutines.Start(LawyerCoroutines.ScaleObjectionButton(this));
                Coroutines.Start(LawyerCoroutines.UpdateObjectionButton(this));
            }
        }
    }



    public override void OnVotingComplete()
    {
        RoleBehaviourStubs.OnVotingComplete(this);

        if (Player.AmOwner)
        {
            meetingMenu?.HideButtons();
        }
    }

    private static bool IsExempt(PlayerVoteArea voteArea)
    {
        var player = GameData.Instance.GetPlayerById(voteArea.TargetPlayerId)?.Object;
        return !player || !player?.Data || player!.Data.Disconnected || player.Data.IsDead;
    }

    private bool IsExemptForObjection(PlayerVoteArea voteArea)
    {
        if (Client == null || Client.HasDied() || voteArea.TargetPlayerId != Client.PlayerId)
        {
            return true;
        }

        return IsExempt(voteArea);
    }

    private void OnObjectClick(PlayerVoteArea voteArea, MeetingHud meeting)
    {
        if (meeting.state != MeetingHud.VoteStates.Voted && meeting.state != MeetingHud.VoteStates.NotVoted)
        {
            return;
        }

        if (IsExemptForObjection(voteArea))
        {
            return;
        }

        if (Client == null || Client.HasDied() || Player.HasDied())
        {
            return;
        }

        var maxObjections = (int)OptionGroupSingleton<LawyerOptions>.Instance.MaxObjections;
        var maxObjectionsPerMeeting = (int)OptionGroupSingleton<LawyerOptions>.Instance.MaxObjectionsPerMeeting;

        if (maxObjections > 0 && ObjectionsUsed >= maxObjections)
        {
            return;
        }
        if (maxObjectionsPerMeeting > 0 && ObjectionsUsedThisMeeting >= maxObjectionsPerMeeting)
        {
            return;
        }

        if (voteArea.TargetPlayerId != Client.PlayerId)
        {
            return;
        }

        var hasAnyVotes = meeting.playerStates.Any(pva =>
    pva.VotedFor != 255 && !pva.AmDead);
        if (!hasAnyVotes)
        {
            return;
        }

        if (IsInLastSecondsOfVoting(meeting, NoObjectLastSeconds))
        {
            return;
        }

        RpcObjectVotes(Player);
    }

    public static bool IsInLastSecondsOfVoting(MeetingHud meeting, float seconds)
    {
        if (meeting == null)
        {
            return false;
        }

        if (meeting.state is not (MeetingHud.VoteStates.Voted or MeetingHud.VoteStates.NotVoted))
        {
            return false;
        }



        var remaining = TryGetVotingSecondsRemainingFromUi(meeting);
        return remaining >= 0f && remaining <= seconds;
    }

    [HideFromIl2Cpp]
    private static float TryGetVotingSecondsRemainingFromUi(MeetingHud meeting)
    {
        var text = meeting.TimerText != null ? meeting.TimerText.text : null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return -1f;
        }


        var cleaned = Regex.Replace(text, "<.*?>", string.Empty);


        var match = SecondsRegex.Matches(cleaned).Cast<Match>().LastOrDefault(m => m.Success);
        if (match != null && int.TryParse(match.Groups[1].Value, out var sec))
        {
            return sec;
        }


        var match2 = LastNumberRegex.Match(cleaned);
        if (match2.Success && int.TryParse(match2.Groups[1].Value, out var sec2))
        {
            return sec2;
        }

        return -1f;
    }

    [MethodRpc((uint)ExtensionRpc.LawyerObject)]
    public static void RpcObjectVotes(PlayerControl lawyer)
    {
        var lawyerRole = lawyer.GetRole<LawyerRole>();
        if (lawyerRole == null || lawyerRole.Client == null || lawyerRole.Client.HasDied())
        {
            return;
        }

        var maxObjections = (int)OptionGroupSingleton<LawyerOptions>.Instance.MaxObjections;
        if (lawyerRole.ObjectionsUsed >= maxObjections)
        {
            return;
        }

        lawyerRole.ObjectionsUsed++; lawyerRole.ObjectionsUsedThisMeeting++; lawyerRole.HasObjected = true;

        var clip = TouExtensionAudio.ObjectionSound.LoadAsset();
        if (clip != null)
        {
            var source = SoundManager.Instance.PlaySound(clip, false, 1f);
            if (source != null) Coroutines.Start(LawyerCoroutines.CoFadeOutObjection(source, 1.2f, 0.5f));
        }

        var lawyerName = lawyer.Data.PlayerName;
        var title = $"<color=#{TownOfUsColors.Lawyer.ToHtmlStringRGBA()}>{TouLocale.Get("ExtensionRoleLawyer")}</color>";
        var message = TouLocale.Get("ExtensionLawyerObjectionNotification")
            .Replace("<lawyer>", lawyerName);

        MiscUtils.AddFakeChat(lawyer.Data, title, message, false, true);

        // Updated chat bubble handling: Show local player instead of random/lawyer 
        // (requested "everyone shows themselves")
        try
        {
            var chat = HudManager.Instance.Chat;
            if (chat != null)
            {
                var bubbles = chat.chatBubblePool.activeChildren;
                if (bubbles.Count > 0)
                {
                    var lastBubble = bubbles[bubbles.Count - 1].Cast<ChatBubble>();
                    if (lastBubble != null && lastBubble.Player != null)
                    {
                        lastBubble.Player.gameObject.SetActive(true);
                        lastBubble.SetCosmetics(PlayerControl.LocalPlayer.Data);
                        
                        // Medic style: Black background
                        if (lastBubble.Background != null)
                        {
                            lastBubble.Background.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);
                        }
                        if (lastBubble.TextArea != null)
                        {
                            lastBubble.TextArea.color = Color.white;
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            /* Failed to update chat avatar - non-critical UI error */
        }

        var meeting = MeetingHud.Instance;
        if (meeting == null)
        {
            return;
        }

        var lawyerOptions = OptionGroupSingleton<LawyerOptions>.Instance;
        if (lawyerOptions != null)
        {
            try
            {
                _ = lawyerOptions.ObjectionPreventsSameVote;
            }
            catch (Exception)
            {
                /* Option access failed - non-critical */
            }
        }

        foreach (var voteArea in meeting.playerStates)
        {
            if (voteArea.VotedFor != 255 && !voteArea.AmDead)
            {
                var voter = MiscUtils.PlayerById(voteArea.TargetPlayerId);
                if (voter == null)
                {
                    continue;
                }

                var originalVote = voteArea.VotedFor;
                voteArea.UnsetVote();

                var voteData = voter.GetVoteData();
                var removedCount = voteData.Votes.Count;
                voteData.Votes.Clear();
                voteData.VotesRemaining += removedCount;

                if (!lawyerRole.ObjectedVoters.Contains(voteArea.TargetPlayerId))
                {
                    lawyerRole.ObjectedVoters.Add(voteArea.TargetPlayerId);
                }

                var options = OptionGroupSingleton<LawyerOptions>.Instance;
                if (options != null && options.ObjectionPreventsSameVote)
                {
                    byte voteToStore = originalVote;
                    
                    if (voter.AmOwner)
                    {
                        var trackedVote = LawyerVoteBlockPatch.GetCurrentVote(voteArea.TargetPlayerId);
                        if (trackedVote.HasValue && trackedVote.Value != 255)
                        {
                            voteToStore = trackedVote.Value;
                        }
                        else
                        {
                            /* Current vote not found */
                        }
                    }
                    
                    if (voteToStore != 255)
                    {
                        LawyerEvents.AddObjectedVoter(voteArea.TargetPlayerId, voteToStore);
                    }
                    else
                    {
                        /* Vote to store was skip/null */
                    }
                }

                if (voter.AmOwner)
                {
                    meeting.ClearVote();
                }
            }
        }

        if (AmongUsClient.Instance.AmHost)
        {
            meeting.SetDirtyBit(1U);
        }

        var maxObjectionsPerMeeting = (int)OptionGroupSingleton<LawyerOptions>.Instance.MaxObjectionsPerMeeting;
        bool objectionsExhausted = false;
        if (maxObjections > 0 && lawyerRole.ObjectionsUsed >= maxObjections)
        {
            objectionsExhausted = true;
        }
        if (maxObjectionsPerMeeting > 0 && lawyerRole.ObjectionsUsedThisMeeting >= maxObjectionsPerMeeting)
        {
            objectionsExhausted = true;
        }

        if (objectionsExhausted && lawyerRole.meetingMenu != null && lawyerRole.Client != null)
        {
            lawyerRole.meetingMenu.HideSingle(lawyerRole.Client.PlayerId);
        }
    }

    public override void OnDeath(DeathReason reason)
    {
        RoleBehaviourStubs.OnDeath(this, reason);

        Client = null;
    }

    public override bool CanUse(IUsable usable)
    {
        return GameManager.Instance.LogicUsables.CanUse(usable, Player);
    }

    public override bool DidWin(GameOverReason gameOverReason)
    {
        if (Player.HasDied() || Client == null || Client.HasDied())
        {
            return false;
        }

        if (gameOverReason == CustomGameOver.GameOverReason<ExtensionNeutralGameOver>())
        {
            return true;
        }

        if (OptionGroupSingleton<LawyerOptions>.Instance.WinMode == LawyerWinMode.WinWithClient)
        {
            try
            {
                if (Client.Data?.Role != null && Client.Data.Role.DidWin(gameOverReason))
                {
                    return true;
                }
            }
            catch
            {
                /* ignore win-check failure for client role */
            }

            try
            {
                if (Client.GetModifiers<GameModifier>().Any(m => m.DidWin(gameOverReason) == true))
                {
                    return true;
                }
            }
            catch
            {
                /* ignore win-check failure for client modifiers */
            }
        }

        return false;
    }

    public void CheckClientDeath(PlayerControl? victim)
    {
        if (AboutToWin || ClientVoted)
        {
            return;
        }

        if (Client == null || victim == Client)
        {
            var dieOnClientDeath = OptionGroupSingleton<LawyerOptions>.Instance.DieOnClientDeath;
            if (dieOnClientDeath && !Player.HasDied())
            {
                var showAnim = MeetingHud.Instance == null && ExileController.Instance == null;
                var murderResultFlags = MurderResultFlags.Succeeded | MurderResultFlags.DecisionByHost;

                DeathHandlerModifier.UpdateDeathHandlerImmediate(Player,
                    TouLocale.Get("ExtensionLawyerDiedClientDeath"),
                    DeathEventHandlers.CurrentRound,
                    (!MeetingHud.Instance && !ExileController.Instance)
                        ? DeathHandlerOverride.SetTrue
                        : DeathHandlerOverride.SetFalse,
                    lockInfo: DeathHandlerOverride.SetTrue);

                Player.CustomMurder(
                    Player,
                    murderResultFlags,
                    false,
                    showAnim,
                    false,
                    showAnim,
                    false);
                return;
            }

            var roleType = ((BecomeOptions)OptionGroupSingleton<LawyerOptions>.Instance.OnClientDeath.Value) switch
            {
                BecomeOptions.Crew => (ushort)RoleTypes.Crewmate,
                BecomeOptions.Jester => RoleId.Get<JesterRole>(),
                BecomeOptions.Survivor => RoleId.Get<SurvivorRole>(),
                BecomeOptions.Amnesiac => RoleId.Get<AmnesiacRole>(),
                BecomeOptions.Mercenary => RoleId.Get<MercenaryRole>(),
                _ => (ushort)RoleTypes.Crewmate
            };

            if (Player.HasModifier<LawyerRevealModifier>())
            {
                Player.RemoveModifier<LawyerRevealModifier>();
            }

            if (Client != null && Client.HasModifier<LawyerTargetModifier>())
            {
                Client.RemoveModifier<LawyerTargetModifier>();
            }

            Client = null;

            Player.ChangeRole(roleType);

            if ((roleType == RoleId.Get<JesterRole>() && OptionGroupSingleton<JesterOptions>.Instance.ScatterOn) ||
                (roleType == RoleId.Get<SurvivorRole>() && OptionGroupSingleton<SurvivorOptions>.Instance.ScatterOn))
            {
                StartCoroutine(Effects.Lerp(0.2f,
                    new Action<float>(p => { Player.GetModifier<ScatterModifier>()?.OnRoundStart(); })));
            }
        }
    }

    [MethodRpc((uint)ExtensionRpc.SetLawyerClient)]
    public static void RpcSetLawyerClient(PlayerControl player, PlayerControl client)
    {
        if (player.Data.Role is not LawyerRole)
        {

            return;
        }

        if (client == null)
        {
            return;
        }

        var role = player.GetRole<LawyerRole>();

        if (role == null)
        {
            return;
        }

        var existingModifiers = client.GetModifiers<LawyerTargetModifier>().ToList();
        foreach (var modifier in existingModifiers)
        {
            client.RpcRemoveModifier<LawyerTargetModifier>();

            PlayerControl? previousLawyer = null;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc != null && pc.PlayerId == modifier.OwnerId && pc.IsRole<LawyerRole>())
                {
                    previousLawyer = pc;
                    break;
                }
            }
            if (previousLawyer != null)
            {
                var previousLawyerRole = previousLawyer.GetRole<LawyerRole>();
                if (previousLawyerRole != null && previousLawyerRole.Client?.PlayerId == client.PlayerId)
                {
                    previousLawyerRole.Client = null;
                }
            }
        }

        role.Client = client;

        client.AddModifier<LawyerTargetModifier>(player.PlayerId);

        LawyerDuoTracker.SetClient(player.PlayerId, client.PlayerId);

        var lawyerRole = RoleManager.Instance.GetRole((RoleTypes)RoleId.Get<LawyerRole>());
        if (!player.HasModifier<LawyerRevealModifier>())
        {
            player.AddModifier<LawyerRevealModifier>(lawyerRole);
        }

        if (!client.HasModifier<ClientRevealModifier>())
        {
            var clientRole = client.Data?.Role;
            if (clientRole != null)
            {
                client.AddModifier<ClientRevealModifier>(clientRole);
            }
        }
    }

}

public static class LawyerCoroutines
{
    [HideFromIl2Cpp]
    public static IEnumerator ScaleObjectionButton(LawyerRole lawyer)
    {
        yield return new WaitForSeconds(0.1f);

        if (lawyer.meetingMenu == null || lawyer.Client == null || lawyer.Client.HasDied())
        {
            yield break;
        }

        var meeting = MeetingHud.Instance;
        if (meeting == null)
        {
            yield break;
        }

        var voteArea = meeting.playerStates.FirstOrDefault(pva => pva.TargetPlayerId == lawyer.Client.PlayerId);
        if (voteArea == null || voteArea.NameText == null)
        {
            yield break;
        }

        if (lawyer.meetingMenu.Buttons.TryGetValue(lawyer.Client.PlayerId, out var button) && button != null)
        {
            voteArea.NameText.ForceMeshUpdate();

            float textWidth = 0f;
            if (voteArea.NameText.textBounds.size.x > 0)
            {
                textWidth = voteArea.NameText.textBounds.size.x / 2f;
            }
            else if (voteArea.NameText.preferredWidth > 0)
            {
                textWidth = voteArea.NameText.preferredWidth / 2f;
            }

            var nameTextLocalPos = voteArea.NameText.transform.localPosition;
            button.transform.localPosition = new Vector3(nameTextLocalPos.x + textWidth + 0.15f, nameTextLocalPos.y, -1f);
            button.transform.localScale = new Vector3(0.07f, 0.07f, 1f);
        }
    }

    [HideFromIl2Cpp]
    public static IEnumerator UpdateObjectionButton(LawyerRole lawyer)
    {
        while (MeetingHud.Instance != null)
        {
            yield return new WaitForSeconds(0.1f);

            if (lawyer.meetingMenu == null || lawyer.Client == null || lawyer.Client.HasDied())
            {
                continue;
            }

            var meeting = MeetingHud.Instance;
            var maxObjections = (int)OptionGroupSingleton<LawyerOptions>.Instance.MaxObjections;
            var maxObjectionsPerMeeting = (int)OptionGroupSingleton<LawyerOptions>.Instance.MaxObjectionsPerMeeting;

            if (!lawyer.meetingMenu.Buttons.TryGetValue(lawyer.Client.PlayerId, out var buttonGo) || buttonGo == null)
            {
                lawyer.meetingMenu.GenButtons(meeting, lawyer.Player.AmOwner && !lawyer.Player.HasDied() && !lawyer.Client.HasDied());
                lawyer.meetingMenu.Buttons.TryGetValue(lawyer.Client.PlayerId, out buttonGo);
            }

            bool objectionsExhausted = false;
            if (maxObjections > 0 && lawyer.ObjectionsUsed >= maxObjections)
            {
                objectionsExhausted = true;
            }
            if (maxObjectionsPerMeeting > 0 && lawyer.ObjectionsUsedThisMeeting >= maxObjectionsPerMeeting)
            {
                objectionsExhausted = true;
            }

            var showButton = !objectionsExhausted &&
                             maxObjections > 0 &&
                             (meeting.state == MeetingHud.VoteStates.Voted || meeting.state == MeetingHud.VoteStates.NotVoted) &&
                             !LawyerRole.IsInLastSecondsOfVoting(meeting, 20f);

            if (buttonGo != null)
            {
                buttonGo.SetActive(showButton);
            }

            if (showButton && buttonGo != null)
            {
                var voteArea = meeting.playerStates.FirstOrDefault(pva => pva.TargetPlayerId == lawyer.Client.PlayerId);
                if (voteArea != null && voteArea.NameText != null)
                {
                    voteArea.NameText.ForceMeshUpdate();

                    float textWidth = 0f;
                    if (voteArea.NameText.textBounds.size.x > 0)
                    {
                        textWidth = voteArea.NameText.textBounds.size.x / 2f;
                    }
                    else if (voteArea.NameText.preferredWidth > 0)
                    {
                        textWidth = voteArea.NameText.preferredWidth / 2f;
                    }

                    var nameTextLocalPos = voteArea.NameText.transform.localPosition;
                    buttonGo.transform.localPosition = new Vector3(nameTextLocalPos.x + textWidth + 0.15f, nameTextLocalPos.y, -1f);
                }
            }
        }
    }

    [HideFromIl2Cpp]
    public static IEnumerator CoFadeOutObjection(AudioSource source, float delay, float duration)
    {
        yield return new WaitForSeconds(delay);
        if (source == null) yield break;
        float startVolume = source.volume;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            if (source == null) yield break;
            source.volume = Mathf.Lerp(startVolume, 0, t / duration);
            yield return null;
        }
        if (source != null) source.Stop();
    }
}




















