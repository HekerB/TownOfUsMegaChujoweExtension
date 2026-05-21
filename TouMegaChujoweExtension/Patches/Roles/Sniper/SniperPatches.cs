using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AmongUs.GameOptions;
using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TownOfUs.Buttons.Neutral;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Options.Roles.Neutral;
using TownOfUs.Utilities;
using TownOfUs.Networking;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Modifiers;

[HarmonyPatch]
public static class SniperDistancePatch
{
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        return AccessTools.PropertyGetter(typeof(CustomActionButton<PlayerControl>), nameof(CustomActionButton<PlayerControl>.Distance));
    }

    [HarmonyPostfix]
    public static void Postfix(ref float __result)
    {
        if (!SniperModifier.LocalPlayerHasSniper())
        {
            return;
        }

        __result = SniperModifier.ApplyRangeMultiplier(__result);
    }
}

[HarmonyPatch]
public static class SniperArsonistIgniteRadiusPatch
{
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        return AccessTools.PropertyGetter(typeof(ArsonistIgniteButton), "PlayersInRange");
    }

    [HarmonyPostfix]
    public static void Postfix(ref List<PlayerControl> __result)
    {
        if (!SniperModifier.LocalPlayerHasSniper() ||
            OptionGroupSingleton<ArsonistOptions>.Instance.LegacyArsonist ||
            ShipStatus.Instance == null)
        {
            return;
        }

        var baseRadius = OptionGroupSingleton<ArsonistOptions>.Instance.IgniteRadius.Value *
            ShipStatus.Instance.MaxLightRadius;
        __result = Helpers.GetClosestPlayers(PlayerControl.LocalPlayer, SniperModifier.ApplyRangeMultiplier(baseRadius));
    }
}

[HarmonyPatch(typeof(ArsonistIgniteButton), "FixedUpdate")]
public static class SniperLegacyArsonistIgnitePatch
{
    public static void Postfix(ArsonistIgniteButton __instance)
    {
        if (!SniperModifier.LocalPlayerHasSniper() ||
            MeetingHud.Instance ||
            !OptionGroupSingleton<ArsonistOptions>.Instance.LegacyArsonist)
        {
            return;
        }

        var killDistances =
            GameOptionsManager.Instance.currentNormalGameOptions.GetFloatArray(FloatArrayOptionNames.KillDistances);
        var baseDistance = killDistances[GameOptionsManager.Instance.currentNormalGameOptions.KillDistance];
        __instance.ClosestTarget = Helpers.GetClosestPlayers(PlayerControl.LocalPlayer,
                SniperModifier.ApplyRangeMultiplier(baseDistance))
            .FirstOrDefault(x => x.HasModifier<ArsonistDousedModifier>());
    }
}

public static class SniperKillLogic
{
    private static readonly MethodInfo? VampireConvertCheck =
        AccessTools.Method(typeof(VampireBiteButton), "ConvertCheck");

    private static bool CanConvertVampireTarget(PlayerControl target)
    {
        return (bool?)VampireConvertCheck?.Invoke(null, new object[] { target }) == true;
    }

    public static bool TryMurderWithTeleport(PlayerControl? target, bool createDeadBody = true)
    {
        if (!SniperModifier.LocalPlayerHasSniper() || target == null)
        {
            return true;
        }

        // Instruction: Teleport murderer to body (teleportMurderer: true)
        PlayerControl.LocalPlayer.RpcCustomMurder(
            target,
            MeetingCheck.OutsideMeeting,
            teleportMurderer: true,
            createDeadBody: createDeadBody);
        return false;
    }

    public static bool TryVampireBiteWithTeleport(PlayerControl? target)
    {
        if (!SniperModifier.LocalPlayerHasSniper() || target == null || CanConvertVampireTarget(target))
        {
            return true;
        }

        // Instruction: Teleport murderer to body (teleportMurderer: true)
        PlayerControl.LocalPlayer.RpcCustomMurder(
            target,
            MeetingCheck.OutsideMeeting,
            teleportMurderer: true);
        return false;
    }
}

[HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
public static class SniperKillButtonPatch
{
    public static bool Prefix(KillButton __instance)
    {
        if (!SniperModifier.LocalPlayerHasSniper() || __instance.currentTarget == null)
        {
            return true;
        }

        return SniperKillLogic.TryMurderWithTeleport(__instance.currentTarget);
    }
}

[HarmonyPatch(typeof(KillButton), "FixedUpdate")]
public static class SniperKillButtonRangePatch
{
    public static void Postfix(KillButton __instance)
    {
        if (!SniperModifier.LocalPlayerHasSniper()) return;
        if (__instance.isCoolingDown || !__instance.isActiveAndEnabled || MeetingHud.Instance) return;

        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || localPlayer.Data == null || localPlayer.Data.IsDead) return;

        var killDistances = GameOptionsManager.Instance.currentNormalGameOptions.GetFloatArray(FloatArrayOptionNames.KillDistances);
        var baseDistance = killDistances[GameOptionsManager.Instance.currentNormalGameOptions.KillDistance];
        var maxDistance = SniperModifier.ApplyRangeMultiplier(baseDistance);

        var closest = Helpers.GetClosestPlayers(localPlayer, maxDistance).FirstOrDefault(p => p != null && p != localPlayer && !p.Data.IsDead);
        if (closest != null)
        {
            __instance.SetTarget(closest);
        }
    }
}

[HarmonyPatch(typeof(GlitchKillButton), "OnClick")]
public static class SniperGlitchKillPatch
{
    public static bool Prefix(GlitchKillButton __instance) =>
        SniperKillLogic.TryMurderWithTeleport(__instance.Target);
}

[HarmonyPatch(typeof(JuggernautKillButton), "OnClick")]
public static class SniperJuggernautKillPatch
{
    public static bool Prefix(JuggernautKillButton __instance) =>
        SniperKillLogic.TryMurderWithTeleport(__instance.Target);
}

[HarmonyPatch(typeof(PestilenceKillButton), "OnClick")]
public static class SniperPestilenceKillPatch
{
    public static bool Prefix(PestilenceKillButton __instance) =>
        SniperKillLogic.TryMurderWithTeleport(__instance.Target);
}

[HarmonyPatch(typeof(SoulCollectorReapButton), "OnClick")]
public static class SniperSoulCollectorReapPatch
{
    public static bool Prefix(SoulCollectorReapButton __instance) =>
        SniperKillLogic.TryMurderWithTeleport(__instance.Target, createDeadBody: false);
}

[HarmonyPatch(typeof(VampireBiteButton), "OnClick")]
public static class SniperVampireBitePatch
{
    public static bool Prefix(VampireBiteButton __instance) =>
        SniperKillLogic.TryVampireBiteWithTeleport(__instance.Target);
}

[HarmonyPatch(typeof(WerewolfKillButton), "OnClick")]
public static class SniperWerewolfKillPatch
{
    public static bool Prefix(WerewolfKillButton __instance) =>
        SniperKillLogic.TryMurderWithTeleport(__instance.Target);
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius))]
public static class SniperScopeVisionPatch
{
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(NetworkedPlayerInfo player, ref float __result)
    {
        if (PlayerControl.LocalPlayer == null) return;
        
        var role = PlayerControl.LocalPlayer.GetRole<TouMegaChujoweExtension.Roles.Classic.Impostor.SniperRole>();
        if (role != null && role.IsScopeActive)
        {
            __result = 25f; // Neon high-tech laser scope long-distance bypass
        }
    }
}

[HarmonyPatch(typeof(LogicOptions), nameof(LogicOptions.GetPlayerSpeedMod))]
public static class SniperMovementBlockPatch
{
    [HarmonyPostfix]
    public static void Postfix(PlayerControl pc, ref float __result)
    {
        if (pc == null || pc != PlayerControl.LocalPlayer) return;

        var role = pc.GetRole<TouMegaChujoweExtension.Roles.Classic.Impostor.SniperRole>();
        if (role != null && role.IsScopeActive)
        {
            __result = 0f;
        }
    }
}
