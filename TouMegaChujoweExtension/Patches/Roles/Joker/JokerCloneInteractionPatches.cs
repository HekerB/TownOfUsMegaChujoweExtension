#pragma warning disable S3011
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AmongUs.GameOptions;
using HarmonyLib;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Buttons.Classic.Neutral;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Patches;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Joker;

[HarmonyPatch]
public static class JokerCloneInteractionPatches
{
    private static readonly HashSet<string> NonCloneInteractableButtonNames =
    [
        "BomberPlantButton",
        "DetonatorAttachButton",
        "KamikazeSuicideButton",
        "PoisonerVineButton",
        "RcXdDeployButton",
        "ShifterShiftButton",
        "SniperShootButton"
    ];

    private static readonly HashSet<string> CloneInteractableButtonNames =
    [
        "ArsonistDouseButton",
        "ArsonistIgniteButton",
        "CampButton",
        "DeathKillButton",
        "DoomsayerObserveButton",
        "GlitchKillButton",
        "HunterStalkButton",
        "JailorJailButton",
        "JuggernautKillButton",
        "PestilenceKillButton",
        "SoulCollectorReapButton",
        "VeteranAlertButton",
        "WerewolfKillButton",
        "WerewolfRampageButton"
    ];

    private static bool TryTriggerFromLocalPlayer(float maxDistance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.HasDied() || MeetingHud.Instance)
        {
            return false;
        }

