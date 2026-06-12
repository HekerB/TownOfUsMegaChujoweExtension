using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using TownOfUs.Extensions;
using TownOfUs.Roles;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;

namespace TouMegaChujoweExtension.Utilities;

public static class AgentUtils
{
    private static readonly Dictionary<byte, RoleTypes> FakeCrewRoles = [];

    public static bool AppearsAsCrewpostor
    {
        get
        {
            try
            {
                return OptionGroupSingleton<AgentOptions>.Instance.AppearsAs.Value == AgentAppearance.Crewpostor;
            }
            catch
            {
                return true;
            }
        }
    }

    public static bool CanUseImpostorChat
    {
        get
        {
            try
            {
                return OptionGroupSingleton<AgentOptions>.Instance.CanUseImpostorChat;
            }
            catch
            {
                return true;
            }
        }
    }

    public static bool AgentCanSpawn()
    {
        try
        {
            var roleType = (RoleTypes)RoleId.Get<AgentRole>();
            var roleOptions = GameOptionsManager.Instance.CurrentGameOptions.RoleOptions;
            return roleOptions.GetNumPerGame(roleType) > 0 && roleOptions.GetChancePerGame(roleType) > 0;
        }
        catch
        {
            return false;
        }
    }

    public static string GetAnonymousImpostorChatTitle()
    {
        return GetAnonymousImpostorChatTitle(null);
    }

    public static string GetAnonymousImpostorChatTitle(PlayerControl? sender)
    {
        return TouLocale.GetParsed("ImpostorChatTitle").Replace("<player>", GetAnonymousImpostorAlias(sender));
    }

    public static string GetAnonymousImpostorAlias(PlayerControl? sender)
    {
        var impostor = TouLocale.Get("ImpostorKeyword", "Impostor");
        if (sender == null)
        {
            return impostor;
        }

        var chatMembers = PlayerControl.AllPlayerControls.ToArray()
            .Where(player => player?.Data != null &&
                             !player.Data.Disconnected &&
                             (player.IsImpostorAligned() || player.IsRole<AgentRole>()))
            .OrderBy(player => player.PlayerId)
            .ToList();

        var index = chatMembers.FindIndex(player => player.PlayerId == sender.PlayerId);
        return index < 0 ? impostor : $"{impostor} {index + 1}";
    }

    public static void ResetFakeRoles()
    {
        FakeCrewRoles.Clear();
    }

    public static RoleBehaviour? GetFakeCrewRole(PlayerControl agent)
    {
        if (agent?.Data?.Role == null)
        {
            return null;
        }

        if (FakeCrewRoles.TryGetValue(agent.PlayerId, out var cachedRoleType))
        {
            return RoleManager.Instance.GetRole(cachedRoleType);
        }

        var fakeRole = PickFakeCrewRole(agent);
        if (fakeRole != null)
        {
            FakeCrewRoles[agent.PlayerId] = fakeRole.Role;
        }

        return fakeRole;
    }

    public static string GetFakeCrewpostorRoleText(PlayerControl agent)
    {
        var fakeRole = GetFakeCrewRole(agent);
        var roleColor = fakeRole?.TeamColor ?? Palette.CrewmateBlue;
        var roleName = fakeRole?.GetRoleName() ?? TouLocale.Get("RoleCrewmate", "Crewmate");
        var result = $"<size=80%>{roleColor.ToTextColor()}{roleName}</color></size>";

        if (AppearsAsCrewpostor)
        {
            var crewpostorName = TouLocale.Get("TouModifierCrewpostorShortName", "Imp");
            result += $"<size=80%><color=#FFFFFF> (<color=#D64042>{crewpostorName}</color>)</color></size>";
        }

        return result;
    }

    private static RoleBehaviour? PickFakeCrewRole(PlayerControl agent)
    {
        var usedRoles = PlayerControl.AllPlayerControls.ToArray()
            .Where(player => player?.Data?.Role != null)
            .Select(player => player.Data.Role.Role)
            .ToHashSet();

        var agentRoleType = (RoleTypes)RoleId.Get<AgentRole>();

        var candidates = MiscUtils.GetRegisteredRoles(ModdedRoleTeams.Crewmate)
            .Where(role => role != null &&
                           !role.IsDead &&
                           role.Role != agentRoleType &&
                           role.Role != RoleTypes.Crewmate &&
                           !usedRoles.Contains(role.Role) &&
                           CustomRoleUtils.CanSpawnOnCurrentMode(role))
            .GroupBy(role => role.Role)
            .Select(group => group.First())
            .OrderBy(role => (ushort)role.Role)
            .ToList();

        if (candidates.Count == 0)
        {
            candidates = MiscUtils.GetRegisteredRoles(ModdedRoleTeams.Crewmate)
                .Where(role => role != null &&
                               !role.IsDead &&
                               role.Role != agentRoleType &&
                               !usedRoles.Contains(role.Role) &&
                               CustomRoleUtils.CanSpawnOnCurrentMode(role))
                .GroupBy(role => role.Role)
                .Select(group => group.First())
                .OrderBy(role => (ushort)role.Role)
                .ToList();
        }

        if (candidates.Count == 0)
        {
            return RoleManager.Instance.GetRole(RoleTypes.Crewmate);
        }

        var index = Math.Abs(GetStableAgentSeed(agent)) % candidates.Count;
        return candidates[index];
    }

    private static int GetStableAgentSeed(PlayerControl agent)
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + agent.PlayerId;

            foreach (var player in PlayerControl.AllPlayerControls.ToArray()
                         .Where(player => player?.Data != null)
                         .OrderBy(player => player.PlayerId))
            {
                hash = hash * 31 + player.PlayerId;
                hash = hash * 31 + player.Data.DefaultOutfit.ColorId;

                var name = player.Data.PlayerName ?? string.Empty;
                foreach (var c in name)
                {
                    hash = hash * 31 + c;
                }
            }

            return hash == int.MinValue ? 0 : hash;
        }
    }
}
