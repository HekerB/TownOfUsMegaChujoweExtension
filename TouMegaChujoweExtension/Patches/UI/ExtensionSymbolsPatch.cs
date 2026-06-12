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
using TouMegaChujoweExtension.Patches.Roles;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Modifiers.Impostor;

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

        // --- INVERTER (?) ---
        if (player.TryGetModifier<InverterDisorientedModifier>(out _) && !__result.Contains('?') && (local.IsImpostorAligned() || deadKnow))
        {
            __result += " <color=#FF0000>?</color>";
        }

        // --- VOODOO MASTER CURSES ---
        bool localIsVoodoo = local.IsRole<VoodooMasterRole>();
        bool canSeeVoodooCurse = localIsVoodoo || deadKnow;
        if (canSeeVoodooCurse)
        {
            if (player.TryGetModifier<VoodooConfusedModifier>(out var confused) &&
                confused.VoodooMaster != null &&
                (deadKnow || confused.VoodooMaster.PlayerId == local.PlayerId) &&
                !__result.Contains("$"))
            {
                __result += " <color=#FF0000>$</color>";
            }

            if (IsVoodooScheduledMute(player) && !__result.Contains("[M]"))
            {
                __result += " <color=#2A1119>[M]</color>";
            }
        }

        if ((local.PlayerId == player.PlayerId || deadKnow) && player.HasModifier<VoodooMutedModifier>() && !__result.Contains("[M]"))
        {
            __result += " <color=#2A1119>[M]</color>";
        }

        // --- VOODOO MASTER TARGET LOCK [L] ---
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc != null && pc.TryGetModifier<VoodooTargetLockModifier>(out var targetLock))
            {
                if (targetLock.TargetId == player.PlayerId)
                {
                    bool isLocalVoodooMaster = local.PlayerId == pc.PlayerId;
                    if ((isLocalVoodooMaster || deadKnow) && !__result.Contains("[L]"))
                    {
                        __result += " <color=#FF0000>[L]</color>";
                    }
                }
            }
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

        InnocentTauntMeetingDisplay.TryAppendTauntSymbol(ref __result, player);

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

        // --- APOCALYPSE TEAM ---
        if (CanSeeApocalypseRole(local, player, deadKnow))
        {
            var roleName = ApocalypseUtils.GetDisplayRoleName(player);
            var roleColorHex = ColorUtility.ToHtmlStringRGB(ApocalypseUtils.GetRoleColor(player));
            var prefix = player.PlayerId != local.PlayerId && !deadKnow && !__result.Contains(roleName)
                ? $"<size=80%><color=#{roleColorHex}>{roleName}</color></size>\n"
                : string.Empty;

            __result = $"{prefix}{__result}";
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
        var pendingAssignments = Patches.Roles.Jackal.JackalStartPatch.PendingAssignments;
        bool playerIsRecruit = player.TryGetModifier<SidekickModifier>(out var mod);
        byte playerJackalId = 255;

        if (playerIsRecruit && mod != null)
        {
            playerJackalId = mod.JackalId;
        }
        if (playerJackalId == 255 && pendingAssignments.TryGetValue(player.PlayerId, out var pendingJId))
        {
            playerJackalId = pendingJId;
            playerIsRecruit = true;
        }

        if (playerIsRecruit)
        {
            bool localIsJackal = local.IsRole<JackalRole>();
            bool isPcRecruitOfLocal = (playerJackalId != 255 && playerJackalId == local.PlayerId);
            byte localJackalId = 255;
            bool localIsSidekick = local.TryGetModifier<SidekickModifier>(out var localMod);
            if (localIsSidekick && localMod != null)
            {
                localJackalId = localMod.JackalId;
            }
            if (localJackalId == 255 && pendingAssignments.TryGetValue(local.PlayerId, out var localPendingJId))
            {
                localJackalId = localPendingJId;
                localIsSidekick = true;
            }

            bool isPcSameRecruitTeam = localIsSidekick &&
                                       playerJackalId != 255 && localJackalId != 255 &&
                                       playerJackalId == localJackalId;

            bool canSeeRecruit = (localIsJackal && isPcRecruitOfLocal) ||
                                 (local.PlayerId == player.PlayerId) ||
                                 isPcSameRecruitTeam;

            bool canSeeTeam = canSeeRecruit || deadKnow;

            if (canSeeTeam)
            {
                string prefix = "";

                bool canSeeRole = localIsJackal ||
                    (isPcSameRecruitTeam && OptionGroupSingleton<TouMegaChujoweExtension.Options.ExtensionGeneralOptions>.Instance.RecruitsKnowEachOther);

                if (canSeeRole && player.PlayerId != local.PlayerId && !deadKnow && player.Data != null)
                {
                    var role = RoleManager.Instance?.GetRole(player.GetRoleWhenAlive().Role);
                    if (role != null)
                    {
                        var roleColor = (role as ITownOfUsRole)?.RoleColor ?? (role as ICustomRole)?.RoleColor ?? Color.white;
                        string colorHex = ColorUtility.ToHtmlStringRGB(roleColor);

                        string roleName = role is ITownOfUsRole touRole ? touRole.RoleName : (role as ICustomRole)?.RoleName ?? role.Role.ToString();
                        if (!__result.Contains(roleName))
                        {
                            prefix = $"<size=80%><color=#{colorHex}>{roleName}</color></size>\n";
                        }
                    }
                }

                if (!__result.Contains("(Rec)") && !__result.Contains("(rec)"))
                {
                    __result = $"{prefix}<color=#{jackalHex}>{__result}</color> <color=#FFFFFF>(</color><color=#{jackalHex}>Rec</color><color=#FFFFFF>)</color>";
                }
            }
        }
        else if (player.IsRole<JackalRole>() && (local.PlayerId == player.PlayerId || deadKnow) && !__result.Contains(jackalHex))
        {
            __result = $"<color=#{jackalHex}>{__result}</color>";
        }
    }

    [HarmonyPatch(nameof(PlayerRoleTextExtensions.UpdateTargetSymbols), typeof(string), typeof(PlayerControl), typeof(DataVisibility))]
    [HarmonyPostfix]
    public static void UpdateTargetSymbolsVisibilityPostfix(ref string __result, PlayerControl player, DataVisibility visibility)
    {
        if (player == null)
        {
            return;
        }

        InnocentTauntMeetingDisplay.TryAppendTauntSymbol(ref __result, player);
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
        bool canSeePoison = CanSeePoisonedIndicator(local, hidden);

        // --- POISONER (%) ---
        if (PoisonSystem.IsTargetPoisonedByPoison(player.PlayerId) && !__result.Contains('%') && canSeePoison)
        {
            __result += " <color=#FF0000>%</color>";
        }

        // --- BODYGUARD (Σ) ---
        if (player.TryGetModifier<BodyguardShieldModifier>(out var shieldMod) && !__result.Contains('Σ') && (shieldMod.VisibleSymbol || deadKnow))
        {
            __result += " <color=#003380>Σ</color>";
        }
    }

    [HarmonyPatch(nameof(PlayerRoleTextExtensions.UpdateTargetColor), typeof(Color), typeof(PlayerControl), typeof(bool))]
    [HarmonyPostfix]
    public static void UpdateTargetColorPostfix(ref Color __result, PlayerControl player, bool hidden)
    {
        var local = PlayerControl.LocalPlayer;
        if (player == null || local == null || local.Data == null)
            return;

        var genOpt = OptionGroupSingleton<TownOfUs.Options.GeneralOptions>.Instance;
        bool isGhost = local.HasDied();
        bool deadKnow = isGhost && genOpt.TheDeadKnow && !hidden;

        if (PoisonSystem.IsTargetPoisonedByPoison(player.PlayerId) && CanSeePoisonedIndicator(local, hidden))
        {
            __result = Color.red;
            return;
        }

        if (player.HasModifier<VoodooBlindModifier>() && (local.IsRole<VoodooMasterRole>() || deadKnow))
        {
            __result = Color.black;
            return;
        }

        var pendingAssignments = Patches.Roles.Jackal.JackalStartPatch.PendingAssignments;
        bool playerIsRecruit = player.TryGetModifier<SidekickModifier>(out var mod);
        byte playerJackalId = 255;

        if (playerIsRecruit && mod != null)
        {
            playerJackalId = mod.JackalId;
        }
        if (playerJackalId == 255 && pendingAssignments.TryGetValue(player.PlayerId, out var pendingJId))
        {
            playerJackalId = pendingJId;
            playerIsRecruit = true;
        }

        if (playerIsRecruit)
        {
            bool localIsJackal = local.IsRole<JackalRole>();
            bool isPcRecruitOfLocal = (playerJackalId != 255 && playerJackalId == local.PlayerId);

            byte localJackalId = 255;
            bool localIsSidekick = local.TryGetModifier<SidekickModifier>(out var localMod);
            if (localIsSidekick && localMod != null)
            {
                localJackalId = localMod.JackalId;
            }
            if (localJackalId == 255 && pendingAssignments.TryGetValue(local.PlayerId, out var localPendingJId))
            {
                localJackalId = localPendingJId;
                localIsSidekick = true;
            }

            bool isPcSameRecruitTeam = localIsSidekick &&
                                       playerJackalId != 255 && localJackalId != 255 &&
                                       playerJackalId == localJackalId;

            bool canSeeRecruit = (localIsJackal && isPcRecruitOfLocal) ||
                                 (local.PlayerId == player.PlayerId) ||
                                 isPcSameRecruitTeam;

            if (canSeeRecruit || deadKnow)
            {
                __result = TouExtensionColors.Jackal;
            }
        }
        else if (player.IsRole<JackalRole>() && (local.PlayerId == player.PlayerId || deadKnow))
        {
            __result = TouExtensionColors.Jackal;
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

    private static bool CanSeePoisonedIndicator(PlayerControl local, bool hidden)
    {
        if (local.IsImpostorAligned())
        {
            return true;
        }

        var genOpt = OptionGroupSingleton<TownOfUs.Options.GeneralOptions>.Instance;
        return local.HasDied() && genOpt.TheDeadKnow && !hidden;
    }

    private static bool CanSeeApocalypseRole(PlayerControl local, PlayerControl player, bool deadKnow)
    {
        if (local == null || player == null || !ApocalypseUtils.IsApocalypsePlayer(player))
        {
            return false;
        }

        if (deadKnow)
        {
            return true;
        }

        return ApocalypseUtils.RolesKnowEachOther && ApocalypseUtils.IsApocalypsePlayer(local);
    }

    private static bool IsVoodooScheduledMute(PlayerControl player)
    {
        return player.TryGetModifier<VoodooScheduledCurseModifier>(out var curse) &&
               curse.CurseType == VoodooEffect.Mute;
    }
}