        return JokerCloneSystem.TryTriggerClosestClone(local, local.GetTruePosition(), maxDistance);
    }

    private static float GetKillDistance()
    {
        var options = GameOptionsManager.Instance?.currentNormalGameOptions;
        if (options == null)
        {
            return 1f;
        }

        var killDistances = options.GetFloatArray(FloatArrayOptionNames.KillDistances);
        var index = Math.Clamp(options.KillDistance, 0, killDistances.Length - 1);
        return killDistances[index];
    }

    private static ActionButton? _cachedKillButton;
    private static SpriteRenderer[] _cachedKillRenderers = [];
    private static TMPro.TMP_Text[] _cachedKillTexts = [];

    private static void ForceKillButtonVisualEnabled(ActionButton button)
    {
        try
        {
            if (_cachedKillButton != button)
            {
                _cachedKillButton = button;
                _cachedKillRenderers = button.GetComponentsInChildren<SpriteRenderer>(true);
                _cachedKillTexts = button.GetComponentsInChildren<TMPro.TMP_Text>(true);
            }

            foreach (var sr in _cachedKillRenderers)
            {
                if (sr == null) continue;
                sr.color = Palette.EnabledColor;
                sr.material?.SetFloat("_Desat", 0f);
            }

            foreach (var tmp in _cachedKillTexts.Where(tmp => tmp != null))
            {
                tmp.color = Palette.EnabledColor;
            }
        }
        catch
        {
            // ignore
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPostfix]
    public static void HudManagerUpdatePostfix(HudManager __instance)
    {
        if (__instance == null || MeetingHud.Instance || PlayerControl.LocalPlayer == null)
        {
            return;
        }

        if (__instance.KillButton != null &&
            __instance.KillButton.isActiveAndEnabled &&
            !__instance.KillButton.isCoolingDown)
        {
            var local = PlayerControl.LocalPlayer;
            var dist = GetKillDistance();
            if (local != null && JokerCloneSystem.TryGetClosestClone(local.GetTruePosition(), dist, out var cloneIndex, out _))
            {
                __instance.KillButton.SetEnabled();
                ForceKillButtonVisualEnabled(__instance.KillButton);
                JokerCloneSystem.UpdateLocalOutline(cloneIndex, Palette.ImpostorRed);
            }
            else
            {
                JokerCloneSystem.ClearLocalOutline();
            }
        }
    }

    [HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    public static bool KillButtonDoClickPrefix()
    {
        var hud = HudManager.Instance;
        if (hud == null || hud.KillButton == null || !hud.KillButton.isActiveAndEnabled || hud.KillButton.isCoolingDown)
        {
            return true;
        }

        if (!TryTriggerFromLocalPlayer(GetKillDistance()))
        {
            return true;
        }

        try
        {
            PlayerControl.LocalPlayer?.SetKillTimer(PlayerControl.LocalPlayer.GetKillCooldown());
        }
        catch
        {
            // vanilla kill cooldown fallback
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
            return HandleKillLikeButtonClick(__instance);
        }
    }

    [HarmonyPatch(typeof(TownOfUsTargetButton<PlayerControl>), nameof(TownOfUsTargetButton<PlayerControl>.ClickHandler))]
    public static class JokerCloneTownOfUsTargetButtonPatches
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(object __instance)
        {
            return HandleKillLikeButtonClick(__instance);
        }
    }

    [HarmonyPatch]
    public static class JokerCloneOverrideClickHandlerPatches
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            return GetLoadedTypes()
                .Where(type => type is { IsAbstract: false } &&
                               IsTownOfUsButtonType(type) &&
                               IsKillButtonType(type))
                .Select(type => type.GetMethod(
                    nameof(TownOfUsButton.ClickHandler),
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                .Where(method => method != null)
                .Cast<MethodBase>();
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(object __instance)
        {
            return HandleKillLikeButtonClick(__instance);
        }

        private static IEnumerable<Type> GetLoadedTypes()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type?[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }
                catch
                {
                    continue;
                }

                foreach (var type in types.Where(type => type != null))
                {
                    yield return type!;
                }
            }
        }

        private static bool IsTownOfUsButtonType(Type type)
        {
            return typeof(TownOfUsButton).IsAssignableFrom(type) ||
                   InheritsGenericDefinition(type, typeof(TownOfUsTargetButton<>));
        }

        private static bool InheritsGenericDefinition(Type type, Type genericDefinition)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == genericDefinition)
                {
                    return true;
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

            if (!IsKillButton(__instance))
            {
                return;
            }

            var local = PlayerControl.LocalPlayer;
            var actionButton = GetActionButton(__instance);
            if (local == null || local.HasDied() || actionButton == null || !actionButton.isActiveAndEnabled)
            {
                JokerCloneSystem.ClearLocalOutline();
                return;
            }

            var distance = GetDistance(__instance);
            if (!JokerCloneSystem.TryGetClosestClone(local.GetTruePosition(), distance, out var cloneIndex, out _))
            {
                JokerCloneSystem.ClearLocalOutline();
                return;
            }

            if (!actionButton.isCoolingDown)
            {
                actionButton.SetEnabled();
                ForceActionButtonVisualEnabled(actionButton);
            }

            JokerCloneSystem.UpdateLocalOutline(cloneIndex, GetOutlineColor(__instance));
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
                foreach (var spriteRenderer in button.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (spriteRenderer == null)
                    {
                        continue;
                    }

                    spriteRenderer.color = Palette.EnabledColor;
                    spriteRenderer.material?.SetFloat("_Desat", 0f);
                }

                foreach (var text in button.GetComponentsInChildren<TMPro.TMP_Text>(true).Where(text => text != null))
                {
                    text.color = Palette.EnabledColor;
                }
            }
            catch
            {
                // visual-only fallback
            }
        }

        private static Color GetOutlineColor(object buttonInstance)
        {
            try
            {
                var roleProp = buttonInstance.GetType().GetProperty("Role", BindingFlags.Instance | BindingFlags.Public);
                var roleObject = roleProp?.GetValue(buttonInstance);
                var teamColorProp = roleObject?.GetType().GetProperty("TeamColor", BindingFlags.Instance | BindingFlags.Public);
                if (teamColorProp?.GetValue(roleObject) is Color color)
                {
                    return color;
                }
            }
            catch
            {
                // fallback below
            }

            return Palette.EnabledColor;
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPostfix]
    public static void MeetingHudStartPostfix()
    {
        JokerCloneSystem.IncrementMeetingCount();
        JokerPlaceCloneButton.LocalInstance?.ResetStage();

        if (OptionGroupSingleton<JokerOptions>.Instance.ResetClonesEachMeeting)
        {
            JokerCloneSystem.ClearClones(includePreviews: false);
        }
    }



    private static void SpendCooldownAndUses(object instance)
    {
        try
        {
            if (instance.GetType().Name.Contains("WarlockKillButton", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (instance is CustomActionButton button)
            {
                if (button.LimitedUses)
                {
                    button.DecreaseUses(1);
                }

                button.EffectActive = false;
                button.Timer = button.Cooldown;
                return;
            }

            if (GetBoolProperty(instance, "LimitedUses"))
            {
                InvokeMethod(instance, "DecreaseUses", 1);
            }

            SetProperty(instance, "EffectActive", false);
            var cooldown = GetFloatProperty(instance, "Cooldown");
            SetProperty(instance, "Timer", cooldown);
        }
        catch
        {
            // cooldown fallback
        }
    }

    private static float GetDistance(object instance)
    {
        try
        {
            var prop = instance.GetType().GetProperty("Distance", BindingFlags.Instance | BindingFlags.Public);
            if (prop?.GetValue(instance) is float distance)
            {
                return distance;
            }

            if (instance is IKillButton)
            {
                return GetKillDistance() + 0.2f;
            }
        }
        catch
        {
            // fallback below
        }

        return 1.5f;
    }

    private static bool IsKillButton(object button)
    {
        return IsKillButtonType(button.GetType());
    }

    private static bool IsKillButtonType(Type type)
    {
        if (NonCloneInteractableButtonNames.Contains(type.Name))
        {
            return false;
        }

        if (CloneInteractableButtonNames.Contains(type.Name))
        {
            return true;
        }

        if (typeof(IKillButton).IsAssignableFrom(type) ||
            IsPlayerTargetKillRoleButton(type))
        {
            return true;
        }

        var typeName = type.Name;
        return typeName.Contains("Kill", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("Bite", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("Shoot", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("Stake", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("Ambush", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("Murder", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("Execute", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("Swallow", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("Overtake", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("Poison", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("Starve", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("Hunt", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("Vanquish", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("Reap", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("Spell", StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanButtonClick(object instance)
    {
        try
        {
            return InvokeBoolMethod(instance, "CanClick", false) &&
                   PlayerControl.LocalPlayer != null &&
                   !PlayerControl.LocalPlayer.HasModifier<GlitchHackedModifier>() &&
                   !PlayerControl.LocalPlayer.HasModifier<DisabledModifier>();
        }
        catch
        {
            return false;
        }
    }

    private static bool HandleKillLikeButtonClick(object instance)
    {
        if (instance is JokerPlaceCloneButton || !IsKillButton(instance) || !CanButtonClick(instance))
        {
            return true;
        }

        var local = PlayerControl.LocalPlayer;
        var distance = GetDistance(instance);
        if (local == null || !JokerCloneSystem.TryGetClosestClone(local.GetTruePosition(), distance, out var cloneIndex, out _))
        {
            return true;
        }

        if (!JokerCloneSystem.TryTriggerClone(local, cloneIndex))
        {
            return true;
        }

        SpendCooldownAndUses(instance);
        return false;
    }



    private static bool IsPlayerTargetKillRoleButton(Type type)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            if (!current.IsGenericType)
            {
                continue;
            }

            var genericDefinition = current.GetGenericTypeDefinition();
            if (genericDefinition == typeof(TownOfUsKillRoleButton<,>))
            {
                var targetType = current.GetGenericArguments()[1];
                return typeof(PlayerControl).IsAssignableFrom(targetType);
            }
        }

        return false;
    }

    private static bool InvokeBoolMethod(object instance, string methodName, bool fallback)
    {
        try
        {
            var method = GetMethod(instance.GetType(), methodName);
            return method?.Invoke(instance, null) is bool value ? value : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static void InvokeMethod(object instance, string methodName, params object[] args)
    {
        try
        {
            GetMethod(instance.GetType(), methodName)?.Invoke(instance, args);
        }
        catch
        {
            // reflection fallback
        }
    }

    private static bool GetBoolProperty(object instance, string propertyName)
    {
        try
        {
            return GetProperty(instance.GetType(), propertyName)?.GetValue(instance) is true;
        }
        catch
        {
            return false;
        }
    }

    private static float GetFloatProperty(object instance, string propertyName)
    {
        try
        {
            return GetProperty(instance.GetType(), propertyName)?.GetValue(instance) is float value ? value : 0f;
        }
        catch
        {
            return 0f;
        }
    }

    private static void SetProperty(object instance, string propertyName, object value)
    {
        try
        {
            GetProperty(instance.GetType(), propertyName)?.SetValue(instance, value);
        }
        catch
        {
            // reflection fallback
        }
    }

    private static PropertyInfo? GetProperty(Type type, string propertyName)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            var property = current.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
            {
                return property;
            }
        }

        return null;
    }

    private static MethodInfo? GetMethod(Type type, string methodName)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            var method = current.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null)
            {
                return method;
            }
        }

        return null;
    }
}

[HarmonyPatch(typeof(HudManagerPatches), nameof(HudManagerPatches.UpdateCamouflageComms))]
public static class JokerCloneCamoCommsPatch
{
    public static void Postfix()
    {
        JokerCloneSystem.SyncCamouflageComms();
    }
}
