using MiraAPI.GameOptions;
using MiraAPI.LocalSettings;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using Reactor.Utilities.Extensions;
using TMPro;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Utilities;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Modifiers.Universal;
using TouMegaChujoweExtension.Options.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Modifiers.Game;
using TownOfUs.Options;
using TownOfUs.Options.Roles.Crewmate;
using TownOfUs.Options.Roles.Neutral;
using TownOfUs.Roles;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TouMegaChujoweExtension.Modules;

public static class ClassicAssassinSystem
{
    private static readonly Dictionary<byte, (GameObject cycleBack, GameObject cycleForward, GameObject guess, TextMeshPro guessText)> Buttons = new();
    private static readonly Dictionary<byte, int> GuessIndices = new();
    private static readonly Dictionary<byte, int> SavedGuessIndices = new();
    private static List<GuessEntry> _guessableEntries = new();
    private static int _remainingKills;
    private static bool _guessedThisMeeting;

    public static bool IsActive =>
        LocalSettingsTabSingleton<TouExtensionLocalSettings>.Instance.UseClassicAssassinGuessing.Value;

    private class GuessEntry
    {
        public string Name { get; set; } = "";
        public Color Color { get; set; }
        public RoleBehaviour? Role { get; set; }
        public BaseModifier? Modifier { get; set; }
        public bool IsModifier => Modifier != null;
    }

    private static bool IsMayorRevealed(PlayerControl player)
    {
        return player.Data?.Role is MayorRole &&
               player.TryGetModifier<TownOfUs.Modifiers.Crewmate.MayorRevealModifier>(out var mayorReveal) &&
               mayorReveal.Visible;
    }

    private static bool IsForestallerRevealed(PlayerControl player)
    {
        return player.Data?.Role is ForestallerRole &&
               player.TryGetModifier<ForestallerMeetingRevealModifier>(out var forestallerReveal) &&
               forestallerReveal.Visible;
    }

    private static List<RoleBehaviour> GetGuessableRolesWithDynamicSupport(IEnumerable<RoleBehaviour> baseRoles)
    {
        var roles = baseRoles.ToList();

        var extraGuessableRoles = MiscUtils.AllRoles
            .Where(r => r is IGuessable guessable && guessable.CanBeGuessed)
            .Where(r => roles.All(x => x.Role != r.Role))
            .ToList();

        roles.AddRange(extraGuessableRoles);

        return roles
            .OrderBy(r => r.GetRoleName())
            .ToList();
    }

    public static void Reset()
    {
        SaveCurrentGuesses();

        foreach (var (_, (cycleBack, cycleForward, guess, guessText)) in Buttons)
        {
            if (cycleBack != null) Object.Destroy(cycleBack);
            if (cycleForward != null) Object.Destroy(cycleForward);
            if (guess != null) Object.Destroy(guess);
            if (guessText != null) Object.Destroy(guessText.gameObject);
        }

        Buttons.Clear();
        GuessIndices.Clear();
        _guessableEntries.Clear();
        _guessedThisMeeting = false;
        _remainingKills = 0;
    }

    public static void FullReset()
    {
        foreach (var (_, (cycleBack, cycleForward, guess, guessText)) in Buttons)
        {
            if (cycleBack != null) Object.Destroy(cycleBack);
            if (cycleForward != null) Object.Destroy(cycleForward);
            if (guess != null) Object.Destroy(guess);
            if (guessText != null) Object.Destroy(guessText.gameObject);
        }

        Buttons.Clear();
        GuessIndices.Clear();
        SavedGuessIndices.Clear();
        _guessableEntries.Clear();
        _guessedThisMeeting = false;
        _remainingKills = 0;
    }

    private static void SaveCurrentGuesses()
    {
        foreach (var (targetId, index) in GuessIndices)
        {
            if (index >= 0)
            {
                SavedGuessIndices[targetId] = index;
            }
        }
    }

    private static int GetSavedGuessIndex(byte targetId)
    {
        if (!SavedGuessIndices.TryGetValue(targetId, out var savedIndex))
            return -1;

        if (savedIndex < 0 || savedIndex >= _guessableEntries.Count)
            return -1;

        return savedIndex;
    }

