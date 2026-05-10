using HarmonyLib;
using MiraAPI.Hud;
using TownOfUs.Buttons;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.MirageDecoy;

/// <summary>
/// Intercept TownOfUs button activations (mouse or keybind) and trigger a Mirage decoy if the local player is in range.
/// IMPORTANT: We patch the base TownOfUs button handler directly to avoid scanning/patching hundreds of derived button types,
/// which can trigger MonoMod/Harmony detour crashes on some IL2CPP + .NET 6 setups.
/// </summary>
[HarmonyPatch(typeof(TownOfUsButton), nameof(TownOfUsButton.ClickHandler))]
public static class MirageDecoyTownOfUsButtonPatches
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool Prefix(TownOfUsButton __instance)
    {
        if (__instance is MirageDecoyButton)
        {
            return true;
        }

        var distance = GetDistance(__instance);
        if (!TryTriggerFromLocalPlayer(distance))
        {
            return true;
        }

        SpendCooldownAndUses(__instance);
        return false;
    }

    private static float GetDistance(TownOfUsButton instance)
    {
        try
        {
            // Try to get Distance property via reflection for targeted buttons
            var prop = instance.GetType().GetProperty("Distance", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (prop != null)
            {
                var val = prop.GetValue(instance);
                if (val is float f) return f;
            }
            
            // Fallback for KillButton which might have its own distance logic
            if (instance is IKillButton)
            {
                var opts = GameOptionsManager.Instance?.currentNormalGameOptions;
                if (opts != null)
                {
                    var killDistances = opts.GetFloatArray(AmongUs.GameOptions.FloatArrayOptionNames.KillDistances);
                    var idx = Mathf.Clamp(opts.KillDistance, 0, killDistances.Length - 1);
                    return killDistances[idx] + 0.2f; // Add small buffer
                }
            }
        }
        catch
        {
            // ignore
        }

        return 1.5f; // Increased default from 1.25f to be safer
    }

    private static void SpendCooldownAndUses(CustomActionButton instance)
    {
        try
        {
            if (instance.LimitedUses)
            {
                instance.DecreaseUses(1);
            }

            instance.EffectActive = false;
            instance.Timer = instance.Cooldown;
        }
        catch
        {
            // ignore
        }
    }

    private static bool TryTriggerFromLocalPlayer(float maxDistance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.HasDied() || MeetingHud.Instance)
        {
            return false;
        }

        var from = local.GetTruePosition();
        if (!MirageDecoySystem.TryGetClosestDecoy(from, maxDistance, out var mirageId, out var decoyPos))
        {
            return false;
        }

        var mirage = MiscUtils.PlayerById(mirageId);
        if (mirage == null || mirage.HasDied() || !mirage.IsRole<MirageRole>())
        {
            return false;
        }

        MirageRole.RpcMirageTriggerDecoy(mirage, local, decoyPos);
        return true;
    }
}
















