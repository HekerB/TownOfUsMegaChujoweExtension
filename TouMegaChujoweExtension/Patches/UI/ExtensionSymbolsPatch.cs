using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TouMegaChujoweExtension.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TouMegaChujoweExtension.Utilities;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Options;
using TownOfUs.Utilities;
using TownOfUs.Modules;
using TownOfUs.Roles;
using TownOfUs.Roles.Neutral;
using TownOfUs.Modifiers.Neutral;
using TownOfUs;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches;

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
        if (TryGetDeathNoteTarget(player))
        {
            if (!__result.Contains("♡"))
            {
                // In this mod, only the cursed target is shown the symbol if specified, 
                // but here we show it to authorized players (authorized as per your mod's design)
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

        // --- WITCH (¤) ---
        if (player.TryGetModifier<WitchSpellboundModifier>(out _))
        {
            if (!__result.Contains("¤"))
            {
                bool hasWitch = local.IsRole<WitchRole>();
                if (hasWitch || deadKnow)
                {
                    __result += " <color=#C0C0C0>¤</color>";
                }
            }
        }

        // --- GASLIGHTER CURSE (¤) ---
        if (player.TryGetModifier<GaslighterCursedModifier>(out _))
        {
            if (!__result.Contains("¤"))
            {
                bool hasGaslighter = local.IsRole<GaslighterRole>();
                if (hasGaslighter || deadKnow)
                {
                    __result += " <color=#FF4500>¤</color>"; // Orange Red for Gaslighter Curse
                }
            }
        }

        // --- GASLIGHTER KNIGHT (♠) ---
        if (player.TryGetModifier<GaslighterKnightedModifier>(out _))
        {
            var symbol = $" {TownOfUsColors.Monarch.ToTextColor()}♠</color>";
            if (!__result.Contains("♠") && !__result.Contains(symbol))
            {
                bool hasGaslighter = local.IsRole<GaslighterRole>();
                if (hasGaslighter || deadKnow)
                {
                    __result += symbol;
                }
            }
        }

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
            bool isProtectedByLocal = player.TryGetModifier<BodyguardShieldModifier>(out var bgMod) && bgMod.Bodyguard.PlayerId == local.PlayerId;
            bool isBodyguard = local.IsRole<BodyguardRole>();
            bool isAnyProtected = player.HasModifier<BodyguardShieldModifier>();

            if ((isBodyguard && isProtectedByLocal) || (deadKnow && isAnyProtected))
            {
                __result += " <color=#003380>Σ</color>";
            }
        }

        // --- GRIM REAPER MARK (ζ) ---
        if (player.TryGetModifier<GrimReaperMarkedModifier>(out _))
        {
            if (!__result.Contains("ζ"))
            {
                bool isReaper = local.IsRole<GrimReaperRole>();
                if (isReaper || deadKnow)
                {
                    __result += $" {TouExtensionColors.GrimReaper.ToTextColor()}ζ</color>";
                }
            }
        }

        // --- BAKER BREAD MARK (β) ---
        if (player.TryGetModifier<BakerBreadModifier>(out _))
        {
            if (!__result.Contains("β"))
            {
                bool isBaker = local.IsRole<BakerRole>();
                if (isBaker || deadKnow)
                {
                    __result += $" {TouExtensionColors.Baker.ToTextColor()}β</color>";
                }
            }
        }

        // --- FAMINE STARVED MARK (φ) ---
        if (player.TryGetModifier<FamineStarvedModifier>(out _))
        {
            if (!__result.Contains("φ"))
            {
                bool isFamine = local.IsRole<FamineRole>();
                if (isFamine || deadKnow)
                {
                    __result += $" {TouExtensionColors.Famine.ToTextColor()}φ</color>";
                }
            }
        }

        // --- VOODOO MASTER BLIND MARK (ξ) ---
        if (player.TryGetModifier<VoodooBlindModifier>(out _))
        {
            if (!__result.Contains("ξ"))
            {
                if (local.IsImpostorAligned() || deadKnow)
                {
                    __result += " <color=#BA55D3>ξ</color>";
                }
            }
        }

        // --- VOODOO MASTER MUTE MARK (μ) ---
        if (player.TryGetModifier<VoodooMutedModifier>(out _) || 
            (player.TryGetModifier<VoodooScheduledCurseModifier>(out var muteScheduled) && muteScheduled.CurseType == VoodooEffect.Mute))
        {
            if (!__result.Contains("μ"))
            {
                if (local.IsImpostorAligned() || deadKnow)
                {
                    __result += " <color=#40E0D0>μ</color>";
                }
            }
        }

        // --- VOODOO MASTER DEAF MARK (δ) ---
        if (player.TryGetModifier<VoodooDeafenedModifier>(out _) || 
            (player.TryGetModifier<VoodooScheduledCurseModifier>(out var deafScheduled) && deafScheduled.CurseType == VoodooEffect.Deafness))
        {
            if (!__result.Contains("δ"))
            {
                if (local.IsImpostorAligned() || deadKnow)
                {
                    __result += " <color=#DC143C>δ</color>";
                }
            }
        }

        // Removed PZ name suffix as requested
        
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

        // --- DETONATOR (λ) ---
        if (DetonatorSystem.HasBomb(player.PlayerId))
        {
            if (!__result.Contains("λ"))
            {
                if (local.IsImpostorAligned() || deadKnow)
                {
                    __result += " <color=#FF0000>λ</color>";
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

    [HarmonyPatch(nameof(PlayerRoleTextExtensions.UpdateTargetColor), typeof(Color), typeof(PlayerControl), typeof(bool))]
    [HarmonyPostfix]
    public static void UpdateTargetColorPostfix(ref Color __result, PlayerControl player, bool hidden)
    {
        var local = PlayerControl.LocalPlayer;
        if (player == null || local == null || player.Data == null || local.Data == null) return;
        
        // Detonator role itself is red
        if (player.IsRole<DetonatorRole>())
        {
            __result = TouExtensionColors.Detonator;
        }

        // Bomb target is red for all Impostors
        if (local.IsImpostorAligned() && DetonatorSystem.HasBomb(player.PlayerId))
        {
            __result = Color.red;
        }

        // Grim Reaper role itself is gray
        if (player.IsRole<GrimReaperRole>())
        {
            __result = TouExtensionColors.GrimReaper;
        }

        // Marked target is gray for Grim Reaper
        if (local.IsRole<GrimReaperRole>() && player.HasModifier<GrimReaperMarkedModifier>())
        {
            __result = TouExtensionColors.GrimReaper;
        }

        // Baker role itself is BurlyWood
        if (player.IsRole<BakerRole>())
        {
            __result = TouExtensionColors.Baker;
        }

        // Player with bread is Baker color for Baker
        if (local.IsRole<BakerRole>() && player.HasModifier<BakerBreadModifier>())
        {
            __result = TouExtensionColors.Baker;
        }

        // Famine role itself is SaddleBrown
        if (player.IsRole<FamineRole>())
        {
            __result = TouExtensionColors.Famine;
        }

        // Starved target is SaddleBrown for Famine
        if (local.IsRole<FamineRole>() && player.HasModifier<FamineStarvedModifier>())
        {
            __result = TouExtensionColors.Famine;
        }

        // Voodoo Master curses color code for Impostors
        if (local.IsImpostorAligned())
        {
            if (player.HasModifier<VoodooBlindModifier>())
            {
                __result = new Color32(186, 85, 211, 255); // MediumOrchid for Blinded
            }
            else if (player.HasModifier<VoodooMutedModifier>() || 
                     (player.TryGetModifier<VoodooScheduledCurseModifier>(out var muteMod) && muteMod.CurseType == VoodooEffect.Mute))
            {
                __result = new Color32(64, 224, 208, 255); // Turquoise for Muted
            }
            else if (player.HasModifier<VoodooDeafenedModifier>() || 
                     (player.TryGetModifier<VoodooScheduledCurseModifier>(out var deafMod) && deafMod.CurseType == VoodooEffect.Deafness))
            {
                __result = new Color32(220, 20, 60, 255); // Crimson for Deafened
            }
        }
    }

    private static bool TryGetDeathNoteTarget(PlayerControl player)
    {
        return DeathNoteModifier.CursedTargets.Contains(player.PlayerId);
    }
}