    // =========================
    // ASSASSIN
    // =========================
    public static void GenerateButtons(MeetingHud meetingHud, AssassinModifier assassin)
    {
        Reset();

        if (!PlayerControl.LocalPlayer.AmOwner) return;
        if (PlayerControl.LocalPlayer.Data.IsDead) return;
        if (PlayerControl.LocalPlayer.HasModifier<TownOfUs.Modifiers.Crewmate.JailedModifier>()) return;
        if (assassin.maxKills <= 0) return;

        _remainingKills = assassin.maxKills;
        BuildGuessableListForAssassin(assassin);

        if (_guessableEntries.Count == 0) return;

        foreach (var voteArea in meetingHud.playerStates)
        {
            if (IsExempt(voteArea, assassin))
                continue;

            GenSharedButton(voteArea, () => DoGuess(voteArea.TargetPlayerId, assassin));
        }
    }

    // =========================
    // VIGILANTE
    // =========================
    public static void GenerateButtons(MeetingHud meetingHud, VigilanteRole vigilante)
    {
        Reset();

        if (!PlayerControl.LocalPlayer.AmOwner) return;
        if (PlayerControl.LocalPlayer.Data.IsDead) return;
        if (PlayerControl.LocalPlayer.HasModifier<TownOfUs.Modifiers.Crewmate.JailedModifier>()) return;
        if (vigilante.MaxKills <= 0) return;

        _remainingKills = vigilante.MaxKills;
        BuildGuessableListForVigilante(vigilante);

        if (_guessableEntries.Count == 0) return;

        foreach (var voteArea in meetingHud.playerStates)
        {
            if (IsExempt(voteArea, vigilante))
                continue;

            GenSharedButton(voteArea, () => DoGuess(voteArea.TargetPlayerId, vigilante));
        }
    }

    // =========================
    // DOOMSAYER
    // =========================
    public static void GenerateButtons(MeetingHud meetingHud, DoomsayerRole doomsayer)
    {
        Reset();

        if (!PlayerControl.LocalPlayer.AmOwner) return;
        if (PlayerControl.LocalPlayer.Data.IsDead) return;
        if (PlayerControl.LocalPlayer.HasModifier<TownOfUs.Modifiers.Crewmate.JailedModifier>()) return;
        if (doomsayer.NumberOfGuesses >= OptionGroupSingleton<DoomsayerOptions>.Instance.DoomsayerGuessesToWin) return;

        BuildGuessableListForDoomsayer(doomsayer);

        if (_guessableEntries.Count == 0) return;

        foreach (var voteArea in meetingHud.playerStates)
        {
            if (IsExempt(voteArea, doomsayer))
                continue;

            GenSharedButton(voteArea, () => DoGuess(voteArea.TargetPlayerId, doomsayer));
        }
    }

