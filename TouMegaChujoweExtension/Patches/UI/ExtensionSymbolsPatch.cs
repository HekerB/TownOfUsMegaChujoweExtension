using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Modules;
using TownOfUs.Options;
using TownOfUs.Roles.Neutral;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TownOfUs;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.UI;

[HarmonyPatch(typeof(PlayerRoleTextExtensions))]
public static class ExtensionSymbolsPatch
{
    [HarmonyPatch(nameof(PlayerRoleTextExtensions.UpdateTargetSymbols), typeof(string), typeof(PlayerControl), typeof(bool))]
    [HarmonyPostfix]
    public static void UpdateTargetSymbolsPostfix(ref string __result, PlayerControl player, bool hidden)
    {
        var local = PlayerControl.LocalPlayer;
        if (player == null || local == null || local.Data == null)
            return;

        var genOpt = OptionGroupSingleton<TownOfUs.Options.GeneralOptions>.Instance;
        bool isGhost = local.HasDied();
        bool deadKnow = isGhost && genOpt.TheDeadKnow && !hidden;

        // --- POPE (Θ) ---
        if (player.HasModifier<PopeCanonizedModifier>())
        {
            if (!__result.Contains("Θ"))
            {
                bool isPope = local.Data.Role is PopeRole;
                if (isPope || deadKnow)
                {
                    __result += " <color=#FFD700>Θ</color>";
                }
            }
        }

        // --- DEATH NOTE (♡) ---
        if (TryGetDeathNoteTarget(out var dnTarget) && dnTarget != null && dnTarget.PlayerId == player.PlayerId)
        {
            if (!__result.Contains("♡"))
            {
                bool hasDeathNote = local.HasModifier<DeathNoteModifier>();
                if (hasDeathNote || deadKnow)
                {
                    __result += " <color=#8B00FF>♡</color>";
                }
            }
        }

        // --- SHROUD (♢) ---
        if (player.TryGetModifier<ShroudedModifier>(out var shroudMod))
        {
            if (!__result.Contains("♢"))
            {
                bool isShroud = local.IsRole<ShroudRole>() && shroudMod.ShroudOwnerId == local.PlayerId;
                if (isShroud || deadKnow)
                {
                    __result += " <color=#6699FF>♢</color>";
                }
            }
        }

        // --- WITCH (Removed gray symbol) ---

        // --- LAWYER (§) ---
        if (!__result.Contains("§"))
        {
            bool isClientOfLocal = LawyerUtils.IsClientOfLawyer(player, local.PlayerId);
            bool isLawyer = local.IsRole<LawyerRole>() && isClientOfLocal;
            bool isClientPair = LawyerUtils.IsClientOfAnyLawyer(local) && LawyerUtils.IsClientOfLawyer(local, player.PlayerId);
            bool isAnyClient = LawyerUtils.IsClientOfAnyLawyer(player);

            if ((isLawyer || isClientPair || (deadKnow && isAnyClient)))
            {
                __result += " <color=#EDB38C>§</color>";
            }
        }

        // --- BODYGUARD (Σ) ---
        if (!__result.Contains("Σ"))
        {
            bool isProtectedByLocal = player.TryGetModifier<BodyguardShieldModifier>(out var bgMod) && 
                                     bgMod != null && bgMod.Bodyguard != null && bgMod.Bodyguard.PlayerId == local.PlayerId;
            bool isBodyguard = local.IsRole<BodyguardRole>();
            bool isAnyProtected = player.HasModifier<BodyguardShieldModifier>();

            if ((isBodyguard && isProtectedByLocal) || (deadKnow && isAnyProtected))
            {
                __result += " <color=#003380>Σ</color>";
            }
        }
    }

    [HarmonyPatch(nameof(PlayerRoleTextExtensions.UpdateProtectionSymbols), typeof(string), typeof(PlayerControl), typeof(bool))]
    [HarmonyPostfix]
    public static void UpdateProtectionSymbolsPostfix(ref string __result, PlayerControl player, bool hidden)
    {
        var local = PlayerControl.LocalPlayer;
        if (player == null || local == null || local.Data == null)
            return;

        var genOpt = OptionGroupSingleton<TownOfUs.Options.GeneralOptions>.Instance;
        bool deadKnow = local.HasDied() && genOpt.TheDeadKnow && !hidden;

        // --- POISONER (%) ---
        if (PoisonSystem.IsTargetPoisonedByPoison(player.PlayerId))
        {
            if (!__result.Contains("%"))
            {
                if (local.IsImpostorAligned() || deadKnow)
                {
                    __result += " <color=#00FF00>%</color>";
                }
            }
        }

        // --- BODYGUARD (Σ) ---
        if (player.TryGetModifier<BodyguardShieldModifier>(out var shieldMod))
        {
            if (!__result.Contains("Σ"))
            {
                if (shieldMod.VisibleSymbol || deadKnow)
                {
                    __result += " <color=#003380>Σ</color>";
                }
            }
        }
    }

    private static bool TryGetDeathNoteTarget(out PlayerControl? target)
    {
        target = null;
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc.TryGetModifier<DeathNoteModifier>(out var dnMod))
            {
                target = dnMod.CursedTarget;
                return target != null;
            }
        }
        return false;
    }
}























