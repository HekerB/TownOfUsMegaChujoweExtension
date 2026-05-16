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
using TownOfUs.Extensions;
using UnityEngine;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TouMegaChujoweExtension.Modifiers.Neutral;

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
        if (player.HasModifier<PopeCanonizedModifier>() && !__result.Contains('Θ') && (local.Data.Role is PopeRole || deadKnow))
        {
            __result += " <color=#FFD700>Θ</color>";
        }

        // --- DEATH NOTE (♡) ---
        if (TryGetDeathNoteTarget(out var dnTarget) && dnTarget != null && dnTarget.PlayerId == player.PlayerId && !__result.Contains('♡') && (local.HasModifier<DeathNoteModifier>() || deadKnow))
        {
            __result += " <color=#8B00FF>♡</color>";
        }

        // --- SHROUD (♢) ---
        if (player.TryGetModifier<ShroudedModifier>(out var shroudMod) && !__result.Contains('♢') && (local.IsRole<ShroudRole>() && shroudMod.ShroudOwnerId == local.PlayerId || deadKnow))
        {
            __result += " <color=#6699FF>♢</color>";
        }

        // --- WITCH (Removed gray symbol) ---

        // --- LAWYER (§) ---
        if (!__result.Contains('§'))
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
        if (!__result.Contains('Σ'))
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

        // --- DETONATOR (λ) ---
        if (DetonatorSystem.IsBombTarget(player.PlayerId) && !__result.Contains('λ') && (local.IsImpostorAligned() || deadKnow))
        {
            __result += " <color=#FF0000>λ</color>";
        }

        // --- JACKAL / SIDEKICK (RECRUIT) ---
        var jackalHex = ColorUtility.ToHtmlStringRGB(TouExtensionColors.Jackal);
        if (player.TryGetModifier<SidekickModifier>(out var mod))
        {
            bool localIsJackal = local.IsRole<JackalRole>();
            bool localIsSidekick = local.TryGetModifier<SidekickModifier>(out var localMod);
            
            bool sameTeam = (localIsJackal && local.PlayerId == mod.JackalId) || 
                           (localIsSidekick && localMod.JackalId == mod.JackalId);
            
            bool canSeeTeam = sameTeam || deadKnow;

            if (canSeeTeam)
            {
                string prefix = "";
                
                // Jackal always sees roles. Sidekicks see roles ONLY if the setting is enabled.
                bool canSeeRole = localIsJackal || (localIsSidekick && OptionGroupSingleton<TouMegaChujoweExtension.Options.ExtensionGeneralOptions>.Instance.RecruitsKnowEachOther);

                if (canSeeRole && player.PlayerId != local.PlayerId && !deadKnow && player.Data?.Role != null)
                {
                    var role = RoleManager.Instance?.GetRole(player.Data.Role.Role);
                    var roleColor = (role as ITownOfUsRole)?.RoleColor ?? (role as ICustomRole)?.RoleColor ?? Color.white;
                    string colorHex = ColorUtility.ToHtmlStringRGB(roleColor);
                    
                    string roleName = role is ITownOfUsRole touRole ? touRole.RoleName : (role as ICustomRole)?.RoleName ?? role.Role.ToString();
                    if (!__result.Contains(roleName))
                    {
                        prefix = $"<size=80%><color=#{colorHex}>{roleName}</color></size>\n";
                    }
                }

                if (!__result.Contains("(Rec)"))
                {
                    __result = $"{prefix}<color=#{jackalHex}>{__result} (Rec)</color>";
                }
            }
        }
        else if (player.IsRole<JackalRole>())
        {
            // Only Jackal sees themselves as Jackal (color-wise)
            // sidekicks do NOT see the jackal in color to keep the leader anonymous
            if (local.PlayerId == player.PlayerId || deadKnow)
            {
                if (!__result.Contains(jackalHex))
                {
                    __result = $"<color=#{jackalHex}>{__result}</color>";
                }
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
        if (PoisonSystem.IsTargetPoisonedByPoison(player.PlayerId) && !__result.Contains('%') && (local.IsImpostorAligned() || deadKnow))
        {
            __result += " <color=#00FF00>%</color>";
        }

        // --- BODYGUARD (Σ) ---
        if (player.TryGetModifier<BodyguardShieldModifier>(out var shieldMod) && !__result.Contains('Σ') && (shieldMod.VisibleSymbol || deadKnow))
        {
            __result += " <color=#003380>Σ</color>";
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