    // =========================
    // SHARED UI
    // =========================
    private static void GenSharedButton(PlayerVoteArea voteArea, System.Action onGuess)
    {
        var targetId = voteArea.TargetPlayerId;

        var confirmButton = voteArea.Buttons.transform.GetChild(0).gameObject;
        var parent = confirmButton.transform.parent.parent;

        var nameText = Object.Instantiate(voteArea.NameText, voteArea.transform);
        voteArea.NameText.transform.localPosition = new Vector3(0.55f, 0.12f, -0.1f);
        nameText.transform.localPosition = new Vector3(0.55f, -0.12f, -0.1f);

        var savedIndex = GetSavedGuessIndex(targetId);
        if (savedIndex >= 0)
        {
            GuessIndices[targetId] = savedIndex;
            var entry = _guessableEntries[savedIndex];
            nameText.text = $"<color=#{ColorUtility.ToHtmlStringRGBA(entry.Color)}>{entry.Name}</color>";
        }
        else
        {
            GuessIndices[targetId] = -1;
            nameText.text = "Guess";
        }

        var cycleBack = Object.Instantiate(confirmButton, voteArea.transform);
        var cycleRendererBack = cycleBack.GetComponent<SpriteRenderer>();
        cycleRendererBack.sprite = TouExtensionAssets.CycleBack.LoadAsset();
        cycleBack.transform.localPosition = new Vector3(-0.5f, 0.15f, -2f);
        cycleBack.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        cycleBack.layer = 5;
        cycleBack.transform.parent = parent;
        var cycleEventBack = new Button.ButtonClickedEvent();
        cycleEventBack.AddListener((UnityEngine.Events.UnityAction)(() => CycleGuess(targetId, nameText, false)));
        cycleBack.GetComponent<PassiveButton>().OnClick = cycleEventBack;
        var cycleColliderBack = cycleBack.GetComponent<BoxCollider2D>();
        cycleColliderBack.size = cycleRendererBack.sprite.bounds.size;
        cycleColliderBack.offset = Vector2.zero;
        cycleBack.transform.GetChild(0).gameObject.Destroy();

        var cycleForward = Object.Instantiate(confirmButton, voteArea.transform);
        var cycleRendererForward = cycleForward.GetComponent<SpriteRenderer>();
        cycleRendererForward.sprite = TouExtensionAssets.CycleForward.LoadAsset();
        cycleForward.transform.localPosition = new Vector3(-0.2f, 0.15f, -2f);
        cycleForward.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        cycleForward.layer = 5;
        cycleForward.transform.parent = parent;
        var cycleEventForward = new Button.ButtonClickedEvent();
        cycleEventForward.AddListener((UnityEngine.Events.UnityAction)(() => CycleGuess(targetId, nameText, true)));
        cycleForward.GetComponent<PassiveButton>().OnClick = cycleEventForward;
        var cycleColliderForward = cycleForward.GetComponent<BoxCollider2D>();
        cycleColliderForward.size = cycleRendererForward.sprite.bounds.size;
        cycleColliderForward.offset = Vector2.zero;
        cycleForward.transform.GetChild(0).gameObject.Destroy();

        var guessBtn = Object.Instantiate(confirmButton, voteArea.transform);
        var guessRenderer = guessBtn.GetComponent<SpriteRenderer>();
        guessRenderer.sprite = TouExtensionAssets.GuessButton.LoadAsset();
        guessBtn.transform.localPosition = new Vector3(-0.35f, -0.15f, -2f);
        guessBtn.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        guessBtn.layer = 5;
        guessBtn.transform.parent = parent;
        var guessEvent = new Button.ButtonClickedEvent();
        guessEvent.AddListener((UnityEngine.Events.UnityAction)(() => onGuess()));
        guessBtn.GetComponent<PassiveButton>().OnClick = guessEvent;
        var guessCollider = guessBtn.GetComponent<BoxCollider2D>();
        guessCollider.size = guessRenderer.sprite.bounds.size;
        guessCollider.offset = Vector2.zero;
        guessBtn.transform.GetChild(0).gameObject.Destroy();

        Buttons[targetId] = (cycleBack, cycleForward, guessBtn, nameText);
    }

    private static void CycleGuess(byte targetId, TextMeshPro nameText, bool forward)
    {
        if (MeetingHud.Instance.state == MeetingHud.VoteStates.Discussion) return;
        if (_guessableEntries.Count == 0) return;

        if (!GuessIndices.TryGetValue(targetId, out var currentIndex))
            currentIndex = -1;

        if (forward)
        {
            currentIndex++;
            if (currentIndex >= _guessableEntries.Count)
                currentIndex = 0;
        }
        else
        {
            currentIndex--;
            if (currentIndex < 0)
                currentIndex = _guessableEntries.Count - 1;
        }

        GuessIndices[targetId] = currentIndex;
        SavedGuessIndices[targetId] = currentIndex;

        var entry = _guessableEntries[currentIndex];
        nameText.text = $"<color=#{ColorUtility.ToHtmlStringRGBA(entry.Color)}>{entry.Name}</color>";
    }

    // =========================
    // LIST BUILDERS
    // =========================
    private static void BuildGuessableListForAssassin(AssassinModifier assassin)
    {
        _guessableEntries.Clear();

        var roles = GetGuessableRolesWithDynamicSupport(
            MiscUtils.GetPotentialRoles().Where(r => IsRoleValid(r, assassin))
        );

        foreach (var role in roles)
        {
            _guessableEntries.Add(new GuessEntry
            {
                Name = role.GetRoleName(),
                Color = role.TeamColor,
                Role = role
            });
        }

        var options = OptionGroupSingleton<AssassinOptions>.Instance;

        if (options.AssassinGuessCrewModifiers || options.AssassinGuessAlliances)
        {
            var modifiers = MiscUtils.AllModifiers
                .Where(m => IsModifierValid(m, options))
                .OrderBy(m => m.ModifierName)
                .ToList();

            foreach (var mod in modifiers)
            {
                var color = mod switch
                {
                    IColoredModifier colored => colored.ModifierColor,
                    _ => MiscUtils.GetRoleColour(mod.ModifierName.Replace(" ", string.Empty))
                };
                _guessableEntries.Add(new GuessEntry
                {
                    Name = mod.ModifierName,
                    Color = color,
                    Modifier = mod
                });
            }
        }
    }

