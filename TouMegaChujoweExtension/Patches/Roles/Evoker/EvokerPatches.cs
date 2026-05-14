using HarmonyLib;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Evoker;

// ==================== HUD (outlines for Evoker) ====================

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class EvokerHudPatch
{
    private static float _lastOutlineUpdateTime = 0f;

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
        __instance.SetDisabled();
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
















