using MiraAPI.GameOptions;
using MiraAPI.LocalSettings;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using Object = UnityEngine.Object;
using Reactor.Utilities.Extensions;
using System.Reflection;
using TMPro;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Modifiers.Game;
using TownOfUs.Options.Roles.Crewmate;
using TownOfUs.Options.Roles.Neutral;
using TownOfUs.Options;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles.Neutral;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine.UI;
using UnityEngine;

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

    private static int _lastAliveCount = -1;
    private static readonly Dictionary<System.Type, FieldInfo> _screenFields = new();
    private static readonly Dictionary<System.Type, MethodInfo[]> _refreshMethods = new();

    private sealed class GuessEntry
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
        _lastAliveCount = -1;
    }

    public static void FullReset()
    {
        _lastAliveCount = -1;
        _screenFields.Clear();
        _refreshMethods.Clear();
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

        var assassinOptions = OptionGroupSingleton<AssassinOptions>.Instance;
        var allPossibleModifiers = GetGuessableModifiersWithDynamicSupport(MiscUtils.AllModifiers);

        var modifiers = allPossibleModifiers
            .Where(m =>
            {
                if (m is DeathNoteModifier or VenomousModifier) return true;
                if (!assassinOptions.AssassinGuessCrewModifiers && !assassinOptions.AssassinGuessAlliances) return false;
                return IsModifierValid(m, assassinOptions);
            })
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

        var vigilanteOptions = OptionGroupSingleton<VigilanteOptions>.Instance;
        var allPossibleModifiers = GetGuessableModifiersWithDynamicSupport(MiscUtils.AllModifiers);

        var modifiers = allPossibleModifiers
            .Where(m =>
            {
                if (m is DeathNoteModifier or VenomousModifier) return true;
                if (!vigilanteOptions.VigilanteGuessAlliances && !vigilanteOptions.VigilanteGuessKillerMods) return false;
                return IsModifierValidForVigilante(m);
            })
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

    private static List<BaseModifier> GetGuessableModifiersWithDynamicSupport(IEnumerable<BaseModifier> baseModifiers)
    {
        var modifiers = baseModifiers.ToList();

        // 1. Player-based discovery (active instances)
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc == null || pc.Data == null) continue;
            var playerModifiers = pc.GetModifiers<BaseModifier>();
            if (playerModifiers == null) continue;

            foreach (var mod in playerModifiers)
            {
                if (mod == null) continue;
                if (mod is IGuessable && !modifiers.Any(m => m.GetType() == mod.GetType()))
                {
                    modifiers.Add(mod);
                }
            }
        }

        // 2. Prototype discovery (for unassigned modifiers)
        // Since these are extension modifiers (C# classes), we can safely instantiate them
        // with IntPtr.Zero to use as prototypes for their names and colors.
        try
        {
            var extensionTypes = new[] 
            { 
                typeof(TouMegaChujoweExtension.Modifiers.Neutral.DeathNoteModifier),
                typeof(TouMegaChujoweExtension.Modifiers.Neutral.VenomousModifier),
                typeof(TouMegaChujoweExtension.Modifiers.Crewmate.PublicityModifier)
            };

            foreach (var type in extensionTypes)
            {
                if (!modifiers.Any(m => m.GetType() == type))
                {
                    try
                    {
                        // Use the IntPtr constructor if it exists (standard for Il2Cpp-wrapped types)
                        var prototype = (BaseModifier)System.Activator.CreateInstance(type, new object[] { System.IntPtr.Zero });
                        if (prototype != null)
                        {
                            modifiers.Add(prototype);
                        }
                    }
                    catch
                    {
                        try
                        {
                            // Fallback to parameterless constructor if it's a pure C# class
                            var prototype = (BaseModifier)System.Activator.CreateInstance(type);
                            if (prototype != null)
                            {
                                modifiers.Add(prototype);
                            }
                        }
                        catch { /* fallback to next instantiator */ }
                    }
                }
            }
        }
        catch { /* ignore dynamic discovery errors */ }

        return modifiers;
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
        if (role is IGuessable { CanBeGuessed: false }) return false;
        if (role is TownOfUs.Roles.Impostor.TraitorRole && assassin.Player.IsImpostorAligned()) return false;

        var options = OptionGroupSingleton<AssassinOptions>.Instance;
        var alignment = role.GetRoleAlignment();

        if (alignment == RoleAlignment.GameOutlier) return false;

        if (alignment == RoleAlignment.CrewmateInvestigative)
            return options.AssassinGuessInvest;

        if (role.IsCrewmate() && (role is MiraAPI.Roles.ICustomRole || role is TownOfUs.Roles.ITownOfUsRole))
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
        if (role is IGuessable { CanBeGuessed: false }) return false;

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

        var genOptions = OptionGroupSingleton<TownOfUs.Options.GeneralOptions>.Instance;

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
        if (MeetingHud.Instance == null) return;
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return;

        // Optimization: Only run refresh logic if someone died or disconnected
        int currentAlive = 0;
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p != null && p.Data != null && !p.Data.IsDead && !p.Data.Disconnected)
                currentAlive++;
        }

        if (currentAlive == _lastAliveCount) return;
        _lastAliveCount = currentAlive;

        // Force meeting UI sync for any newly dead players
        // This ensures mid-meeting deaths (e.g. Death Note, Assassin) are visually reflected
        foreach (var pva in MeetingHud.Instance.playerStates)
        {
            if (pva == null) continue;
            var pc = pva.GetPlayer();
            if (pc != null && pc.Data != null && (pc.Data.IsDead || pc.Data.Disconnected) && !pva.AmDead)
            {
                pva.AmDead = true;
                if (pva.Overlay != null) pva.Overlay.gameObject.SetActive(true);
                if (pva.XMark != null) pva.XMark.gameObject.SetActive(true);
            }
        }

        RefreshBaseModButtons();

        if (IsActive)
        {
            var local = PlayerControl.LocalPlayer;
            var roleName = local.Data?.Role?.GetType().Name ?? "";
            var isGuesser = local.TryGetModifier<AssassinModifier>(out _) ||
                            local.Data.Role is VigilanteRole ||
                            local.Data.Role is DoomsayerRole ||
                            local.Data.Role is JailorRole ||
                            roleName.Contains("Imitator");

            if (!isGuesser)
            {
                HideAllButtons();
                return;
            }

            foreach (var targetId in Buttons.Keys.ToList())
            {
                var voteArea = MeetingHud.Instance.playerStates?.FirstOrDefault(x => x.TargetPlayerId == targetId);
                if (voteArea == null)
                {
                    HideSingle(targetId);
                    continue;
                }

                if (local.TryGetModifier<AssassinModifier>(out var assassin))
                {
                    if (IsExempt(voteArea, assassin))
                    {
                        HideSingle(targetId);
                    }
                    continue;
                }

                if (local.Data.Role is VigilanteRole vigilante)
                {
                    if (IsExempt(voteArea, vigilante))
                    {
                        HideSingle(targetId);
                    }
                    continue;
                }

                if (local.Data.Role is DoomsayerRole doomsayer)
                {
                    if (IsExempt(voteArea, doomsayer))
                    {
                        HideSingle(targetId);
                    }

                }
                
                // If we reach here, it means they are a guesser (e.g. Jailor) but we don't have custom logic for them yet
                // For now, we'll keep them shown unless we add more checks
            }
        }
        else
        {
            TryRefreshTablet();
        }
    }

    public static void TryRefreshTablet()
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            if (local == null) return;

            object? screen = null;
            var roleName = local.Data?.Role?.GetType().Name ?? "";
            var isGuesser = local.TryGetModifier<AssassinModifier>(out _) ||
                            local.Data.Role is VigilanteRole ||
                            local.Data.Role is DoomsayerRole ||
                            local.Data.Role is JailorRole ||
                            roleName.Contains("Imitator");

            // Find active guessing screen using cached fields
            System.Type? type = null;
            object? target = null;

            if (local.TryGetModifier<AssassinModifier>(out var assassin))
            {
                type = assassin.GetType();
                target = assassin;
            }
            else if (local.Data?.Role != null)
            {
                type = local.Data.Role.GetType();
                target = local.Data.Role;
            }

            if (type != null && target != null)
            {
                if (!_screenFields.TryGetValue(type, out var field))
                {
                    field = type.GetField("guessingScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null) _screenFields[type] = field;
                }
                screen = _screenFields.TryGetValue(type, out var f) ? f.GetValue(target) : null;
            }

            if (screen != null)
            {
                var screenType = screen.GetType();

                // If the player is no longer a guesser, close the tablet
                if (!isGuesser)
                {
                    screenType.GetMethod("Close", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?.Invoke(screen, null);
                    return;
                }

                // Force an update of the guessing screen using cached methods
                if (!_refreshMethods.TryGetValue(screenType, out var methods))
                {
                    var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                    var list = new List<MethodInfo>();
                    string[] names = { "UpdatePlayers", "UpdateButtons", "Update", "OnEnable" };
                    foreach (var name in names)
                    {
                        var m = screenType.GetMethod(name, flags);
                        if (m != null) list.Add(m);
                    }
                    methods = list.ToArray();
                    _refreshMethods[screenType] = methods;
                }

                foreach (var m in methods)
                {
                    m.Invoke(screen, null);
                }
            }
        }
        catch { /* Ignore reflection errors */ }
    }

    public static void RefreshBaseModButtons()
    {
        try
        {
            // TownOfUs.Modules.MeetingMenu.Instances stores all active guessing menus (Assassin, Vigilante, etc.)
            foreach (var menu in TownOfUs.Modules.MeetingMenu.Instances)
            {
                if (menu == null) continue;

                // Check all buttons currently managed by this menu
                foreach (var targetId in menu.Buttons.Keys.ToList())
                {
                    var voteArea = TownOfUs.Modules.MeetingMenu.Instances.Count > 0 ? MeetingHud.Instance?.playerStates?.FirstOrDefault(x => x.TargetPlayerId == targetId) : null;
                    if (voteArea == null)
                    {
                        menu.HideSingle(targetId);
                        continue;
                    }

                    // Evaluate the actual exemption rules of the menu
                    if (menu.IsExempt != null && menu.IsExempt(voteArea))
                    {
                        menu.HideSingle(targetId);
                    }
                }
            }
        }
        catch { /* Ignore errors from base mod interactions */ }
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



