    private static void BuildGuessableListForVigilante(VigilanteRole vigilante)
    {
        _guessableEntries.Clear();

        var roles = GetGuessableRolesWithDynamicSupport(
            MiscUtils.GetPotentialRoles().Where(r => IsRoleValid(r, vigilante))
        );

        foreach (var role in roles)
        {
            _guessableEntries.Add(new GuessEntry
            {
                Name = role.GetRoleName(),
                Color = role.TeamColor,
                Role = role
            });
        }

        var options = OptionGroupSingleton<VigilanteOptions>.Instance;
        if (options.VigilanteGuessAlliances || options.VigilanteGuessKillerMods)
        {
            var modifiers = MiscUtils.AllModifiers
                .Where(IsModifierValidForVigilante)
                .OrderBy(m => m.ModifierName)
                .ToList();

            foreach (var mod in modifiers)
            {
                var color = mod switch
                {
                    IColoredModifier colored => colored.ModifierColor,
                    _ => MiscUtils.GetRoleColour(mod.ModifierName.Replace(" ", string.Empty))
                };
                _guessableEntries.Add(new GuessEntry
                {
                    Name = mod.ModifierName,
                    Color = color,
                    Modifier = mod
                });
            }
        }
    }

    private static void BuildGuessableListForDoomsayer(DoomsayerRole doomsayer)
    {
        _guessableEntries.Clear();

        var roles = GetGuessableRolesWithDynamicSupport(
            MiscUtils.GetPotentialRoles().Where(IsRoleValidForDoomsayer)
        );

        foreach (var role in roles)
        {
            _guessableEntries.Add(new GuessEntry
            {
                Name = role.GetRoleName(),
                Color = role.TeamColor,
                Role = role
            });
        }
    }

    // =========================
    // ROLE VALIDATION
    // =========================
    private static bool IsRoleValid(RoleBehaviour role, AssassinModifier assassin)
    {
        if (role.IsDead) return false;
        if (role is IUnguessable { IsGuessable: false }) return false;
        if (role is TownOfUs.Roles.Impostor.TraitorRole && assassin.Player.IsImpostorAligned()) return false;

        var options = OptionGroupSingleton<AssassinOptions>.Instance;
        var alignment = role.GetRoleAlignment();

        if (alignment == RoleAlignment.GameOutlier) return false;

        if (alignment == RoleAlignment.CrewmateInvestigative)
            return options.AssassinGuessInvest;

        if (role.IsCrewmate() && role is MiraAPI.Roles.ICustomRole)
            return true;

        if (role.IsCrewmate() && options.AssassinCrewmateGuess)
            return true;

        var assassinAlignment = assassin.Player.Data.Role.GetRoleAlignment();

        if (role.TeamType == RoleTeamTypes.Impostor &&
            options.AssassinGuessImpostors &&
            assassinAlignment is RoleAlignment.NeutralKilling or RoleAlignment.NeutralEvil)
            return true;

        if (alignment == RoleAlignment.NeutralBenign)
            return options.AssassinGuessNeutralBenign;

        if (alignment == RoleAlignment.NeutralEvil)
            return options.AssassinGuessNeutralEvil;

        if (alignment == RoleAlignment.NeutralKilling)
            return options.AssassinGuessNeutralKilling;

        if (alignment == RoleAlignment.NeutralOutlier)
            return options.AssassinGuessNeutralOutlier;

        return false;
    }

    private static bool IsRoleValid(RoleBehaviour role, VigilanteRole vigilante)
    {
        if (role.IsDead) return false;
        if (role is IUnguessable { IsGuessable: false }) return false;

        var options = OptionGroupSingleton<VigilanteOptions>.Instance;

        if (role.IsCrewmate() &&
            !(PlayerControl.LocalPlayer.TryGetModifier<AllianceGameModifier>(out var allyMod) && !allyMod.GetsPunished))
        {
            return false;
        }

        var alignment = role.GetRoleAlignment();

        if (!OptionGroupSingleton<AssassinOptions>.Instance.AssassinGuessInvest &&
            alignment == RoleAlignment.CrewmateInvestigative)
        {
            return false;
        }

        if (role.IsCrewmate()) return true;
        if (role.TeamType == RoleTeamTypes.Impostor) return true;

        if (alignment == RoleAlignment.NeutralBenign)
            return options.VigilanteGuessNeutralBenign.Value;
        if (alignment == RoleAlignment.NeutralEvil)
            return options.VigilanteGuessNeutralEvil.Value;
        if (alignment == RoleAlignment.NeutralKilling)
            return options.VigilanteGuessNeutralKilling.Value;
        if (alignment == RoleAlignment.NeutralOutlier)
            return options.VigilanteGuessNeutralOutlier.Value;

        return false;
    }

