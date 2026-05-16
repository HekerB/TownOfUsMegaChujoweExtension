using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Random = System.Random;
using Reactor.Networking.Attributes;
using Reactor.Utilities;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using TownOfUs.Extensions;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Modifiers.Game.Alliance;
using TownOfUs.Modifiers;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles.Neutral;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Crewmate;

internal sealed class ToBecomeVampireHunterModifier : ExcludedGameModifier
{
    public override string ModifierName => "Possible Vampire Hunter";
    public override bool HideOnUi => true;
    public override int GetAmountPerGame() => 0;
    public override int GetAssignmentChance() => 0;
    private static int _vampireHuntersSpawned;
    private static bool _vhHasDied;
    private static bool _assignmentScheduled;

    public static void ResetGame()
    {
        _vampireHuntersSpawned = 0;
        _vhHasDied = false;
        _assignmentScheduled = false;
    }

    private static int GetMaxVampireHunters()
    {
        var roleBehaviour = RoleManager.Instance.GetRole((RoleTypes)RoleId.Get<VampireHunterRole>());
        if (roleBehaviour is not ICustomRole customRole) return 0;
        return (int)customRole.GetCount()!;
    }

    private static bool IsVampireHunterEnabled()
    {
        var roleBehaviour = RoleManager.Instance.GetRole((RoleTypes)RoleId.Get<VampireHunterRole>());
        if (roleBehaviour is not ICustomRole customRole) return false;
        return (int)customRole.GetCount()! > 0 && (int)customRole.GetChance()! > 0;
    }

    private static int GetVampireHunterChance()
    {
        var roleBehaviour = RoleManager.Instance.GetRole((RoleTypes)RoleId.Get<VampireHunterRole>());
        if (roleBehaviour is not ICustomRole customRole) return 0;
        return (int)customRole.GetChance()!;
    }




    private static bool IsCrewPowerOrProtective(RoleBehaviour role)
    {
        if (role == null) return false;

        if (role is ITouCrewRole crewRole)
        {
            if (crewRole.IsPowerCrew) return true;
            if (crewRole.RoleAlignment == RoleAlignment.CrewmateProtective) return true;
        }

        var alignment = role.GetRoleAlignment();
        if (alignment == RoleAlignment.CrewmateProtective) return true;

        var alignStr = alignment.ToString();
        return alignStr.Contains("Protective", StringComparison.OrdinalIgnoreCase) ||
               alignStr.Contains("Power", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasModifierLike(PlayerControl player, string token)
    {
        if (player == null) return false;

        var modifiers = player.GetModifiers<BaseModifier>();
        if (modifiers == null) return false;

        return modifiers.Any(m =>
            (m.GetType().Name?.Contains(token, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (m.ModifierName?.Contains(token, StringComparison.OrdinalIgnoreCase) ?? false)
        );
    }

    private static bool IsEligible(PlayerControl player)
    {
        if (player == null || player.Data == null) return false;
        if (player.Data.Disconnected) return false;
        var role = player.Data.Role;
        if (role == null) return false;
        if (role.IsImpostor) return false;
        if (role is NeutralRole) return false;
        if (IsCrewPowerOrProtective(role)) return false;
        if (player.HasModifier<EgotistModifier>() || HasModifierLike(player, "Egotist")) return false;
        if (player.HasModifier<CrewpostorModifier>() || HasModifierLike(player, "Crewpostor")) return false;
        if (player.HasModifier<ToBecomeTraitorModifier>() || HasModifierLike(player, "Traitor")) return false;
        if (player.Data.Role is ImitatorRole || HasModifierLike(player, "Imitator")) return false;

        return true;
    }

    public static void TryAssignAtGameStart()
    {
        if (!PlayerControl.LocalPlayer.IsHost()) return;
        if (!IsVampireHunterEnabled()) return;

        ResetGame();

        Random rnd = new();
        var chance = GetVampireHunterChance();
        if (chance <= 0) return;
        if (rnd.Next(1, 101) > chance) return;

        if (_assignmentScheduled) return;
        _assignmentScheduled = true;

        Coroutines.Start(CoAssignAfterRolesReady());
    }

    private static IEnumerator CoAssignAfterRolesReady()
    {
        float timeout = 10f;
        while (timeout > 0f)
        {
            timeout -= Time.deltaTime;

            var local = PlayerControl.LocalPlayer;
            if (local != null && local.Data != null && local.moveable && ShipStatus.Instance != null)
                break;

            yield return null;
        }

        yield return new WaitForSeconds(0.35f);

        if (!PlayerControl.LocalPlayer.IsHost()) yield break;
        if (_vhHasDied) yield break;

        var candidates = PlayerControl.AllPlayerControls.ToArray()
            .Where(x => x != null && !x.HasDied() && IsEligible(x))
            .ToList();

        foreach (var c in candidates)
            c.RpcAddModifier<ToBecomeVampireHunterModifier>();
    }

    public static void TrySpawnAfterMeeting()
    {
        if (!PlayerControl.LocalPlayer.IsHost()) return;
        if (_vhHasDied) return;

        var maxVh = GetMaxVampireHunters();

        var livingVhCount = PlayerControl.AllPlayerControls.ToArray()
            .Count(x => !x.HasDied() && x.Data.Role is VampireHunterRole);
        if (livingVhCount >= maxVh) return;
        if (_vampireHuntersSpawned >= maxVh) return;

        var livingVampires = PlayerControl.AllPlayerControls.ToArray()
            .Count(x => !x.HasDied() && x.Data.Role is VampireRole);
        var minRequired = (int)OptionGroupSingleton<VampireHunterOptions>.Instance.MinVampiresForSpawn;
        if (livingVampires < minRequired) return;

        var candidates = ModifierUtils.GetActiveModifiers<ToBecomeVampireHunterModifier>()
            .Where(x => !x.Player.HasDied() && IsEligible(x.Player))
            .ToList();

        if (candidates.Count == 0) return;

        Random rnd = new();
        var chosen = candidates[rnd.Next(0, candidates.Count)];

        RpcSetVampireHunter(chosen.Player);
    }

    public static void OnVampireHunterDied()
    {
        _vhHasDied = true;
        RemoveAllModifiers();
    }

    private static void RemoveAllModifiers()
    {
        if (!PlayerControl.LocalPlayer.IsHost()) return;

        var remaining = ModifierUtils.GetActiveModifiers<ToBecomeVampireHunterModifier>().ToList();
        foreach (var mod in remaining)
            mod.Player.RpcRemoveModifier<ToBecomeVampireHunterModifier>();
    }

    [MethodRpc((uint)ExtensionRpc.SetVampireHunter)]
    public static void RpcSetVampireHunter(PlayerControl player)
    {
        player.ChangeRole(RoleId.Get<VampireHunterRole>());

        if (player.HasModifier<ToBecomeVampireHunterModifier>())
            player.RemoveModifier<ToBecomeVampireHunterModifier>();

        _vampireHuntersSpawned++;

        if (_vampireHuntersSpawned >= GetMaxVampireHunters())
            RemoveAllModifiers();
    }
}