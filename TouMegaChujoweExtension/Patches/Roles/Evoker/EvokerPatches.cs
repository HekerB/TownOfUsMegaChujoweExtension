using HarmonyLib;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Evoker;

// ==================== HUD (outlines for Evoker) ====================

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class EvokerHudPatch
{
    private static float _lastOutlineUpdateTime;

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix()
    {
        EvokerSystem.Update();

        if (!EvokerSystem.IsBlindActive) return;

        if (Time.time - _lastOutlineUpdateTime < 0.2f) return;
        _lastOutlineUpdateTime = Time.time;

        var local = PlayerControl.LocalPlayer;
        if (local?.Data?.Role is not EvokerRole) return;

        foreach (var kvp in EvokerSystem.VerifiedPlayers)
        {
            var target = MiscUtils.PlayerById(kvp.Key);
            if (target != null && !target.HasDied())
            {
                var color = kvp.Value ? Palette.ImpostorRed : Palette.CrewmateBlue;
                target.cosmetics.SetOutline(true, new Il2CppSystem.Nullable<Color>(color));
            }
        }
    }
}

// ==================== VANILLA KILL BUTTON BLOCK ====================

[HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
public static class EvokerBlockVanillaKillClickPatch
{
    [HarmonyPrefix]
    public static bool Prefix()
    {
        return !EvokerSystem.IsLocalPlayerBlocked();
    }
}

[HarmonyPatch(typeof(KillButton), nameof(KillButton.SetTarget))]
public static class EvokerBlockVanillaKillTargetPatch
{
    [HarmonyPrefix]
    public static bool Prefix(KillButton __instance)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __instance.currentTarget = null;
        return false;
    }
}

// ==================== VANILLA ABILITY BUTTON BLOCK (Shapeshifter etc.) ====================

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class EvokerBlockAbilityClickPatch
{
    [HarmonyPrefix]
    public static bool Prefix()
    {
        return !EvokerSystem.IsLocalPlayerBlocked();
    }
}

// ==================== SERVER-SIDE SAFETY (backup) ====================

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckMurder))]
public static class EvokerBlockCmdMurderPatch
{
    [HarmonyPrefix]
    public static bool Prefix(PlayerControl __instance)
    {
        if (!EvokerSystem.IsBlindActive) return true;
        return !EvokerSystem.IsBlindTarget(__instance);
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
public static class EvokerBlockCheckMurderPatch
{
    [HarmonyPrefix]
    public static bool Prefix(PlayerControl __instance)
    {
        if (!EvokerSystem.IsBlindActive) return true;
        return !EvokerSystem.IsBlindTarget(__instance);
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
public static class EvokerBlockMurderPlayerPatch
{
    [HarmonyPrefix]
    public static bool Prefix(PlayerControl __instance)
    {
        if (!EvokerSystem.IsBlindActive) return true;
        return !EvokerSystem.IsBlindTarget(__instance);
    }
}

// ==================== POISONER SPECIAL BLOCK ====================

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Impostor.PoisonerPoisonButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Impostor.PoisonerPoisonButton.ClickHandler))]
public static class EvokerBlockPoisonerClickPatch
{
    [HarmonyPrefix]
    public static bool Prefix()
    {
        return !EvokerSystem.IsLocalPlayerBlocked();
    }
}

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Impostor.PoisonerPoisonButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Impostor.PoisonerPoisonButton.CanUse))]
public static class EvokerBlockPoisonerCanUsePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
public static class EvokerRoundStartPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        EvokerSystem.OnRoundStart();
    }
}


[HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
public static class EvokerMeetingResetPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        EvokerSystem.OnRoundStart();
    }
}

// ==================== ALL CUSTOM BUTTONS BLOCK ====================

[HarmonyPatch(typeof(TownOfUs.Buttons.TownOfUsButton), nameof(TownOfUs.Buttons.TownOfUsButton.CanUse))]
public static class EvokerBlockTownOfUsButtonCanUsePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(TownOfUs.Buttons.TownOfUsButton), nameof(TownOfUs.Buttons.TownOfUsButton.ClickHandler))]
public static class EvokerBlockTownOfUsButtonClickHandlerPatch
{
    [HarmonyPrefix]
    public static bool Prefix()
    {
        return !EvokerSystem.IsLocalPlayerBlocked();
    }
}

[HarmonyPatch(typeof(TownOfUs.Buttons.TownOfUsTargetButton<PlayerControl>), nameof(TownOfUs.Buttons.TownOfUsTargetButton<PlayerControl>.CanUse))]
public static class EvokerBlockPlayerTargetButtonCanUsePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(TownOfUs.Buttons.TownOfUsTargetButton<PlayerControl>), nameof(TownOfUs.Buttons.TownOfUsTargetButton<PlayerControl>.ClickHandler))]
public static class EvokerBlockPlayerTargetButtonClickHandlerPatch
{
    [HarmonyPrefix]
    public static bool Prefix()
    {
        return !EvokerSystem.IsLocalPlayerBlocked();
    }
}