    private static bool IsRoleValidForDoomsayer(RoleBehaviour role)
    {
        var unguessableRole = role as IUnguessable;
        if (role.IsDead || role is IGhostRole || (unguessableRole != null && !unguessableRole.IsGuessable))
            return false;

        if (role.GetRoleAlignment() == RoleAlignment.CrewmateInvestigative)
            return OptionGroupSingleton<DoomsayerOptions>.Instance.DoomGuessInvest;

        return true;
    }

    // =========================
    // MODIFIER VALIDATION
    // =========================
    private static bool IsModifierValid(BaseModifier modifier, AssassinOptions options)
    {
        if (modifier is DeathNoteModifier)
            return true;

        if (modifier is VenomousModifier)
            return true;

        if (modifier is ChildModifier)
            return false;

        if (modifier is TouGameModifier touMod &&
            (touMod.CustomAmount <= 0 || touMod.CustomChance <= 0))
            return false;

        if (modifier is AllianceGameModifier allyMod &&
            (allyMod.CustomAmount <= 0 || allyMod.CustomChance <= 0))
            return false;

        if (modifier is UniversalGameModifier uniMod &&
            (uniMod.CustomAmount <= 0 || uniMod.CustomChance <= 0))
            return false;

        if (options.AssassinGuessAlliances && modifier is AllianceGameModifier)
            return true;

        if (!options.AssassinGuessCrewModifiers)
            return false;

        if (!options.AssassinGuessUtilityModifiers &&
            modifier is TouGameModifier touMod2 &&
            touMod2.FactionType == ModifierFaction.CrewmateUtility)
            return false;

        if (modifier is TouGameModifier crewMod &&
            crewMod.FactionType.ToDisplayString().Contains("Crew") &&
            !crewMod.FactionType.ToDisplayString().Contains("Non"))
            return true;

        return false;
    }

    private static bool IsModifierValidForVigilante(BaseModifier modifier)
    {
        if (modifier is DeathNoteModifier)
            return true;

        if (modifier is VenomousModifier)
            return true;

        if (modifier is ChildModifier)
            return false;

        var isValid =
            !((modifier is TouGameModifier touMod && (touMod.CustomAmount <= 0 || touMod.CustomChance <= 0)) ||
              (modifier is AllianceGameModifier allyMod && (allyMod.CustomAmount <= 0 || allyMod.CustomChance <= 0)) ||
              (modifier is UniversalGameModifier uniMod && (uniMod.CustomAmount <= 0 || uniMod.CustomChance <= 0)));

        if (!isValid)
            return false;

        if (OptionGroupSingleton<VigilanteOptions>.Instance.VigilanteGuessAlliances &&
            modifier is AllianceGameModifier)
        {
            return true;
        }

        if (modifier is TouGameModifier impMod &&
            (impMod.FactionType.ToDisplayString().Contains("Imp") ||
             impMod.FactionType.ToDisplayString().Contains("Killer")) &&
            !impMod.FactionType.ToDisplayString().Contains("Non"))
        {
            return OptionGroupSingleton<VigilanteOptions>.Instance.VigilanteGuessKillerMods;
        }

        return false;
    }

    // =========================
    // EXEMPTIONS
    // =========================
    private static bool IsExempt(PlayerVoteArea voteArea, AssassinModifier assassin)
    {
        var player = MiscUtils.PlayerById(voteArea.TargetPlayerId);
        if (player == null) return true;
        if (voteArea.TargetPlayerId == assassin.Player.PlayerId) return true;
        if (assassin.Player.Data.IsDead) return true;
        if (assassin.Player.HasModifier<TownOfUs.Modifiers.Crewmate.JailedModifier>()) return true;
        if (voteArea.AmDead) return true;
        if (player.Data.IsDead || player.Data.Disconnected) return true;

        var genOptions = OptionGroupSingleton<GeneralOptions>.Instance;

        if (assassin.Player.IsImpostorAligned() && player.IsImpostorAligned() && !genOptions.FFAImpostorMode)
            return true;

        if (assassin.Player.Data.Role is VampireRole &&
            player.Data.Role is VampireRole)
            return true;

        if (IsMayorRevealed(player))
            return true;

        if (player.IsRevealed())
            return true;

        if (IsForestallerRevealed(player))
            return true;

        if (assassin.Player.IsLover() && player.IsLover())
            return true;

        if (player.HasModifier<TownOfUs.Modifiers.Crewmate.JailedModifier>())
            return true;

        if (player.TryGetModifier<ChildModifier>(out var child) && !child.IsAdult)
            return true;

        return false;
    }

