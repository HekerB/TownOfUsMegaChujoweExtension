using HarmonyLib;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Roles.Impostor;
using TouMegaChujoweExtension.Buttons.Classic.Impostor;
using TownOfUs.Utilities;
using UnityEngine;
using TownOfUs.Networking;
using MiraAPI.Networking;
using MiraAPI.Modifiers;

namespace TouMegaChujoweExtension.Patches.Impostor;

[HarmonyPatch]
public static class DumperPatches
{
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    public static void HudManagerUpdatePostfix()
    {
        if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started) return;
        DumperSystem.Update();
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.OnEnable))]
    [HarmonyPrefix]
    public static void ShipStatusOnEnablePrefix()
    {
        DumperSystem.Reset();
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPrefix]
    public static void MeetingHudStartPrefix()
    {
        DumperSystem.Reset();
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    [HarmonyPostfix]
    public static void MurderPlayerPostfix(PlayerControl __instance)
    {
        if (__instance == PlayerControl.LocalPlayer && __instance.IsRole<DumperRole>())
        {
            DumperDragButton.SetOwnCooldown();
        }
    }

    [HarmonyPatch(typeof(CustomMurderRpc), nameof(CustomMurderRpc.RpcCustomMurder), typeof(PlayerControl), typeof(PlayerControl), typeof(MeetingCheck), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool))]
    [HarmonyPostfix]
    public static void RpcCustomMurderPostfix(PlayerControl source, PlayerControl target, MeetingCheck inMeeting, bool __result)
    {
        if (__result && source == PlayerControl.LocalPlayer && source.IsRole<DumperRole>())
        {
            DumperDragButton.SetOwnCooldown();
        }
    }

    [HarmonyPatch(typeof(CustomTouMurderRpcs), nameof(CustomTouMurderRpcs.RpcSpecialMurder))]
    [HarmonyPostfix]
    public static void RpcSpecialMurderPostfix(PlayerControl source, PlayerControl target, bool isIndirect, bool ignoreShield, bool __result)
    {
        if (__result && source == PlayerControl.LocalPlayer && source.IsRole<DumperRole>())
        {
            DumperDragButton.SetOwnCooldown();
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetKillTimer))]
public static class DumperKillTimerSyncPatch
{
    private static float _lastKillTimer;

    [HarmonyPostfix]
    public static void Postfix(PlayerControl __instance, float time)
    {
        if (__instance != PlayerControl.LocalPlayer) return;
        if (!__instance.IsRole<DumperRole>()) return;

        if (time > 5f && time > _lastKillTimer + 1f)
        {
            DumperDragButton.SetOwnCooldown();
        }

        _lastKillTimer = time;
    }
}

[HarmonyPatch(typeof(TownOfUs.Modifiers.Impostor.DragModifier), nameof(TownOfUs.Modifiers.Impostor.DragModifier.OnActivate))]
public static class DumperDragModifierOnActivatePatch
{
    [HarmonyPostfix]
    public static void Postfix(TownOfUs.Modifiers.Impostor.DragModifier __instance)
    {
        if (__instance.Player != null && __instance.Player.IsRole<DumperRole>())
        {
            __instance.SpeedFactor = 1.0f;
        }
    }
}

[HarmonyPatch(typeof(LogicOptions), nameof(LogicOptions.GetPlayerSpeedMod))]
public static class DumperSpeedPatch
{
    [HarmonyPrefix]
    public static void Prefix(PlayerControl pc)
    {
        if (pc == null || pc.Data == null || pc.Data.IsDead) return;

        var role = pc.GetRole<DumperRole>();
        if (role != null && role.DraggingBodyId.HasValue)
        {
            if (pc.TryGetModifier<TownOfUs.Modifiers.Impostor.DragModifier>(out var drag))
            {
                drag.SpeedFactor = 1.0f;
            }
        }
    }

    [HarmonyPostfix]
    public static void Postfix(PlayerControl pc, ref float __result)
    {
        if (pc == null || pc.Data == null || pc.Data.IsDead) return;

        var role = pc.GetRole<DumperRole>();
        if (role != null && role.DraggingBodyId.HasValue && role.AutoDumpTime.HasValue)
        {
            var options = MiraAPI.GameOptions.OptionGroupSingleton<TouMegaChujoweExtension.Options.Roles.Impostor.DumperOptions>.Instance;
            float remaining = role.AutoDumpTime.Value - UnityEngine.Time.time;

            if (remaining <= 3f)
            {
                float progress = UnityEngine.Mathf.Clamp01(remaining / 3f);
                float speedMultiplier = UnityEngine.Mathf.Lerp(options.DragSpeedModifier, 1.0f, progress);
                __result *= speedMultiplier;
            }
        }
    }
}
