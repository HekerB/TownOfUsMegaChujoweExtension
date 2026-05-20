using HarmonyLib;
using MiraAPI.Hud;
using System.Reflection;
using TownOfUs.Buttons;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.MirageDecoy;

public static class MirageDecoyTownOfUsTargetButtonPatches
{

    [HarmonyPatch(typeof(TownOfUsTargetButton<PlayerControl>), nameof(TownOfUsTargetButton<PlayerControl>.ClickHandler))]
    private static class PlayerTargetClickHandlerPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(object __instance)
        {
            if (__instance is MirageDecoyButton)
            {
                return true;
            }

            if (!TryTriggerFromLocalPlayer(GetDistance(__instance)))
            {
                return true;
            }

            SpendCooldownAndUses(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(TownOfUsTargetButton<PlayerControl>), nameof(TownOfUsTargetButton<PlayerControl>.FixedUpdateHandler))]
    private static class PlayerTargetFixedUpdateHandlerPatch
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
            if (actionButton == null || !actionButton.isActiveAndEnabled || actionButton.isCoolingDown)
            {
                MirageDecoySystem.ClearLocalOutline();
                return;
            }

            var distance = GetDistance(__instance);
            if (!MirageDecoySystem.TryGetClosestDecoy(local.GetTruePosition(), distance, out _, out _))
            {
                MirageDecoySystem.ClearLocalOutline();
                return;
            }

            actionButton.SetEnabled();
            ForceActionButtonVisualEnabled(actionButton);
            MirageDecoySystem.UpdateLocalOutline(local.GetTruePosition(), distance, GetOutlineColor(__instance));
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
    }




    private static bool TryTriggerFromLocalPlayer(float maxDistance)
    {
        return MirageDecoySystem.TryTriggerFromLocalPlayer(maxDistance);
    }

    private static void SpendCooldownAndUses(object instance)
    {
        try
        {
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
}
