[HarmonyPatch(typeof(TownOfUs.Buttons.TownOfUsTargetButton<DeadBody>), nameof(TownOfUs.Buttons.TownOfUsTargetButton<DeadBody>.CanUse))]
public static class EvokerBlockDeadBodyTargetButtonCanUsePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(TownOfUs.Buttons.TownOfUsTargetButton<DeadBody>), nameof(TownOfUs.Buttons.TownOfUsTargetButton<DeadBody>.ClickHandler))]
public static class EvokerBlockDeadBodyTargetButtonClickHandlerPatch
{
    [HarmonyPrefix]
    public static bool Prefix()
    {
        return !EvokerSystem.IsLocalPlayerBlocked();
    }
}

[HarmonyPatch(typeof(TownOfUs.Buttons.TownOfUsTargetButton<Vent>), nameof(TownOfUs.Buttons.TownOfUsTargetButton<Vent>.CanUse))]
public static class EvokerBlockVentTargetButtonCanUsePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(TownOfUs.Buttons.TownOfUsTargetButton<Vent>), nameof(TownOfUs.Buttons.TownOfUsTargetButton<Vent>.ClickHandler))]
public static class EvokerBlockVentTargetButtonClickHandlerPatch
{
    [HarmonyPrefix]
    public static bool Prefix()
    {
        return !EvokerSystem.IsLocalPlayerBlocked();
    }
}

