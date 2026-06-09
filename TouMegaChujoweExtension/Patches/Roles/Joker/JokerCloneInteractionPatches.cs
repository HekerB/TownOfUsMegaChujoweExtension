using AmongUs.GameOptions;
using HarmonyLib;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using System;
using System.Reflection;
using TouMegaChujoweExtension.Buttons.Classic.Neutral;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Joker;

[HarmonyPatch]
public static class JokerCloneInteractionPatches
{
    private static bool TryTriggerFromLocalPlayer(float maxDistance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.HasDied() || MeetingHud.Instance)
        {
            return false;
        }

        var from = local.GetTruePosition();
        if (!JokerCloneSystem.TryGetClosestClone(from, maxDistance, out var cloneIndex, out _))
        {
            return false;
        }

        var clone = JokerCloneSystem.Clones[cloneIndex];
        var joker = MiscUtils.PlayerById(clone.JokerId);
        if (joker == null || joker.HasDied() || !joker.IsRole<JokerRole>())
        {
            return false;
        }

        JokerRole.RpcJokerCloneKilled(local, clone.JokerId, (byte)cloneIndex);
        return true;
    }

    private static float GetKillDistance()
    {
        var opts = GameOptionsManager.Instance?.currentNormalGameOptions;
        if (opts == null)
        {
            return 1.0f;
        }

        var killDistances = opts.GetFloatArray(FloatArrayOptionNames.KillDistances);
        var idx = Math.Clamp(opts.KillDistance, 0, killDistances.Length - 1);
        return killDistances[idx];
    }

    [HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    public static bool KillButtonDoClickPrefix()
    {
        if (!TryTriggerFromLocalPlayer(GetKillDistance()))
        {
            return true;
        }

        try
        {
            var local = PlayerControl.LocalPlayer;
            if (local != null)
            {
                local.SetKillTimer(local.GetKillCooldown());
            }
        }
        catch
        {
            // ignore
        }

        return false;
    }

    [HarmonyPatch(typeof(TownOfUsButton), nameof(TownOfUsButton.ClickHandler))]
    public static class JokerCloneTownOfUsButtonPatches
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(TownOfUsButton __instance)
        {
            if (__instance is JokerPlaceCloneButton)
            {
                return true;
            }

            var distance = GetDistance(__instance);
            var local = PlayerControl.LocalPlayer;
            if (local == null) return true;

            if (!JokerCloneSystem.TryGetClosestClone(local.GetTruePosition(), distance, out _, out _))
            {
                return true;
            }

            if (CanButtonClick(__instance))
            {
                if (IsKillButton(__instance))
                {
                    if (TryTriggerFromLocalPlayer(distance))
                    {
                        SpendCooldownAndUses(__instance);
                    }
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(TownOfUsTargetButton<PlayerControl>), nameof(TownOfUsTargetButton<PlayerControl>.ClickHandler))]
    public static class JokerCloneTownOfUsTargetButtonPatches
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(object __instance)
        {
            if (__instance is JokerPlaceCloneButton)
            {
                return true;
            }

            var distance = GetDistance(__instance);
            var local = PlayerControl.LocalPlayer;
            if (local == null) return true;

            if (!JokerCloneSystem.TryGetClosestClone(local.GetTruePosition(), distance, out _, out _))
            {
                return true;
            }

            if (CanButtonClick(__instance))
            {
                if (IsKillButton(__instance))
                {
                    if (TryTriggerFromLocalPlayer(distance))
                    {
                        SpendCooldownAndUses(__instance);
                    }
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(TownOfUsTargetButton<PlayerControl>), nameof(TownOfUsTargetButton<PlayerControl>.FixedUpdateHandler))]
    public static class JokerCloneTownOfUsTargetButtonFixedUpdatePatches
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(object __instance)
        {
            if (MeetingHud.Instance)
            {
                return;
            }

            var local = PlayerControl.LocalPlayer;
            if (local == null || (local.Data?.IsDead ?? false))
            {
                return;
            }

            var actionButton = GetActionButton(__instance);
            if (actionButton == null || !actionButton.isActiveAndEnabled)
            {
                JokerCloneSystem.ClearLocalOutline();
                return;
            }

            var distance = GetDistance(__instance);
            if (!JokerCloneSystem.TryGetClosestClone(local.GetTruePosition(), distance, out _, out _))
            {
                JokerCloneSystem.ClearLocalOutline();
                return;
            }

            if (!actionButton.isCoolingDown)
            {
                actionButton.SetEnabled();
                ForceActionButtonVisualEnabled(actionButton);
            }
            JokerCloneSystem.UpdateLocalOutline(local.GetTruePosition(), distance, GetOutlineColor(__instance));
        }
    }

    private static ActionButton? GetActionButton(object instance)
    {
        try
        {
            var prop = instance.GetType().GetProperty("Button", BindingFlags.Instance | BindingFlags.Public);
            return prop?.GetValue(instance) as ActionButton;
        }
        catch
        {
            return null;
        }
    }

    private static void ForceActionButtonVisualEnabled(ActionButton button)
    {
        try
        {
            var renderers = button.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in renderers)
            {
                if (sr == null) continue;
                sr.color = Palette.EnabledColor;
                if (sr.material != null)
                {
                    sr.material.SetFloat("_Desat", 0f);
                }
            }

            var tmps = button.GetComponentsInChildren<TMPro.TMP_Text>(true);
            foreach (var tmp in tmps)
            {
                if (tmp == null) continue;
                tmp.color = Palette.EnabledColor;
            }
        }
        catch
        {
            // ignore
        }
    }

    private static Color GetOutlineColor(object buttonInstance)
    {
        try
        {
            var roleProp = buttonInstance.GetType().GetProperty("Role", BindingFlags.Instance | BindingFlags.Public);
            var roleObj = roleProp?.GetValue(buttonInstance);
            if (roleObj != null)
            {
                var teamColorProp = roleObj.GetType().GetProperty("TeamColor", BindingFlags.Instance | BindingFlags.Public);
                var teamColorObj = teamColorProp?.GetValue(roleObj);
                if (teamColorObj is Color c)
                {
                    return c;
                }
            }
        }
        catch
        {
            // ignore
        }

        return Palette.EnabledColor;
    }

    private static void SpendCooldownAndUses(object instance)
    {
        try
        {
            // If it's a Warlock's kill button, do not spend cooldown or uses, just ignore it so that they are fooled but don't lose charge/cooldown.
            if (instance.GetType().Name.Contains("WarlockKillButton"))
            {
                return;
            }

            if (instance is CustomActionButton btn)
            {
                if (btn.LimitedUses)
                {
                    btn.DecreaseUses(1);
                }

                btn.EffectActive = false;
                btn.Timer = btn.Cooldown;
            }
        }
        catch
        {
            // ignore
        }
    }

    private static float GetDistance(object instance)
    {
        try
        {
            var prop = instance.GetType().GetProperty("Distance", BindingFlags.Instance | BindingFlags.Public);
            if (prop != null)
            {
                var val = prop.GetValue(instance);
                if (val is float f) return f;
            }

            if (instance is IKillButton)
            {
                var opts = GameOptionsManager.Instance?.currentNormalGameOptions;
                if (opts != null)
                {
                    var killDistances = opts.GetFloatArray(AmongUs.GameOptions.FloatArrayOptionNames.KillDistances);
                    var idx = Mathf.Clamp(opts.KillDistance, 0, killDistances.Length - 1);
                    return killDistances[idx] + 0.2f;
                }
            }
        }
        catch
        {
            // ignore
        }

        return 1.5f;
    }

    private static bool IsKillButton(object button)
    {
        if (button == null) return false;
        if (button is IKillButton) return true;
        var typeName = button.GetType().Name;
        if (typeName.Contains("Kill", StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains("Bite", StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains("Shoot", StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains("Stake", StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains("Ambush", StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains("Overtake", StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains("Vanquish", StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains("Murder", StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains("Reap", StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains("Execute", StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains("Euthanize", StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains("Lethal", StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains("Death", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return false;
    }

    private static bool CanButtonClick(object instance)
    {
        try
        {
            if (instance is TownOfUsButton btn)
            {
                return btn.CanClick() && 
                       PlayerControl.LocalPlayer != null &&
                       !PlayerControl.LocalPlayer.HasModifier<GlitchHackedModifier>() &&
                       !PlayerControl.LocalPlayer.HasModifier<DisabledModifier>();
            }
        }
        catch
        {
            // fallback
        }
        return false;
    }
}