    private static bool IsExempt(PlayerVoteArea voteArea, VigilanteRole vigilante)
    {
        var player = voteArea.GetPlayer();

        return voteArea.TargetPlayerId == vigilante.Player.PlayerId ||
               vigilante.Player.Data.IsDead ||
               vigilante.Player.HasModifier<TownOfUs.Modifiers.Crewmate.JailedModifier>() ||
               voteArea.AmDead ||
               player?.HasModifier<TownOfUs.Modifiers.Crewmate.JailedModifier>() == true ||
               (player != null && IsMayorRevealed(player)) ||
               (player != null && player.IsRevealed()) ||
               (player != null && IsForestallerRevealed(player)) ||
               (vigilante.Player.IsLover() && player?.IsLover() == true) ||
               (player != null && player.TryGetModifier<ChildModifier>(out var child) && !child.IsAdult);
    }

    private static bool IsExempt(PlayerVoteArea voteArea, DoomsayerRole doomsayer)
    {
        var player = voteArea.GetPlayer();

        return voteArea.TargetPlayerId == doomsayer.Player.PlayerId ||
               doomsayer.Player.Data.IsDead ||
               doomsayer.Player.HasModifier<TownOfUs.Modifiers.Crewmate.JailedModifier>() ||
               voteArea.AmDead ||
               player?.HasModifier<TownOfUs.Modifiers.Crewmate.JailedModifier>() == true ||
               (player != null && IsMayorRevealed(player)) ||
               (player != null && player.IsRevealed()) ||
               (player != null && IsForestallerRevealed(player)) ||
               (doomsayer.Player.IsLover() && player?.IsLover() == true) ||
               (player != null && player.TryGetModifier<ChildModifier>(out var child) && !child.IsAdult);
    }

    // =========================
    // GUESS LOGIC
    // =========================
    private static void DoGuess(byte targetId, AssassinModifier assassin)
    {
        if (MeetingHud.Instance.state == MeetingHud.VoteStates.Discussion) return;
        if (PlayerControl.LocalPlayer.Data.IsDead) return;
        if (PlayerControl.LocalPlayer.HasModifier<TownOfUs.Modifiers.Crewmate.JailedModifier>()) return;
        if (_guessedThisMeeting && !OptionGroupSingleton<AssassinOptions>.Instance.AssassinMultiKill) return;
        if (_remainingKills <= 0) return;
        if (!GuessIndices.TryGetValue(targetId, out var index) || index < 0) return;

        var entry = _guessableEntries[index];
        var targetPlayer = MiscUtils.PlayerById(targetId);
        if (targetPlayer == null || targetPlayer.Data.IsDead) return;

        PlayerControl victim;

        if (entry.IsModifier)
        {
            var hasModifier = targetPlayer.HasModifier(entry.Modifier!.TypeId);
            victim = hasModifier ? targetPlayer : assassin.Player;
        }
        else
        {
            var targetRole = targetPlayer.Data.Role;
            var pickVictim = entry.Role!.Role == targetRole.Role;

            var cachedMod = targetPlayer.GetModifiers<BaseModifier>()
                .FirstOrDefault(x => x is ICachedRole) as ICachedRole;

            if (cachedMod != null)
            {
                pickVictim = cachedMod.GuessMode switch
                {
                    CacheRoleGuess.ActiveRole => entry.Role.Role == targetRole.Role,
                    CacheRoleGuess.CachedRole => entry.Role.Role == cachedMod.CachedRole.Role,
                    _ => entry.Role.Role == cachedMod.CachedRole.Role || entry.Role.Role == targetRole.Role
                };
            }

            victim = pickVictim ? targetPlayer : assassin.Player;
        }

        if (victim.Data.IsDead || assassin.Player.Data.IsDead) return;

        if (victim != assassin.Player &&
            victim.TryGetModifier<TownOfUs.Modifiers.Crewmate.OracleBlessedModifier>(out var oracleMod))
        {
            OracleRole.RpcOracleBlessNotify(PlayerControl.LocalPlayer, oracleMod.Oracle, victim);
            ShieldUtils.TriggerShieldFlash(PlayerControl.LocalPlayer, ShieldType.Oracle);
            HideSingle(targetId);
            return;
        }

        if (victim == assassin.Player &&
            assassin.Player.TryGetModifier<DoubleShotModifier>(out var dsModifier) &&
            !dsModifier.Used)
        {
            dsModifier.Used = true;
            Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(Color.red));
            return;
        }

