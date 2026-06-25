using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Utilities.Extensions;
using System.Text;
using TMPro;
using TownOfUs.Extensions;
using TownOfUs.Modifiers.Impostor;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine.Events;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Crewmate;

public sealed class PresidentRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITouCrewRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Insight;

    public const byte AbstainTargetId = 250;

    [HideFromIl2Cpp] public PlayerVoteArea? AbstainButton { get; private set; }

    public int VoteBank { get; set; } = -1;

    public bool SelectingAbstain { get; set; }

    public bool HasAbstained { get; set; }

    public bool HasVotedOnPlayer { get; set; }

    public string LocaleKey => "President";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "President");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public override bool IsAffectedByComms => false;

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") +
               TownOfUs.Utilities.MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities
    {
        get
        {
            return
            [
                new(
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}AbstainWiki", "Abstain"),
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}AbstainWikiDescription"),
                    TouExtensionIcons.PresidentRoleIcon)
            ];
        }
    }

    public Color RoleColor => TouExtensionColors.President;
    public ModdedRoleTeams Team => ModdedRoleTeams.Crewmate;
    public RoleAlignment RoleAlignment => RoleAlignment.CrewmatePower;
    public bool IsPowerCrew => true;

    public CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TouExtensionIcons.PresidentRoleIcon,
        IntroSound = TownOfUs.Assets.TouAudio.ProsIntroSound,
        OptionsScreenshot = TouExtensionBanners.PresidentBanner,
    };

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);

        if (OptionGroupSingleton<PresidentOptions>.Instance != null)
        {
            VoteBank = (int)OptionGroupSingleton<PresidentOptions>.Instance.StartingVoteBank;
        }
        else
        {
            VoteBank = 2;
        }
    }

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var text = ITownOfUsRole.SetNewTabText(this);
        text.AppendLine($"Vote Bank: {VoteBank}");
        return text;
    }

    [HideFromIl2Cpp]
    public bool IsBlackmailActive()
    {
        if (Player == null) return false;
        if (!Player.HasModifier<BlackmailedModifier>()) return false;
        return Helpers.GetAlivePlayers().Count > BlackmailedModifier.MaxAlivesNeeded;
    }

    public void FixedUpdate()
    {
        if (Player == null || Player.Data.Role is not PresidentRole)
        {
            return;
        }

        var meeting = MeetingHud.Instance;

        if (!Player.AmOwner || meeting == null || AbstainButton == null)
        {
            return;
        }

        var voteData = Player.GetVoteData();
        var hasVotesLeft = voteData != null && voteData.VotesRemaining > 0;
        var blackmailed = IsBlackmailActive();

        AbstainButton.gameObject.SetActive(
            hasVotesLeft &&
            !blackmailed &&
            !HasAbstained &&
            !HasVotedOnPlayer &&
            meeting.state == MeetingHud.VoteStates.NotVoted &&
            !meeting.SkipVoteButton.voteComplete);

        if (!AbstainButton.gameObject.active)
        {
            if (SelectingAbstain)
            {
                SelectingAbstain = false;
                AbstainButton.ClearButtons();
            }
            return;
        }

        if (meeting.state == MeetingHud.VoteStates.Discussion &&
            meeting.discussionTimer < GameOptionsManager.Instance.currentNormalGameOptions.DiscussionTime)
        {
            AbstainButton.SetDisabled();
        }
        else
        {
            AbstainButton.SetEnabled();
        }

        AbstainButton.voteComplete = meeting.SkipVoteButton.voteComplete;
    }

    public bool HasCastKnightedVote { get; set; }

    public override void OnMeetingStart()
    {
        RoleBehaviourStubs.OnMeetingStart(this);

        if (!Player.AmOwner)
        {
            return;
        }

        SelectingAbstain = false;
        HasAbstained = false;
        HasVotedOnPlayer = false;
        HasCastKnightedVote = false;

        var voteData = Player.GetVoteData();
        if (voteData != null)
        {
            var totalVotes = 1 + VoteBank;
            voteData.SetRemainingVotes(totalVotes);
        }

        CreateAbstainButton();
    }

    private void CreateAbstainButton()
    {
        var meeting = MeetingHud.Instance;
        if (meeting == null)
        {
            return;
        }

        var skip = meeting.SkipVoteButton;
        AbstainButton = UnityEngine.Object.Instantiate(skip, skip.transform.parent);
        AbstainButton.Parent = meeting;
        AbstainButton.SetTargetPlayerId(AbstainTargetId);
        AbstainButton.transform.localPosition = skip.transform.localPosition + new Vector3(0f, -0.17f, 0f);

        AbstainButton.gameObject.GetComponentInChildren<TextTranslatorTMP>().Destroy();
        AbstainButton.gameObject.GetComponentInChildren<TextMeshPro>().text =
            TouLocale.Get("ExtensionRolePresidentAbstain", "ABSTAIN").ToUpperInvariant();

        AbstainButton.gameObject.name = "button_abstainButton";

        skip.transform.localPosition += new Vector3(0f, 0.20f, 0f);

        foreach (var playerVoteArea in meeting.playerStates)
        {
            playerVoteArea.gameObject.GetComponentInChildren<PassiveButton>().OnClick
                .AddListener((UnityAction)(() =>
                {
                    if (AbstainButton != null)
                    {
                        AbstainButton.ClearButtons();
                        SelectingAbstain = false;
                    }
                }));
        }

        skip.gameObject.GetComponentInChildren<PassiveButton>().OnClick
            .AddListener((UnityAction)(() =>
            {
                if (AbstainButton != null)
                {
                    AbstainButton.ClearButtons();
                    SelectingAbstain = false;
                }
            }));

        AbstainButton.gameObject.GetComponentInChildren<PassiveButton>().OnClick
            .AddListener((UnityAction)(() =>
            {
                skip.ClearButtons();
            }));
    }

    public void DoAbstain()
    {
        var meeting = MeetingHud.Instance;
        if (meeting == null)
        {
            return;
        }

        HasAbstained = true;

        var bonus = (int)OptionGroupSingleton<PresidentOptions>.Instance.AbstainBonus;
        var maxBank = (int)OptionGroupSingleton<PresidentOptions>.Instance.MaxVoteBank;
        VoteBank = System.Math.Min(VoteBank + bonus, maxBank);

        if (Player.AmOwner)
        {
            if (Constants.ShouldPlaySfx())
            {
                SoundManager.Instance.PlaySound(meeting.VoteLockinSound, false);
            }

            meeting.SkipVoteButton.voteComplete = true;
            meeting.SkipVoteButton.gameObject.SetActive(false);

            foreach (var playerVoteArea in meeting.playerStates)
            {
                playerVoteArea.ClearButtons();
            }

            meeting.SkipVoteButton.ClearButtons();

            if (AbstainButton != null)
            {
                AbstainButton.ClearButtons();
                AbstainButton.gameObject.SetActive(false);
            }

            SelectingAbstain = false;
        }
    }

    public void OnMeetingEnd()
    {
        if (AbstainButton != null)
        {
            UnityEngine.Object.Destroy(AbstainButton.gameObject);
        }

        AbstainButton = null;
        SelectingAbstain = false;
        HasAbstained = false;
        HasVotedOnPlayer = false;
    }
}