// Specific Button Overrides:

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Neutral.BountyHunterKillButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Neutral.BountyHunterKillButton.ClickHandler))]
public static class EvokerBlockBountyHunterClickPatch { [HarmonyPrefix] public static bool Prefix() => !EvokerSystem.IsLocalPlayerBlocked(); }

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Neutral.BountyHunterKillButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Neutral.BountyHunterKillButton.CanUse))]
public static class EvokerBlockBountyHunterCanUsePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Impostor.RcXdDeployButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Impostor.RcXdDeployButton.ClickHandler))]
public static class EvokerBlockRcXdClickPatch { [HarmonyPrefix] public static bool Prefix() => !EvokerSystem.IsLocalPlayerBlocked(); }

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Impostor.RcXdDeployButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Impostor.RcXdDeployButton.CanUse))]
public static class EvokerBlockRcXdCanUsePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Impostor.WitchSpellButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Impostor.WitchSpellButton.ClickHandler))]
public static class EvokerBlockWitchSpellClickPatch { [HarmonyPrefix] public static bool Prefix() => !EvokerSystem.IsLocalPlayerBlocked(); }

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Impostor.WitchSpellButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Impostor.WitchSpellButton.CanUse))]
public static class EvokerBlockWitchSpellCanUsePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Impostor.WitchKillButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Impostor.WitchKillButton.ClickHandler))]
public static class EvokerBlockWitchKillClickPatch { [HarmonyPrefix] public static bool Prefix() => !EvokerSystem.IsLocalPlayerBlocked(); }

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Impostor.WitchKillButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Impostor.WitchKillButton.CanUse))]
public static class EvokerBlockWitchKillCanUsePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Impostor.SniperShootButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Impostor.SniperShootButton.ClickHandler))]
public static class EvokerBlockSniperShootClickPatch { [HarmonyPrefix] public static bool Prefix() => !EvokerSystem.IsLocalPlayerBlocked(); }

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Impostor.SniperShootButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Impostor.SniperShootButton.CanUse))]
public static class EvokerBlockSniperShootCanUsePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Impostor.OutlawKillButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Impostor.OutlawKillButton.ClickHandler))]
public static class EvokerBlockOutlawKillClickPatch { [HarmonyPrefix] public static bool Prefix() => !EvokerSystem.IsLocalPlayerBlocked(); }

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Impostor.OutlawKillButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Impostor.OutlawKillButton.CanUse))]
public static class EvokerBlockOutlawKillCanUsePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Impostor.KamikazeSuicideButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Impostor.KamikazeSuicideButton.ClickHandler))]
public static class EvokerBlockKamikazeSuicideClickPatch { [HarmonyPrefix] public static bool Prefix() => !EvokerSystem.IsLocalPlayerBlocked(); }

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Impostor.KamikazeSuicideButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Impostor.KamikazeSuicideButton.CanUse))]
public static class EvokerBlockKamikazeSuicideCanUsePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Impostor.DetonatorAttachButton), nameof(TouMegaChujoweExtension.Buttons.Impostor.DetonatorAttachButton.ClickHandler))]
public static class EvokerBlockDetonatorAttachClickPatch { [HarmonyPrefix] public static bool Prefix() => !EvokerSystem.IsLocalPlayerBlocked(); }

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Impostor.DetonatorAttachButton), nameof(TouMegaChujoweExtension.Buttons.Impostor.DetonatorAttachButton.CanUse))]
public static class EvokerBlockDetonatorAttachCanUsePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Neutral.BakerGiveButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Neutral.BakerGiveButton.ClickHandler))]
public static class EvokerBlockBakerGiveClickPatch { [HarmonyPrefix] public static bool Prefix() => !EvokerSystem.IsLocalPlayerBlocked(); }

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Neutral.BakerGiveButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Neutral.BakerGiveButton.CanUse))]
public static class EvokerBlockBakerGiveCanUsePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Neutral.FamineStarveButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Neutral.FamineStarveButton.ClickHandler))]
public static class EvokerBlockFamineStarveClickPatch { [HarmonyPrefix] public static bool Prefix() => !EvokerSystem.IsLocalPlayerBlocked(); }

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Neutral.FamineStarveButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Neutral.FamineStarveButton.CanUse))]
public static class EvokerBlockFamineStarveCanUsePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Neutral.BerserkerKillButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Neutral.BerserkerKillButton.ClickHandler))]
public static class EvokerBlockBerserkerKillClickPatch { [HarmonyPrefix] public static bool Prefix() => !EvokerSystem.IsLocalPlayerBlocked(); }

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Neutral.BerserkerKillButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Neutral.BerserkerKillButton.CanUse))]
public static class EvokerBlockBerserkerKillCanUsePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Neutral.WarKillButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Neutral.WarKillButton.ClickHandler))]
public static class EvokerBlockWarKillClickPatch { [HarmonyPrefix] public static bool Prefix() => !EvokerSystem.IsLocalPlayerBlocked(); }

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Neutral.WarKillButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Neutral.WarKillButton.CanUse))]
public static class EvokerBlockWarKillCanUsePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Neutral.DoppelgangerKillButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Neutral.DoppelgangerKillButton.ClickHandler))]
public static class EvokerBlockDoppelgangerKillClickPatch { [HarmonyPrefix] public static bool Prefix() => !EvokerSystem.IsLocalPlayerBlocked(); }

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Neutral.DoppelgangerKillButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Neutral.DoppelgangerKillButton.CanUse))]
public static class EvokerBlockDoppelgangerKillCanUsePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Neutral.PelicanSwallowButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Neutral.PelicanSwallowButton.ClickHandler))]
public static class EvokerBlockPelicanSwallowClickPatch { [HarmonyPrefix] public static bool Prefix() => !EvokerSystem.IsLocalPlayerBlocked(); }

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Neutral.PelicanSwallowButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Neutral.PelicanSwallowButton.CanUse))]
public static class EvokerBlockPelicanSwallowCanUsePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Neutral.SerialKillerKillButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Neutral.SerialKillerKillButton.ClickHandler))]
public static class EvokerBlockSerialKillerKillClickPatch { [HarmonyPrefix] public static bool Prefix() => !EvokerSystem.IsLocalPlayerBlocked(); }

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Neutral.SerialKillerKillButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Neutral.SerialKillerKillButton.CanUse))]
public static class EvokerBlockSerialKillerKillCanUsePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Neutral.ShroudKillButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Neutral.ShroudKillButton.ClickHandler))]
public static class EvokerBlockShroudKillClickPatch { [HarmonyPrefix] public static bool Prefix() => !EvokerSystem.IsLocalPlayerBlocked(); }

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Neutral.ShroudKillButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Neutral.ShroudKillButton.CanUse))]
public static class EvokerBlockShroudKillCanUsePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Neutral.ShroudAbilityButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Neutral.ShroudAbilityButton.ClickHandler))]
public static class EvokerBlockShroudAbilityClickPatch { [HarmonyPrefix] public static bool Prefix() => !EvokerSystem.IsLocalPlayerBlocked(); }

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Neutral.ShroudAbilityButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Neutral.ShroudAbilityButton.CanUse))]
public static class EvokerBlockShroudAbilityCanUsePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Neutral.SoulCollectorReapButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Neutral.SoulCollectorReapButton.ClickHandler))]
public static class EvokerBlockSoulCollectorReapClickPatch { [HarmonyPrefix] public static bool Prefix() => !EvokerSystem.IsLocalPlayerBlocked(); }

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Neutral.SoulCollectorReapButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Neutral.SoulCollectorReapButton.CanUse))]
public static class EvokerBlockSoulCollectorReapCanUsePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Neutral.DeathKillButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Neutral.DeathKillButton.ClickHandler))]
public static class EvokerBlockDeathKillClickPatch { [HarmonyPrefix] public static bool Prefix() => !EvokerSystem.IsLocalPlayerBlocked(); }

[HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Classic.Neutral.DeathKillButton), nameof(TouMegaChujoweExtension.Buttons.Classic.Neutral.DeathKillButton.CanUse))]
public static class EvokerBlockDeathKillCanUsePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!EvokerSystem.IsLocalPlayerBlocked()) return true;
        __result = false;
        return false;
    }
}