        SavedGuessIndices.Remove(targetId);

        assassin.Player.RpcSpecialMurder(
            victim,
            isIndirect: true,
            ignoreShield: true,
            didSucceed: true,
            resetKillTimer: true,
            createDeadBody: false,
            teleportMurderer: false,
            showKillAnim: false,
            playKillSound: false,
            causeOfDeath: victim != assassin.Player ? "Guess" : "Misguess");

        _remainingKills--;
        assassin.maxKills--;

        if (victim == assassin.Player ||
            _remainingKills <= 0 ||
            !OptionGroupSingleton<AssassinOptions>.Instance.AssassinMultiKill)
        {
            HideAllButtons();
            _guessedThisMeeting = true;
        }
        else
        {
            HideSingle(targetId);
        }
    }

    private static void DoGuess(byte targetId, VigilanteRole vigilante)
    {
        if (MeetingHud.Instance.state == MeetingHud.VoteStates.Discussion) return;
        if (PlayerControl.LocalPlayer.Data.IsDead) return;
        if (PlayerControl.LocalPlayer.HasModifier<TownOfUs.Modifiers.Crewmate.JailedModifier>()) return;
        if (_remainingKills <= 0) return;
        if (!GuessIndices.TryGetValue(targetId, out var index) || index < 0) return;

        var entry = _guessableEntries[index];
        var targetPlayer = MiscUtils.PlayerById(targetId);
        if (targetPlayer == null || targetPlayer.Data.IsDead) return;

        PlayerControl victim;

        if (entry.IsModifier)
        {
            victim = targetPlayer.HasModifier(entry.Modifier!.TypeId) ? targetPlayer : vigilante.Player;
        }
        else
        {
            victim = entry.Role!.Role == targetPlayer.Data.Role.Role ? targetPlayer : vigilante.Player;
        }

        if (victim != vigilante.Player &&
            victim.TryGetModifier<TownOfUs.Modifiers.Crewmate.OracleBlessedModifier>(out var oracleMod))
        {
            OracleRole.RpcOracleBlessNotify(PlayerControl.LocalPlayer, oracleMod.Oracle, victim);
            ShieldUtils.TriggerShieldFlash(PlayerControl.LocalPlayer, ShieldType.Oracle);
            HideSingle(targetId);
            return;
        }

        if (victim == vigilante.Player && vigilante.SafeShotsLeft != 0)
        {
            SavedGuessIndices.Remove(targetId);
            vigilante.SafeShotsLeft--;
            Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(Color.red));
            HideAllButtons();
            return;
        }

        SavedGuessIndices.Remove(targetId);

        vigilante.Player.RpcSpecialMurder(
            victim,
            isIndirect: true,
            ignoreShield: true,
            didSucceed: true,
            resetKillTimer: true,
            createDeadBody: false,
            teleportMurderer: false,
            showKillAnim: false,
            playKillSound: false,
            causeOfDeath: victim != vigilante.Player ? "Guess" : "Misguess");

        _remainingKills--;
        vigilante.MaxKills--;

        if (victim == vigilante.Player ||
            _remainingKills <= 0 ||
            !OptionGroupSingleton<VigilanteOptions>.Instance.VigilanteMultiKill)
        {
            HideAllButtons();
        }
        else
        {
            HideSingle(targetId);
        }
    }

    private static void DoGuess(byte targetId, DoomsayerRole doomsayer)
    {
        if (MeetingHud.Instance.state == MeetingHud.VoteStates.Discussion) return;
        if (PlayerControl.LocalPlayer.Data.IsDead) return;
        if (PlayerControl.LocalPlayer.HasModifier<TownOfUs.Modifiers.Crewmate.JailedModifier>()) return;
        if (doomsayer.NumberOfGuesses >= OptionGroupSingleton<DoomsayerOptions>.Instance.DoomsayerGuessesToWin)
        {
            HideAllButtons();
            return;
        }
        if (!GuessIndices.TryGetValue(targetId, out var index) || index < 0) return;

        var entry = _guessableEntries[index];
        var targetPlayer = MiscUtils.PlayerById(targetId);
        if (targetPlayer == null || targetPlayer.Data.IsDead) return;

        bool correctGuess = entry.Role != null && entry.Role.Role == targetPlayer.Data.Role.Role;

        if (!correctGuess)
        {
            SavedGuessIndices.Remove(targetId);
            Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(Color.red));
            HideAllButtons();
            return;
        }

        var victim = targetPlayer;

        if (victim.TryGetModifier<TownOfUs.Modifiers.Crewmate.OracleBlessedModifier>(out var oracleMod))
        {
            OracleRole.RpcOracleBlessNotify(PlayerControl.LocalPlayer, oracleMod.Oracle, victim);
            ShieldUtils.TriggerShieldFlash(PlayerControl.LocalPlayer, ShieldType.Oracle);
            HideSingle(targetId);
            return;
        }

        SavedGuessIndices.Remove(targetId);

        doomsayer.Player.RpcSpecialMurder(
            victim,
            isIndirect: true,
            ignoreShield: true,
            didSucceed: true,
            resetKillTimer: true,
            createDeadBody: false,
            teleportMurderer: false,
            showKillAnim: false,
            playKillSound: false,
            causeOfDeath: "Doomsayer");

        doomsayer.NumberOfGuesses++;

        if (doomsayer.NumberOfGuesses >= OptionGroupSingleton<DoomsayerOptions>.Instance.DoomsayerGuessesToWin)
        {
            HideAllButtons();
        }
        else
        {
            HideSingle(targetId);
        }
    }

    // =========================
    // LIVE REFRESH
    // =========================
    public static void RefreshExemptions()
    {
        if (!IsActive) return;
        if (MeetingHud.Instance == null) return;
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return;

        foreach (var targetId in Buttons.Keys.ToList())
        {
            var voteArea = MeetingHud.Instance.playerStates?.FirstOrDefault(x => x.TargetPlayerId == targetId);
            if (voteArea == null)
            {
                HideSingle(targetId);
                continue;
            }

            if (PlayerControl.LocalPlayer.TryGetModifier<AssassinModifier>(out var assassin))
            {
                if (IsExempt(voteArea, assassin))
                {
                    HideSingle(targetId);
                }

                continue;
            }

            if (PlayerControl.LocalPlayer.Data.Role is VigilanteRole vigilante)
            {
                if (IsExempt(voteArea, vigilante))
                {
                    HideSingle(targetId);
                }

                continue;
            }

            if (PlayerControl.LocalPlayer.Data.Role is DoomsayerRole doomsayer)
            {
                if (IsExempt(voteArea, doomsayer))
                {
                    HideSingle(targetId);
                }
            }
        }
    }

    // =========================
    // HIDE
    // =========================
    public static void HideForPlayer(byte targetId)
    {
        HideSingle(targetId);
    }

    public static void HideAllButtons()
    {
        SaveCurrentGuesses();

        foreach (var targetId in Buttons.Keys.ToList())
        {
            HideSingle(targetId);
        }
    }

    private static void HideSingle(byte targetId)
    {
        if (!Buttons.TryGetValue(targetId, out var tuple)) return;

        if (GuessIndices.TryGetValue(targetId, out var idx) && idx >= 0)
        {
            SavedGuessIndices[targetId] = idx;
        }

        var (cycleBack, cycleForward, guess, guessText) = tuple;

        if (cycleBack != null) cycleBack.SetActive(false);
        if (cycleForward != null) cycleForward.SetActive(false);
        if (guess != null) guess.SetActive(false);
        if (guessText != null) guessText.gameObject.SetActive(false);

        Buttons.Remove(targetId);
        GuessIndices.Remove(targetId);

        var voteArea = MeetingHud.Instance?.playerStates?.FirstOrDefault(x => x.TargetPlayerId == targetId);
        if (voteArea != null)
        {
            voteArea.NameText.transform.localPosition = new Vector3(0.3384f, 0.0311f, -0.1f);
        }
    }
}

