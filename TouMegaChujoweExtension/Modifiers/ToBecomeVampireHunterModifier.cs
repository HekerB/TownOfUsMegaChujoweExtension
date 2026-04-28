using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using Reactor.Utilities;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Modifiers.Game.Alliance;
using TownOfUs.Roles;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using UnityEngine;
using AmongUs.GameOptions;
using Random = System.Random;

namespace TouMegaChujoweExtension.Modifiers.Crewmate;

internal sealed class ToBecomeVampireHunterModifier : ExcludedGameModifier
{
    public override string ModifierName => "Possible Vampire Hunter";
    public override bool HideOnUi => true;
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

    private static bool TryGetBoolByName(object obj, string propOrFieldName, out bool value)
    {
        value = false;
        if (obj == null) return false;

        var t = obj.GetType();

        var p = t.GetProperty(propOrFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (p != null && p.PropertyType == typeof(bool))
        {
            value = (bool)p.GetValue(obj);
            return true;
        }

        var f = t.GetField(propOrFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null && f.FieldType == typeof(bool))
        {
            value = (bool)f.GetValue(obj);
            return true;
        }

        return false;
    }

    private static bool TryGetEnumStringByName(object obj, string propOrFieldName, out string? enumString)
    {
        enumString = null;
        if (obj == null) return false;

        var t = obj.GetType();

        var p = t.GetProperty(propOrFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (p != null && p.PropertyType.IsEnum)
        {
            var v = p.GetValue(obj);
            enumString = v?.ToString();
            return enumString != null;
        }

        var f = t.GetField(propOrFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null && f.FieldType.IsEnum)
        {
            var v = f.GetValue(obj);
            enumString = v?.ToString();
            return enumString != null;
        }

        return false;
    }

    private static bool IsCrewPowerOrProtective(RoleBehaviour role)
    {
        if (role == null) return false;

        if (role is ITouCrewRole crewRole)
        {
            if (crewRole.IsPowerCrew) return true;
            if (crewRole.RoleAlignment == RoleAlignment.CrewmateProtective) return true;
            return false;
        }

        if (TryGetBoolByName(role, "IsPowerCrew", out var isPower) && isPower)
            return true;

        string? align;
        if (!TryGetEnumStringByName(role, "RoleAlignment", out align))
            TryGetEnumStringByName(role, "Alignment", out align);

        if (!string.IsNullOrEmpty(align))
        {
            if (align.Equals("CrewmateProtective", StringComparison.OrdinalIgnoreCase)) return true;
            if (align.Contains("Protective", StringComparison.OrdinalIgnoreCase)) return true;
            if (align.Contains("Power", StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }

    private static bool HasModifierLike(PlayerControl player, string token)
    {
        if (player == null) return false;

        try
        {
            var comp = player.GetComponent<ModifierComponent>();
            if (comp == null) return false;

            object? listObj = null;

            var t = comp.GetType();
            var p = t.GetProperty("Modifiers", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? t.GetProperty("ActiveModifiers", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (p != null)
                listObj = p.GetValue(comp);
            else
            {
                var f = t.GetField("Modifiers", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        ?? t.GetField("ActiveModifiers", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null) listObj = f.GetValue(comp);
            }

            if (listObj is not IEnumerable enumerable) return false;

            foreach (var m in enumerable)
            {
                if (m == null) continue;

                var name = m.GetType().Name ?? string.Empty;
                if (name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                var mnProp = m.GetType().GetProperty("ModifierName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var mn = mnProp?.GetValue(m)?.ToString() ?? string.Empty;
                if (mn.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
        }
        catch { }

        return false;
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
